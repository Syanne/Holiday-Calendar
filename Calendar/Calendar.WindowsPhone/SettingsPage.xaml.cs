using Calendar.Services;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
            if (!smartTileToggle.IsOn)
            {
                string name = "TileBackgroundTask";
                string entryPoint = "BackgroundUpdater.TileBackgroundTask";

                ssServices.TileEnableController(name, entryPoint, tileToggle.IsOn, ref ssServices.IsTileSet, 15);
            }
            else tileToggle.IsOn = false;
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            //if toast set - do not unset it until a user call this method
            if (!ssServices.IsToastSet && toastToggle.IsOn)
            {
                string name = "ToastBackgroundTask";
                string entryPoint = "BackgroundUpdater.ToastBackgroundTask";

                try
                {
                    //set/unset if purchased
                    LicenseInformation license = CurrentApp.LicenseInformation;
                    if (license.ProductLicenses[PurchasingService.ALL_STUFF].IsActive ||
                        license.ProductLicenses[PurchasingService.BACKGROUND_SERVICES].IsActive)
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

                            toastToggle.IsOn = false;
                            comboPeriod.IsEnabled = true;
                            comboToast.IsEnabled = true;
                        }
                    }
                    //elseway - unset
                    else
                    {
                        toastToggle.IsOn = false;
                        comboPeriod.IsEnabled = true;
                        comboToast.IsEnabled = true;
                        FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);
                    } 
                }
                catch (Exception ex)
                {
                    pService.MyMessage(ex.Message);
                }
            }

            ssServices.IsToastSet = false;
        }

        private void smartTileToggle_Toggled(object sender, RoutedEventArgs e)
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
                else smartTileToggle.IsOn = false;
            }
            else
            {
                smartTileToggle.IsOn = false;
                FlyoutBase.ShowAttachedFlyout(baseStackPanel as FrameworkElement);
            }
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
