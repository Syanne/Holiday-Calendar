using Calendar.SocialNetworkConnector;
using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Calendar
{
    public sealed partial class BaseSettingsFlyout : SettingsFlyout
    {
        private bool isTileSet = false;
        private bool isToastSet = false;
        private bool isGoogleService = false;
        private ExtraServices eService;

        public BaseSettingsFlyout()
        {
            this.InitializeComponent();
            eService = new ExtraServices();
            //enable task toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                {
                    isTileSet = true;
                    tileToggle.IsOn = true;
                }
                if (task.Value.Name == "ToastBackgroundTask")
                {
                    isToastSet = true;
                    toastToggle.IsOn = true;
                    comboPeriod.IsEnabled = false;
                    comboToast.IsEnabled = false;
                }
            }

            //services
            if (DataManager.Services != null)
                if (DataManager.Services.Contains("google"))
                {
                    isGoogleService = true;
                    googleToggle.IsOn = true;
                    googleToggle.IsEnabled = true;
                    comboGooglePeriod.IsEnabled = false;
                }
        }

        private void tileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isTileSet)
            {
                string name = "TileBackgroundTask";
                string entryPoint = "BackgroundUpdater.TileBackgroundTask";

                if (tileToggle.IsOn)
                    eService.BackgroundTaskCreator(name, entryPoint, 15);
                
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                        if (task.Value.Name == "TileBackgroundTask")
                            task.Value.Unregister(true);
                }
            }
            isTileSet = false;
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isToastSet)
            {
                string name = "ToastBackgroundTask";
                string entryPoint = "BackgroundUpdater.ToastBackgroundTask";
                string licenseName = "allstuff1";

                try
                {
                    if (CurrentApp.LicenseInformation.ProductLicenses[licenseName].IsActive)
                    {
                        if (toastToggle.IsOn)
                        {
                            DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                            //save changes
                            DataManager.SaveDocumentAsync();

                            //set period
                            uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                            eService.BackgroundTaskCreator(name, entryPoint, period * 60);
                            comboPeriod.IsEnabled = false;
                            comboToast.IsEnabled = false;
                        }
                        else
                        {
                            foreach (var task in BackgroundTaskRegistration.AllTasks)
                                if (task.Value.Name == name)
                                    task.Value.Unregister(true);

                            comboPeriod.IsEnabled = true;
                            comboToast.IsEnabled = true;
                        }
                    }
                    else
                    {
                        toastToggle.IsOn = false;
                        eService.OfferPurchase("Unlicensed", null);
                    }
                }
                catch (Exception ex)
                {
                    eService.MyMessage(ex.Message);
                }

                isToastSet = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            eService.BuyStuff("allstuff1");
        }

        private void buy_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }
        
        private void BuyButtonController()
        {
            eService.BuyStuff("allstuff1");
        }
      
        private void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = 600;
        }

        private void googleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isGoogleService)
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

            isGoogleService = false;
        }
        
    }
}
