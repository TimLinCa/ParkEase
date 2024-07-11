﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Utilities;
using System.Windows.Input;
using ParkEase.Core.Contracts.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using System.Collections.ObjectModel;
using MongoDB.Driver;
using ParkEase.Core.Services;
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using ZXing.Net.Maui;
using CommunityToolkit.Mvvm.Messaging;
using ParkEase.Messages;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ParkEase.Page;
using ParkEase.Controls;

namespace ParkEase.ViewModel
{
    /*
     * What information are needed
     * - Company name
     * - Address(picker)
     * - Fee
     * - LimitedHour
     * 
     * - Floor(picker)
     */

    public partial class PrivateStatusViewModel : ObservableObject
    {
        #region ObservableProperty
        [ObservableProperty]
        private ObservableCollection<string> propertyAddressList;

        [ObservableProperty]
        private string addressSelected;

        [ObservableProperty]
        private PrivateParking propertySelected;

        [ObservableProperty]
        private ObservableCollection<string> floorItemSource;

        [ObservableProperty]
        private string floorItemSelected;

        [ObservableProperty]
        private FloorInfo floorSelected;

        [ObservableProperty]
        private ObservableCollection<Rectangle> listRectangleFill;

        [ObservableProperty]
        private IImage imgSourceData;

        [ObservableProperty]
        private int availabilityCount = 0;
        #endregion

        private ParkEaseModel model;
        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        private List<PrivateParking> privateParkings;
        private List<FloorInfo> listFloorInfos;
        private string privateParkingId;

        private CancellationTokenSource cts;
        readonly bool stopping = false;
        CancellationTokenSource cls = new CancellationTokenSource();

        #region OnPropertyChangedEvent
        partial void OnAddressSelectedChanged(string value)
        {
            _ = LoadPropertyInfo();
        }

        partial void OnFloorItemSelectedChanged(string value)
        {
            LoadFloorInfo();
        }
        #endregion



        public PrivateStatusViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.model = model;
        }

        public ICommand LoadedCommand => new RelayCommand(async () =>
        {
            await LoadPropertyAddressList();

            cts = new CancellationTokenSource();
            var token = cts.Token;
            _ = Run(token); // Start the real-time update loop
        });

        public ICommand UnLoadedCommand => new RelayCommand(() =>
        {
            cts?.Cancel(); // Cancel the real-time update loop
            PropertyAddressList = new ObservableCollection<string>();
            AddressSelected = string.Empty;
            PropertySelected = null;
            FloorItemSource = new ObservableCollection<string>();
            FloorItemSelected = string.Empty;
            FloorSelected = null;
            ListRectangleFill = new ObservableCollection<Rectangle>();
            ImgSourceData = null;
            AvailabilityCount = 0;
            privateParkings = new List<PrivateParking>();
            listFloorInfos = new List<FloorInfo>();
            privateParkingId = string.Empty;
        });

        private async Task LoadPropertyAddressList()
        {
            var filter = Builders<PrivateParking>.Filter.Eq(p => p.CreatedBy, model.User.Email);
            privateParkings = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);
            if (privateParkings == null)
            {
                System.Diagnostics.Debug.WriteLine("Private Parkings is null.");
                return;
            }

            // Sort by company name
            var sortedPrivateParkings = privateParkings.OrderBy(pp => pp.CompanyName);

            PropertyAddressList = new ObservableCollection<string>(sortedPrivateParkings.Select(pp => pp.CompanyName + $" ({pp.Address})"));
        }

        // Load the selected property information
        private async Task LoadPropertyInfo()
        {
           if (AddressSelected == null || AddressSelected == string.Empty) return;
           // Split company name and address from the selected address
            string[] strings = AddressSelected.Split('(');
            if (strings.Length > 1)
            {
                string companyName = strings[0].Trim();
                string address = strings[1].Replace(")", "").Trim();

                // Get the selected property
                PropertySelected = privateParkings.FirstOrDefault(pp => pp.CompanyName == companyName && pp.Address == address);
                if (PropertySelected == null)
                {
                    System.Diagnostics.Debug.WriteLine("Property Selected is null.");
                    return;
                }
                privateParkingId = PropertySelected.Id;

                // Get the floor information
                listFloorInfos = PropertySelected.FloorInfo;
                // this is for the floor picker
                FloorItemSource = new ObservableCollection<string>(PropertySelected.FloorInfo.Select(fi => fi.Floor));
                // Set the selected floor to the first floor
                if (FloorItemSource.FirstOrDefault() != null)
                {
                    FloorItemSelected = FloorItemSource.First();
                }
            }
        }

        private async Task Run(CancellationToken token)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (!stopping)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            await LoadFloorInfo();
                            await Task.Delay(2000, token);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }, token);
            } catch (Exception ex) 
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
                });
                //await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
            
        }

        // Load the selected floor information
        private async Task LoadFloorInfo()
        {
            if (listFloorInfos != null)
            {
                // Fetch PrivateStatus data from MongoDB
                var status = await mongoDBService.GetStatusData<PrivateStatus>(CollectionName.PrivateStatus);
                if (status == null || status.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No private status data found.");
                    return;
                }

                // Filter privateStatusData based on privateParkingId
                var privateStatusData = status.Where(item => item.AreaId == privateParkingId).ToList();
                // Filter by selectedFloorName and Create a dictionary for quick lookup of statuses by index
                var matchingStatus = privateStatusData
                    .Where(item => item.Floor == FloorItemSelected)
                    .ToDictionary(item => item.LotId, item => item.Status);

                // Fetch floor information
                FloorInfo selectedFloor = listFloorInfos.FirstOrDefault(data => data.Floor == FloorItemSelected);
                if (selectedFloor == null)
                {
                    System.Diagnostics.Debug.WriteLine("No selected floor map found.");
                    return;
                }

                // Fetch image data
                var imageData = selectedFloor.ImageData;
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
                }

                // Variable to count availability status (false means available lot)
                ObservableCollection<Rectangle> rectangles = new ObservableCollection<Rectangle>();
                // Update rectangle colors based on status and add them to ListRectangle
                foreach (var rectangle in selectedFloor.Rectangles)
                {
                    if (matchingStatus.TryGetValue(rectangle.Index, out bool isAvailable))
                    {
                        if (!isAvailable)
                        {
                            rectangle.Color = "green";
                            AvailabilityCount++;
                        }
                        else
                        {
                            rectangle.Color = "red";
                        }
                    }
                    rectangles.Add(rectangle);
                }
                ListRectangleFill = rectangles;
            }
            else
            {
                //await dialogService.ShowAlertAsync("Error", "No floor information found", "OK");
            }
        }
    }
}