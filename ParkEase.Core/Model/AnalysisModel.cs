using ParkEase.Core.Contracts.abstracts;
using ParkEase.Core.Data;
using ParkEase.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Model
{
    //From Claude
    partial class ParkEaseModel
    {
        public Dictionary<DateTime, double> GetAverageUsagePercentage(List<ParkingLog> data, DateOnly startDate, DateOnly endDate, TimeOnly startTime, TimeOnly endTime, TimeInterval interval)
        {
            switch (interval)
            {
                case TimeInterval.Hourly:
                    return AnalysisUtility.CalculateAverageHourlyUsage(data, startDate, endDate, startTime, endTime);
                case TimeInterval.Daily:
                    return AnalysisUtility.CalculateDailyUsage(data, startDate, endDate, startTime, endTime);
                case TimeInterval.Monthly:
                    return AnalysisUtility.CalculateMonthlyUsage(data, startDate, endDate, startTime, endTime);
                default:
                    throw new ArgumentException("Invalid interval");
            }
        }

        public Dictionary<DateTime, double> GetAverageParkingTime(List<ParkingLog> data, DateOnly startDate, DateOnly endDate, TimeOnly startTime, TimeOnly endTime, TimeInterval interval)
        {
            switch (interval)
            {
                case TimeInterval.Daily:
                    return AnalysisUtility.CalculateDailyAverageParkingTime(data, startDate, endDate, startTime, endTime);
                case TimeInterval.Monthly:
                    return AnalysisUtility.CalculateMonthlyAverageParkingTime(data, startDate, endDate, startTime, endTime);
                default:
                    throw new ArgumentException("Invalid interval");
            }
        }


     

    

    }
}
