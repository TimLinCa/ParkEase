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
		private bool isAllFloorCheckEnabled;

		[ObservableProperty]
		private string selectedStartDate;

		[ObservableProperty]
		private string selectedEndDate;

		[ObservableProperty]
		private bool isCurrentDayCheck;

		[ObservableProperty]
		private bool isAllDayCheck;

		[ObservableProperty]
		private TimeSpan startTime;

		[ObservableProperty]
		private TimeSpan endTime;

		[ObservableProperty]
		private ISeries[] seriesCollection;
		#endregion

		#region OnPropertyChangedEvent
		partial void OnAreaTypeSelectedChanged(string value)
		{
			if (value == string.Empty || value == null)
			{
				AreaNameItemSource = new ObservableCollection<string>();
				AreaNameSelected = null;
			}
			UpdateAreaNameItemSource();
		}

		partial void OnIsAllFloorCheckChanged(bool value)
		{
			if (value == false) LoadFloorInfo();
			else ClearFloorInfo();
		}
		#endregion

		#region private variables
		private ParkEaseModel model;
		private IMongoDBService mongoDBService;
		private IDialogService dialogService;
		private List<ParkingData> parkingDatas;
		private List<PrivateParking> privateParkings;
		#endregion

		public AnalysisViewModel(ParkEaseModel model, IMongoDBService mongoDBService, IDialogService dialogService)
		{
			this.dialogService = dialogService;
			this.mongoDBService = mongoDBService;
			this.model = model;
			IsAllFloorCheckEnabled = false;
			IsCurrentDayCheck = true;
			IsFloorEnabled = false;
			IsAllFloorCheck = true;
			AreaTypeItemSource = new ObservableCollection<string>();
			StartTime = new TimeSpan(0, 0, 0);
			SeriesCollection = new ISeries[]
			{
				new LineSeries<int>
				{
					Values = new int[] { 4, 6, 5, 3, -3, -1, 2 }
				},
				new ColumnSeries<double>
				{
					Values = new double[] { 2, 5, 4, -2, 4, -3, 5 }
				}
			};
		}

		#region ICommand method
		public ICommand LoadedCommand => new RelayCommand(() =>
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


		public ICommand ApplyCommand => new RelayCommand(() =>
		{
			if(EndTime < StartTime)
			{
				dialogService.ShowAlertAsync("Error", "End time should be greater than start time", "OK");
				return;
			}


		});
		#endregion

		#region Private method
		private async Task UpdateAreaNameItemSource()
		{
			if (AreaTypeSelected == AreaType.Public.ToString())
			{
				parkingDatas = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
				AreaNameItemSource = new ObservableCollection<string>(parkingDatas.Select(pd => pd.ParkingSpot).ToList());
				IsAllFloorCheckEnabled = false;
			}
			else if (AreaTypeSelected == AreaType.Private.ToString())
			{
				var filter = Builders<PrivateParking>.Filter.Eq(p => p.CreatedBy, model.User.Email);
				privateParkings = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);
				AreaNameItemSource = new ObservableCollection<string>(privateParkings.Select(pp => pp.CompanyName + $"({pp.Address})"));
				IsAllFloorCheckEnabled = true;
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
		#endregion
	}
}
