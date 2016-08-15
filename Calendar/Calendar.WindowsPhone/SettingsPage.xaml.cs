using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
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
        private bool isGoogleService = false;
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

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            string name = "ToastBackgroundTask";
            string entryPoint = "BackgroundUpdater.ToastBackgroundTask";
            string licenseName = "allstuff1";

            try
            {
                var license = CurrentApp.LicenseInformation;
                if (license.ProductLicenses[licenseName].IsActive)
                {
                    if (toastToggle.IsOn)
                    {
                        //save changes
                        DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                        DataManager.SaveDocumentAsync();

                        //set period
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
                else
                {
                    toastToggle.IsOn = false;
                    eService.OfferPurchase("Unlicensed", "", licenseName);
                }

            }
            catch (Exception ex)
            {
                eService.MyMessage(ex.Message);
                toastToggle.IsOn = false;
            }

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
    }
}
