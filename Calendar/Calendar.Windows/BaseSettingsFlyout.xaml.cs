﻿using Calendar.SocialNetworkConnector;
using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Resources;

namespace Calendar
{
    public sealed partial class BaseSettingsFlyout : SettingsFlyout
    {
        SharedSettingsServices ssServices;
        PurchasingService pService;

        public BaseSettingsFlyout()
        {
            this.InitializeComponent();

            ssServices = new SharedSettingsServices();
            pService = new PurchasingService();

            //enable task toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                {
                    ssServices.IsTileSet = true;
                    tileToggle.IsOn = true;
                }
                if (task.Value.Name == "ToastBackgroundTask")
                {
                    toastToggle.IsOn = true;
                    comboPeriod.IsEnabled = false;
                    comboToast.IsEnabled = false;
                }
                if (task.Value.Name == "SmartTileBackgroundTask")
                {
                    ssServices.IsSmartTileSet = true;
                    smartTileToggle.IsOn = true;
                    comboAmount.IsEnabled = false;
                }
            }

            //shopping toggles
            if (pService.License.ProductLicenses[PurchasingService.ALL_STUFF].IsActive)
            {
                allBuy.IsOn = true;
                allBuy.IsEnabled = false;
                bgBuy.IsEnabled = false;
            }
            else if (pService.License.ProductLicenses[PurchasingService.BACKGROUND_SERVICES].IsActive)
            {
                bgBuy.IsOn = true;
                bgBuy.IsEnabled = false;
            }

            //services
            if (DataManager.Services != null && DataManager.Services.Contains("google"))
            {
                ssServices.IsGoogleService = true;
                googleToggle.IsOn = true;
                googleToggle.IsEnabled = true;
                comboGooglePeriod.IsEnabled = false;
            }
        }

        private void tileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!smartTileToggle.IsOn)
            {
                string name = "TileBackgroundTask";
                string entryPoint = "BackgroundUpdater.TileBackgroundTask";

                ssServices.TileEnableController(name, entryPoint, tileToggle.IsOn, ref ssServices.IsTileSet, 15);
            }
            else
            {
                tileToggle.IsOn = false;
                flyoutText.Text = ResourceLoader.GetForCurrentView("Resources").GetString("disableSmartTile");
                FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);
            }
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            string name = "ToastBackgroundTask";
            string entryPoint = "BackgroundUpdater.ToastBackgroundTask";

            try
            {
                LicenseInformation license = CurrentApp.LicenseInformation;
                if (license.ProductLicenses[PurchasingService.ALL_STUFF].IsActive ||
                    license.ProductLicenses[PurchasingService.BACKGROUND_SERVICES].IsActive)
                {
                    if (toastToggle.IsOn)
                    {
                        DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                        //save changes
                        DataManager.SaveDocumentAsync();

                        //set period
                        uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                        ssServices.BackgroundTaskCreator(name, entryPoint, period * 60);
                        comboPeriod.IsEnabled = false;
                        comboToast.IsEnabled = false;
                    }
                    else
                    {
                        foreach (var task in BackgroundTaskRegistration.AllTasks)
                            if (task.Value.Name == name)
                                task.Value.Unregister(true);

                        toastToggle.IsOn = false;
                        comboPeriod.IsEnabled = true;
                        comboToast.IsEnabled = true;
                    }
                }
                else
                {
                    //pService.OfferPurchase("Unlicensed", null);
                    flyoutText.Text = ResourceLoader.GetForCurrentView("Resources").GetString("Unlicensed");
                    FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);

                    toastToggle.IsOn = false;
                    comboPeriod.IsEnabled = true;
                    comboToast.IsEnabled = true;
                }
            }
            catch { }

        }
      
        private void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = 700;
        }

        private void googleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!ssServices.IsGoogleService)
                if (googleToggle.IsOn)
                {
                    //set period
                    int period = Convert.ToInt32((comboGooglePeriod.SelectedItem as ComboBoxItem).Content);
                    DateTime date = DateTime.Now;

                    try
                    {
                        //period = int.Parse(DataManager.PersonalData.Root.Element("google").Attribute("period").Value);
                        var array = DataManager.PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(BaseConnector.DateSeparator);
                        date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                        if (DataManager.PersonalData.Root.Element("google").Attribute("isActive").Value == "false")
                            DataManager.ChangeServiceState("google", true);
                    }
                    catch
                    {
                        date = DateTime.Now;
                    }
                    finally
                    {
                        if (date.Day >= DateTime.Now.Day && date.Month >= DateTime.Now.Month && date.Year >= DateTime.Now.Year)
                            SyncManager.Manager.AddService("google", DateTime.Now, period);                        
                    }

                    comboGooglePeriod.IsEnabled = false;
                }
                else
                {
                    DataManager.ChangeServiceState("google", false);
                    googleToggle.IsOn = false;
                    comboGooglePeriod.IsEnabled = true;
                }

            ssServices.IsGoogleService = false;
        }

        private void smartTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                LicenseInformation license = CurrentApp.LicenseInformation;
                if (license.ProductLicenses[PurchasingService.ALL_STUFF].IsActive ||
                    license.ProductLicenses[PurchasingService.BACKGROUND_SERVICES].IsActive)
                {
                    if (!tileToggle.IsOn)
                    {
                        ssServices.SmartTileController(smartTileToggle.IsOn, (comboAmount.SelectedItem as ComboBoxItem).Content.ToString());

                        if (smartTileToggle.IsOn)
                            comboAmount.IsEnabled = false;
                        else comboAmount.IsEnabled = true;
                    }
                    else
                    {
                        smartTileToggle.IsOn = false;
                        flyoutText.Text = ResourceLoader.GetForCurrentView("Resources").GetString("disableTile");
                        FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);
                    }
                }
                else
                {
                    smartTileToggle.IsOn = false;
                    flyoutText.Text = ResourceLoader.GetForCurrentView("Resources").GetString("Unlicensed");
                    FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);
                }
            }
            catch { }
        }
        
        #region purchase
        private void allBuy_Toggled(object sender, RoutedEventArgs e)
        {
            if (allBuy.IsOn)
                pService.BuyStuff(PurchasingService.ALL_STUFF);
            if (pService.toggle != true)
                allBuy.IsOn = false;
        }

        private void bgBuy_Toggled(object sender, RoutedEventArgs e)
        {
            if (bgBuy.IsOn)
                pService.BuyStuff(PurchasingService.BACKGROUND_SERVICES);
            if (pService.toggle != true)
                bgBuy.IsOn = false;
        }
        #endregion
    }
}
