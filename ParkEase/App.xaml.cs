﻿using ParkEase.Page;
using ParkEase.ViewModel;

namespace ParkEase
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NBaF5cXmZCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXpcd3ZXQmZYVEF1W0s=");

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                AppShellMobileViewModel appShellMobileViewModel = new AppShellMobileViewModel();
                MainPage = new AppShellMobile(appShellMobileViewModel);
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                AppShellViewModel appShellViewModel = new AppShellViewModel();
                MainPage = new AppShell(appShellViewModel);
            }

        }
    }
}
