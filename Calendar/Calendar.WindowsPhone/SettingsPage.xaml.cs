using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Phone.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private bool isTileSet = false;
        private bool isToastSet = false;
        //private bool isGoogleService = false;
        private ExtraServices eService;

        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;

            eService = new ExtraServices();

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            //enable toggles
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
                }
            }
        }

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Вызывается перед отображением этой страницы во фрейме.
        /// </summary>
        /// <param name="e">Данные события, описывающие, каким образом была достигнута эта страница.
        /// Этот параметр обычно используется для настройки страницы.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

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
            //if toast set - do not unset it until a user call this method
            if (!isToastSet)
            {
                string name = "ToastBackgroundTask";
                string entryPoint = "BackgroundUpdater.ToastBackgroundTask";
                string licenseName = "allstuff1";

                try
                {
                    //set/unset if purchased
                    var license = CurrentApp.LicenseInformation;
                    if (license.ProductLicenses[licenseName].IsActive)
                    {
                        //if enabled
                        if (toastToggle.IsOn)
                        {
                            //save changes
                            DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                            DataManager.SaveDocumentAsync();

                            //set period and create a task
                            uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                            eService.BackgroundTaskCreator(name, entryPoint, period * 60);
                        }
                        else
                        {
                            foreach (var task in BackgroundTaskRegistration.AllTasks)
                                if (task.Value.Name == name)
                                    task.Value.Unregister(true);

                            toastToggle.IsOn = false;
                        }
                    }
                    //elseway - unset
                    else
                    {
                        isToastSet = true;
                        toastToggle.IsOn = false;
                        eService.OfferPurchase("Unlicensed", null);
                    }

                }
                catch (Exception ex)
                {
                    isToastSet = true;
                    toastToggle.IsOn = false;
                    eService.MyMessage(ex.Message);
                }
            }
            isToastSet = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }
        
        private void buy_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }
        
        
        private async void BuyButtonController()
        {
            try
            {
                LicenseInformation license = CurrentApp.LicenseInformation;
                if (!license.ProductLicenses["allstuff1"].IsActive)
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
            }
            catch (Exception ex)
            {
                eService.MyMessage(ex.Message);
            }
        }

        #region purchase

        private async void BuyStuff(string packageName)
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses[packageName].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    eService.MyMessage(ex.Message);
                }
            }
        }
       
        #endregion
    }
}
