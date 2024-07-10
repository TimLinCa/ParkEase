using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Core.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ParkEase.Core.Data;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Services;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Syncfusion.Maui.Calendar;
using ParkEase.Core.Contracts.abstracts;
using LiveChartsCore.Defaults;
namespace ParkEase.ViewModel
{
    //https://livecharts.dev/docs/Maui/2.0.0-rc2/CartesianChart.Cartesian%20chart%20control
    //https://enisn-projects.io/docs/en/uranium/latest/Getting-Started
    public partial class AnalysisViewModel : ObservableObject
    {


        #region ObservableProperty
        [ObservableProperty]
        private ObservableCollection<string> areaTypeItemSource;

        [ObservableProperty]
        private string areaTypeSelected;

        [ObservableProperty]
        private ObservableCollection<string> areaNameItemSource;

        [ObservableProperty]
        private string areaNameSelected;

        [ObservableProperty]
        private string areaName;

        [ObservableProperty]
        private ObservableCollection<string> floorItemSource;

        [ObservableProperty]
        private string floorSelected;

        [ObservableProperty]
        private bool isFloorEnabled;

        [ObservableProperty]
        private bool isAllFloorCheck;

        [ObservableProperty]
        private DateTime currentDate;

        [ObservableProperty]
        private CalendarDateRange selectedDateRange;

        [ObservableProperty]
        private bool isCurrentDayCheck;

        [ObservableProperty]
        private bool isAllDayCheck;

        [ObservableProperty]
        private bool isUsageMonthlyChecked;

        [ObservableProperty]
        private bool isUsageDailyChecked;

        [ObservableProperty]
        private bool isUsageHourlyChecked;

        [ObservableProperty]
        private bool isParkingTimeMonthlyChecked;

        [ObservableProperty]
        private bool isParkingTimeDailyChecked;

        [ObservableProperty]
        private bool isFloowSelectedVisible;

        [ObservableProperty]
        private TimeSpan startTime;

        [ObservableProperty]
        private TimeSpan endTime;

        [ObservableProperty]
        private ISeries[] usageSeriesCollection;

        [ObservableProperty]
        private Axis[] usageXAxes;

        [ObservableProperty]
        private Axis[] usageYAxes;

        [ObservableProperty]
        private string averageUsage;

        [ObservableProperty]
        private Color averageUsageColor;

        [ObservableProperty]
        private ISeries[] parkingTimeSeriesCollection;

        [ObservableProperty]
        private Axis[] parkingTimeXAxes;

        [ObservableProperty]
        private Axis[] parkingTimeYAxes;

        [ObservableProperty]
        private string averageParkingTime;

        [ObservableProperty]
        private string areaNameText;

        [ObservableProperty]
        private IAsyncRelayCommand applyCommand;
        #endregion

        #region OnPropertyChangedEvent
        partial void OnAreaTypeSelectedChanged(string value)
        {
            if (value == string.Empty || value == null)
            {
                AreaNameItemSource = new ObservableCollection<string>();
                AreaNameSelected = null;
                IsFloowSelectedVisible = false;
            }
            loadDataTask = UpdateAreaNameItemSource();
        }

        partial void OnIsAllFloorCheckChanged(bool value)
        {
            if (value == false) LoadFloorInfo();
            else ClearFloorInfo();
        }

        partial void OnIsUsageMonthlyCheckedChanged(bool value)
        {
            UpdateUsageGraph();
        }

        partial void OnIsUsageDailyCheckedChanged(bool value)
        {
            UpdateUsageGraph();
        }

        partial void OnIsParkingTimeMonthlyCheckedChanged(bool value)
        {
            UpdateParkingTimeGraph();
        }

        partial void OnIsParkingTimeDailyCheckedChanged(bool value)
        {
            UpdateParkingTimeGraph();
        }

        partial void OnIsUsageHourlyCheckedChanged(bool value)
        {
            UpdateUsageGraph();
        }
        #endregion

        #region public properties

        #endregion

        #region private variables
        private List<ParkingLog> parkingLogs;
        private ParkEaseModel model;
        private IMongoDBService mongoDBService;
        private IDialogService dialogService;
        private List<ParkingData> parkingDatas;
        private List<PrivateParking> privateParkings;
        DateOnly currentStartDate;
        DateOnly currentEndDate;
        TimeOnly currentStartTime;
        TimeOnly currentEndTime;
        private Task loadDataTask;
        #endregion
        public AnalysisViewModel(ParkEaseModel model, IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.mongoDBService = mongoDBService;
            this.model = model;
            CurrentDate = DateTime.Now;
            IsCurrentDayCheck = true;
            IsFloorEnabled = false;
            IsAllFloorCheck = true;
            IsParkingTimeDailyChecked = true;
            IsFloowSelectedVisible  = false;
            AreaTypeItemSource = new ObservableCollection<string>();
            StartTime = new TimeSpan(0, 0, 0);

            UsageYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Usage(%)",
                    NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),
                }
            };

            ParkingTimeYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Parking Time(hs)",
                    NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),
                }
            };

            ApplyCommand = new AsyncRelayCommand(ExecuteApplyCommandAsync);

            IsUsageHourlyChecked = true;
        }

        #region ICommand method
        public ICommand LoadedCommand => new RelayCommand(async () =>
        {
            AreaTypeItemSource.Clear();
            switch (model.User.Role)
            {
                case Roles.Administrator:
                    AreaTypeItemSource.Add(AreaType.Private.ToString());
                    break;
                case Roles.Engineer:
                    AreaTypeItemSource.Add(AreaType.Public.ToString());
                    break;
                case Roles.Developer:
                    AreaTypeItemSource.Add(AreaType.Public.ToString());
                    AreaTypeItemSource.Add(AreaType.Private.ToString());
                    break;
            }
        });

        public ICommand AreaNameUnfocusedCommand => new RelayCommand(() =>
        {
            try
            {
                if (AreaTypeSelected == AreaType.Public.ToString()) return;
                if (IsAllFloorCheck) return;
                LoadFloorInfo();
            }
            catch (Exception ex)
            {
                dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        private async Task ExecuteApplyCommandAsync()
        {
            try
            {
                if (EndTime < StartTime)
                {
                    await dialogService.ShowAlertAsync("Error", "End time should be greater than start time", "OK");
                    return;
                }

                if (!IsCurrentDayCheck)
                {
                    if (SelectedDateRange == null)
                    {
                        await dialogService.ShowAlertAsync("Error", "Please select a date range", "OK");
                        return;
                    }
                }

                await UpdateUsageData();
                UpdateUsageGraph();
                UpdateParkingTimeGraph();
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", $"Load data error, please contact developer team. Reason:{ex}", "OK");
            }
        }
        #endregion

        #region Private method
        public async Task UpdateAreaNameItemSource()
        {
            if (AreaTypeSelected == AreaType.Public.ToString())
            {
                parkingDatas = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
                AreaNameItemSource = new ObservableCollection<string>(parkingDatas.Select(pd => pd.ParkingSpot).ToList());
                IsFloowSelectedVisible = false;
                AreaNameText = "";
            }
            else if (AreaTypeSelected == AreaType.Private.ToString())
            {
                var filter = Builders<PrivateParking>.Filter.Eq(p => p.CreatedBy, model.User.Email);
                privateParkings = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);
                AreaNameItemSource = new ObservableCollection<string>(privateParkings.Select(pp => pp.CompanyName + $"({pp.Address})"));
                IsFloowSelectedVisible = true;
                AreaNameText = "";
            }
        }

        private void LoadFloorInfo()
        {
            bool enableFloor = false;

            if (AreaNameSelected != string.Empty && AreaNameSelected != null)
            {
                string[] strings = AreaNameSelected.Split('(');
                if (strings.Length > 1)
                {
                    string areaName = strings[0];
                    string address = strings[1].Replace(")", "");
                    PrivateParking privateParking = privateParkings.FirstOrDefault(pp => pp.CompanyName == areaName && pp.Address == address);
                    if (privateParking == null) enableFloor = false;
                    else
                    {
                        FloorItemSource = new ObservableCollection<string>(privateParking.FloorInfo.Select(fl => fl.Floor).ToList());
                        FloorSelected = FloorItemSource.FirstOrDefault();
                        enableFloor = true;
                    }
                }
            }

            IsFloorEnabled = enableFloor;
            if (enableFloor == false)
            {
                ClearFloorInfo();
            }
        }

        private void ClearFloorInfo()
        {
            IsFloorEnabled = false;
            FloorItemSource = new ObservableCollection<string>();
            FloorSelected = string.Empty;
        }

        private async Task UpdateUsageData()
        {
            try
            {
                if(loadDataTask.IsCompleted == false)
                {
                    await loadDataTask;
                }
                currentStartDate = IsCurrentDayCheck ? DateOnly.FromDateTime(DateTime.Now) : DateOnly.FromDateTime(SelectedDateRange.StartDate.Value);
                currentEndDate = IsCurrentDayCheck ? DateOnly.FromDateTime(DateTime.Now) : DateOnly.FromDateTime(SelectedDateRange.EndDate.Value);
                currentStartTime = IsAllDayCheck ? TimeOnly.MinValue : new TimeOnly(StartTime.Hours, StartTime.Minutes, 0);
                currentEndTime = IsAllDayCheck ? TimeOnly.MaxValue : new TimeOnly(EndTime.Hours, EndTime.Minutes, 0);

                switch(AreaTypeSelected)
                {
                    case "Public":
                        ParkingData parkingData = parkingDatas.FirstOrDefault(pd => pd.ParkingSpot == AreaNameSelected);
                        parkingLogs = (await mongoDBService.GetData<PublicLog>(CollectionName.PublicLogs)).Cast<ParkingLog>().ToList();
                        parkingLogs = parkingLogs.Where(pl => pl.AreaId.Equals(parkingData.Id) &&
                        pl.Timestamp >= currentStartDate.ToDateTime(TimeOnly.MinValue) &&
                        pl.Timestamp <= currentEndDate.ToDateTime(TimeOnly.MaxValue)).ToList();
                        break;
                    case "Private":
                        string[] strings = AreaNameSelected.Split('(');
                        string areaName = strings[0];
                        string address = strings[1].Replace(")", "");
                        PrivateParking privateParking = privateParkings.FirstOrDefault(pp => pp.CompanyName == areaName && pp.Address == address);

                        List<PrivateLog> privateLogs = await mongoDBService.GetData<PrivateLog>(CollectionName.PrivateLogs);
                        if (!IsAllFloorCheck)
                        {
                            privateLogs = privateLogs.Where(pl => pl.Floor.Equals(FloorSelected)).ToList();
                        }
                        parkingLogs = privateLogs.Cast<ParkingLog>().ToList();
                        parkingLogs = parkingLogs.Where(pl => pl.AreaId.Equals(privateParking.Id) &&
                        pl.Timestamp >= currentStartDate.ToDateTime(TimeOnly.MinValue) &&
                        pl.Timestamp <= currentEndDate.ToDateTime(TimeOnly.MaxValue)).ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void UpdateUsageGraph()
        {
            try
            {
                if (parkingLogs != null && parkingLogs.Count != 0)
                {
                    TimeInterval timeInterval = TimeInterval.Hourly;

                    if (IsUsageMonthlyChecked)
                    {
                        timeInterval = TimeInterval.Monthly;
                    }
                    else if (IsUsageDailyChecked)
                    {
                        timeInterval = TimeInterval.Daily;
                    }
                    else if (IsUsageHourlyChecked)
                    {
                        timeInterval = TimeInterval.Hourly;
                    }

                    Dictionary<DateTime, double> usageData = model.GetAverageUsagePercentage(parkingLogs, currentStartDate, currentEndDate, currentStartTime, currentEndTime, timeInterval);
                    UsageSeriesCollection = new ISeries[] {
                        new LineSeries<double>
                        {
                            Values = usageData.Select(ud=>ud.Value).ToArray()
                        }
                    };

                    UsageXAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = usageData.Select(ud=>ud.Key.ToString()).ToArray()
                        }
                    };
                    
                    double AverageUsageNum = Math.Round(usageData.Values.Average(), 2);
                    if (AverageUsageNum < 50)
                    {
                        AverageUsageColor = Color.FromRgb(0, 255, 0);
                    }
                    else if (AverageUsageNum < 80)
                    {
                        AverageUsageColor = Color.FromRgb(255, 223, 0);
                    }
                    else
                    {
                        AverageUsageColor = Color.FromRgb(255, 0, 0);
                    }
                    AverageUsage = AverageUsageNum.ToString() + "%";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void UpdateParkingTimeGraph()
        {
            try
            {
                if (parkingLogs != null && parkingLogs.Count != 0)
                {
                    TimeInterval timeInterval = TimeInterval.Daily;

                    if (IsParkingTimeMonthlyChecked)
                    {
                        timeInterval = TimeInterval.Monthly;
                    }
                    else if (IsParkingTimeDailyChecked)
                    {
                        timeInterval = TimeInterval.Daily;
                    }

                    Dictionary<DateTime, double> parkingTimeData = model.GetAverageParkingTime(parkingLogs, currentStartDate, currentEndDate, currentStartTime, currentEndTime, timeInterval);
                    ParkingTimeSeriesCollection = new ISeries[] {
                        new ColumnSeries<double>
                        {
                            Values = parkingTimeData.Select(ud=>ud.Value).ToArray()
                        }
                    };

                    ParkingTimeXAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = parkingTimeData.Select(ud=>ud.Key.ToString()).ToArray()
                        }
                    };
                    double averageParkingTime = Math.Round(parkingTimeData.Values.Average(), 2);
                    AverageParkingTime = averageParkingTime.ToString() + "hs";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
