using ParkEase.Core.Contracts.abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Utilities
{
    ////From Claude
    public class AnalysisUtility
    {
        public static Dictionary<DateTime, double> CalculateAverageHourlyUsage(
         List<ParkingLog> parkingData,
         DateOnly startDate,
         DateOnly endDate,
         TimeOnly startTime,
         TimeOnly endTime)
        {
            var hourlyUsages = new Dictionary<DateTime, List<double>>();
            var spotIndices = parkingData.Select(p => p.Index).Distinct();

            foreach (var spotIndex in spotIndices)
            {
                var spotData = parkingData
                    .Where(p => p.Index == spotIndex &&
                                DateOnly.FromDateTime(p.Timestamp) >= startDate &&
                                DateOnly.FromDateTime(p.Timestamp) <= endDate &&
                                TimeOnly.FromDateTime(p.Timestamp) >= startTime &&
                                TimeOnly.FromDateTime(p.Timestamp) <= endTime)
                    .OrderBy(p => p.Timestamp)
                    .ToList();

                var currentDate = startDate;
                bool lastKnownStatus = false;
                DateTime? lastKnownTimestamp = null;

                while (currentDate <= endDate)
                {
                    var dailyData = spotData.Where(p => DateOnly.FromDateTime(p.Timestamp) == currentDate).ToList();

                    DateTime currentDateTime = currentDate.ToDateTime(startTime);
                    DateTime endDateTime = currentDate.ToDateTime(endTime);

                    if (currentDate == endDate)
                    {
                        endDateTime = endDateTime.Date.Add(endTime.ToTimeSpan());
                    }

                    while (currentDateTime < endDateTime)
                    {
                        var nextHour = currentDateTime.AddHours(1);
                        var hourEnd = nextHour > endDateTime ? endDateTime : nextHour;

                        var hourData = dailyData.Where(p => p.Timestamp >= currentDateTime && p.Timestamp < hourEnd).ToList();

                        var occupiedMinutes = CalculateOccupiedMinutes(hourData, ref lastKnownStatus, ref lastKnownTimestamp, currentDateTime, hourEnd);
                        var usage = occupiedMinutes / (hourEnd - currentDateTime).TotalMinutes;

                        if (!hourlyUsages.ContainsKey(currentDateTime))
                        {
                            hourlyUsages[currentDateTime] = new List<double>();
                        }
                        hourlyUsages[currentDateTime].Add(usage);

                        currentDateTime = nextHour;
                    }

                    currentDate = currentDate.AddDays(1);
                }
            }

            // Calculate average usage for each hour
            return hourlyUsages.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Round(kvp.Value.Average() * 100, 2)
            );
        }

        public static Dictionary<DateTime, double> CalculateMonthlyUsage(
        List<ParkingLog> parkingData,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly startTime,
        TimeOnly endTime)
        {
            var hourlyUsage = CalculateAverageHourlyUsage(parkingData, startDate, endDate, startTime, endTime);

            var monthlyUsage = new Dictionary<DateTime, List<double>>();

            foreach (var (hour, usage) in hourlyUsage)
            {
                var monthStart = new DateTime(hour.Year, hour.Month, 1);
                if (!monthlyUsage.ContainsKey(monthStart))
                {
                    monthlyUsage[monthStart] = new List<double>();
                }
                monthlyUsage[monthStart].Add(usage);
            }

            return monthlyUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Average()
            );
        }

        public static Dictionary<DateTime, double> CalculateDailyUsage(
        List<ParkingLog> parkingData,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly startTime,
        TimeOnly endTime)
        {
            var hourlyUsage = CalculateAverageHourlyUsage(parkingData, startDate, endDate, startTime, endTime);

            var dailyUsage = new Dictionary<DateTime, List<double>>();

            foreach (var (hour, usage) in hourlyUsage)
            {
                var dayStart = hour.Date;
                if (!dailyUsage.ContainsKey(dayStart))
                {
                    dailyUsage[dayStart] = new List<double>();
                }
                dailyUsage[dayStart].Add(usage);
            }

            return dailyUsage.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Average()
            );
        }

        private static double CalculateOccupiedMinutes(
        List<ParkingLog> hourData,
        ref bool lastKnownStatus,
        ref DateTime? lastKnownTimestamp,
        DateTime hourStart,
        DateTime hourEnd)
        {
            double occupiedMinutes = 0;

            if (lastKnownTimestamp != null && lastKnownTimestamp < hourStart)
            {
                // Use the last known status for the beginning of the hour
                if (lastKnownStatus)
                {
                    occupiedMinutes += (hourData.Count > 0 ? hourStart - lastKnownTimestamp.Value : hourEnd - hourStart).TotalMinutes;
                    lastKnownTimestamp = hourStart;
                }
            }

            foreach (var data in hourData)
            {
                if (lastKnownStatus && lastKnownTimestamp != null)
                {
                    occupiedMinutes += (data.Timestamp - lastKnownTimestamp.Value).TotalMinutes;
                }

                lastKnownStatus = data.Status;
                lastKnownTimestamp = data.Timestamp;
            }

            // Account for the last status until the end of the hour
            if (lastKnownStatus)
            {
                occupiedMinutes += (hourEnd - lastKnownTimestamp.Value).TotalMinutes;
                lastKnownTimestamp = hourEnd;
            }

            return Math.Min(occupiedMinutes, (hourEnd - hourStart).TotalMinutes);
        }

        public static Dictionary<DateTime, double> CalculateDailyAverageParkingTime(
         List<ParkingLog> parkingData,
         DateOnly startDate,
         DateOnly endDate,
         TimeOnly startTime,
         TimeOnly endTime)
        {
            var dailyParkingTimes = new Dictionary<DateTime, List<TimeSpan>>();
            var spotStates = new Dictionary<int, (bool Occupied, DateTime LastChange)>();

            foreach (var data in parkingData.OrderBy(d => d.Timestamp))
            {
                var day = data.Timestamp.Date;
                var time = TimeOnly.FromDateTime(data.Timestamp);

                if (day < startDate.ToDateTime(TimeOnly.MinValue) || day > endDate.ToDateTime(TimeOnly.MaxValue) ||
                    time < startTime || time > endTime)
                    continue;

                if (!dailyParkingTimes.ContainsKey(day))
                    dailyParkingTimes[day] = new List<TimeSpan>();

                if (!spotStates.ContainsKey(data.Index))
                    spotStates[data.Index] = (false, DateTime.MinValue);

                var (occupied, lastChange) = spotStates[data.Index];

                if (data.Status && !occupied)
                {
                    // Car starts parking
                    spotStates[data.Index] = (true, data.Timestamp);
                }
                else if (!data.Status && occupied)
                {
                    // Car ends parking
                    var parkingStart = lastChange < day.Add(startTime.ToTimeSpan()) ? day.Add(startTime.ToTimeSpan()) : lastChange;
                    var parkingEnd = data.Timestamp > day.Add(endTime.ToTimeSpan()) ? day.Add(endTime.ToTimeSpan()) : data.Timestamp;
                    var parkingDuration = parkingEnd - parkingStart;
                    if (parkingDuration > TimeSpan.Zero)
                        dailyParkingTimes[day].Add(parkingDuration);
                    spotStates[data.Index] = (false, DateTime.MinValue);
                }
            }

            // Handle cars still parked at the end of the analysis period
            var analysisEnd = endDate.ToDateTime(endTime);
            foreach (var (spotIndex, (occupied, lastChange)) in spotStates)
            {
                if (occupied)
                {
                    var day = lastChange.Date;
                    var parkingStart = lastChange < day.Add(startTime.ToTimeSpan()) ? day.Add(startTime.ToTimeSpan()) : lastChange;
                    var parkingEnd = analysisEnd < day.Add(endTime.ToTimeSpan()) ? analysisEnd : day.Add(endTime.ToTimeSpan());
                    var parkingDuration = parkingEnd - parkingStart;
                    if (parkingDuration > TimeSpan.Zero)
                        dailyParkingTimes[day].Add(parkingDuration);
                }
            }

            return dailyParkingTimes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count > 0 ? Math.Round(kvp.Value.Average(ts => ts.TotalHours), 2) : 0
            );
        }

        public static Dictionary<DateTime, double> CalculateMonthlyAverageParkingTime(
            List<ParkingLog> parkingData,
            DateOnly startDate,
            DateOnly endDate,
            TimeOnly startTime,
            TimeOnly endTime)
        {
            var dailyAverages = CalculateDailyAverageParkingTime(parkingData, startDate, endDate, startTime, endTime);
            var monthlyAverages = new Dictionary<DateTime, List<double>>();

            foreach (var (day, averageTime) in dailyAverages)
            {
                var monthStart = new DateTime(day.Year, day.Month, 1);
                if (!monthlyAverages.ContainsKey(monthStart))
                    monthlyAverages[monthStart] = new List<double>();

                monthlyAverages[monthStart].Add(averageTime);
            }

            return monthlyAverages.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Round(kvp.Value.Average(), 2)
            );
        }
    }
}
