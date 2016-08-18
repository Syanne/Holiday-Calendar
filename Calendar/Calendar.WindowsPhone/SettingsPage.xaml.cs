using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.Phone.UI.Input;
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
        SharedSettingsServices ssServices;
        PurchasingService pService;

        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            ssServices = new SharedSettingsServices();
            pService = new PurchasingService();

            //enable toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                {
                    ssServices.IsTileSet = true;
                    tileToggle.IsOn = true;
                }
                if (task.Value.Name == "ToastBackgroundTask")
                {
                    ssServices.IsToastSet = true;
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
            string name = "TileBackgroundTask";
            string entryPoint = "BackgroundUpdater.TileBackgroundTask";

            ssServices.TileEnableController(name, entryPoint, tileToggle.IsOn, ref ssServices.IsTileSet);
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            //if toast set - do not unset it until a user call this method
            if (!ssServices.IsToastSet)
            {
                string name = "ToastBackgroundTask";
                string entryPoint = "BackgroundUpdater.ToastBackgroundTask";

                try
                {
                    //set/unset if purchased
                    var license = CurrentApp.LicenseInformation;
                    if (license.ProductLicenses[PurchasingService.ALL_STUFF].IsActive)
                    {
                        //if enabled
                        if (toastToggle.IsOn)
                        {
                            //save changes
                            DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                            DataManager.SaveDocumentAsync();

                            //set period and create a task
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

                        }
                    }
                    //elseway - unset
                    else pService.OfferPurchase("Unlicensed", null);   
                }
                catch (Exception ex)
                {
                    pService.MyMessage(ex.Message);
                }
            }

            ssServices.IsToastSet = false;
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
                if (!license.ProductLicenses[PurchasingService.ALL_STUFF].IsActive)
                {
                    await CurrentApp.RequestProductPurchaseAsync(PurchasingService.ALL_STUFF);
                }
            }
            catch (Exception ex)
            {
                pService.MyMessage(ex.Message);
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
                    await CurrentApp.RequestProductPurchaseAsync(PurchasingService.ALL_STUFF);
                }
                catch (Exception ex)
                {
                    pService.MyMessage(ex.Message);
                }
            }
        }

        #endregion

        private void smartTileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ssServices.SmartTileController(smartTileToggle.IsOn, (comboAmount.SelectedItem as ComboBoxItem).Content.ToString());
            
        }
    }
}
