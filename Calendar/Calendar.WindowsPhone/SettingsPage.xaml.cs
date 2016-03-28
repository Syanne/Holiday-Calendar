using CalendarResources;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.Globalization;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента пустой страницы см. по адресу http://go.microsoft.com/fwlink/?LinkID=390556

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            //enable toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                    tileToggle.IsOn = true;
                if (task.Value.Name == "ToastBackgroundTask")
                    toastToggle.IsOn = true;
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
            CalendarResourcesManager.SaveDocumentAsync();

            TileToggle("TileBackgroundTask", "BackgroundUpdater.TileBackgroundTask");
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToastToggle("ToastBackgroundTask", "BackgroundUpdater.ToastBackgroundTask");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }
        
        private void buy_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }

        private void SettingsFlyout_Unloaded(object sender, RoutedEventArgs e)
        {
            var rootFrame = new Frame();
            rootFrame.Navigate(typeof(MainPage));
        }


        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }


        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (toastToggle != null && toastToggle.IsOn)
            {
                if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
                {
                    CalendarResourcesManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                    //save changes
                    CalendarResourcesManager.SaveDocumentAsync();

                    //set period
                    uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                    BackgroundTaskCreator("ToastBackgroundTask", "BackgroundUpdater.ToastBackgroundTask", period);

                }
            }
        }


        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        #region BgTasks

        private void TileToggle(string name, string entryPoint)
        {
            if (tileToggle.IsOn)
            {
                BackgroundTaskCreator(name, entryPoint, 15);
            }
            else
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                    if (task.Value.Name == "TileBackgroundTask")
                        task.Value.Unregister(true);
            }
        }

        private void ToastToggle(string name, string entryPoint)
        {
                if (toastToggle.IsOn)
                {
                    CalendarResourcesManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                    //save changes
                    CalendarResourcesManager.SaveDocumentAsync();

                    //set period
                    uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                    BackgroundTaskCreator(name, entryPoint, period * 60);
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                        if (task.Value.Name == "ToastBackgroundTask")
                            task.Value.Unregister(true);

                    toastToggle.IsOn = false;
                }
            
        }


        public async void BackgroundTaskCreator(string name, string entryPoint, uint time)
        {
            //clean
            foreach (var task in BackgroundTaskRegistration.AllTasks)
                if (task.Value.Name == name)
                    task.Value.Unregister(true);

            //register
            var timerTrigger = new TimeTrigger(time, false);
            await BackgroundExecutionManager.RequestAccessAsync();

            var builder = new BackgroundTaskBuilder();

            builder.Name = name;
            builder.TaskEntryPoint = entryPoint;
            builder.SetTrigger(timerTrigger);
            builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));

            BackgroundTaskRegistration tTask = builder.Register();
        }

        #endregion

        #region Purchasing

        private async void BuyButtonController()
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses["allstuff1"].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);
                }
            }
        }


        private async void BuyThis()
        {
            var dial = new MessageDialog(CalendarResourcesManager.resource.GetString("Unlicensed"));

            dial.Commands.Add(new UICommand(CalendarResourcesManager.resource.GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(CalendarResourcesManager.resource.GetString("UnlicensedButton"),
            new UICommandInvokedHandler((args) =>
            {
                BuyStuff();
            })));
            var command = await dial.ShowAsync();
        }

        private async void BuyStuff()
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses["allstuff1"].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);
                }
            }
        }

        #endregion


    }
}
