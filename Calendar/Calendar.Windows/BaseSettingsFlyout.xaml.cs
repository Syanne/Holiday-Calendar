using Calendar.SocialNetworkConnector;
using CalendarResources;
using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Calendar
{
    public sealed partial class BaseSettingsFlyout : SettingsFlyout
    {
        public BaseSettingsFlyout()
        {
            this.InitializeComponent();
            //enable task toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                    tileToggle.IsOn = true;
                if (task.Value.Name == "ToastBackgroundTask")
                {
                    toastToggle.IsOn = true;
                    comboPeriod.IsEnabled = false;
                    comboToast.IsEnabled = false;
                }
            }

            //services
            if (DataManager.Services != null)
                if (DataManager.Services.Contains("google"))
                {
                    googleToggle.IsOn = true;
                    googleToggle.IsEnabled = true;
                    comboGooglePeriod.IsEnabled = false;
                }
        }

        private void tileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            TileToggle("TileBackgroundTask", "BackgroundUpdater.TileBackgroundTask");
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToastToggle("ToastBackgroundTask", "BackgroundUpdater.ToastBackgroundTask");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BuyStuff();
        }

        private void buy_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
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
            try
            {
                if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
                {
                    if (toastToggle.IsOn)
                    {
                        DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                        //save changes
                        DataManager.SaveDocumentAsync();

                        //set period
                        uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                        BackgroundTaskCreator(name, entryPoint, period * 60);
                        comboPeriod.IsEnabled = false;
                        comboToast.IsEnabled = false;
                    }
                    else
                    {
                        foreach (var task in BackgroundTaskRegistration.AllTasks)
                            if (task.Value.Name == "ToastBackgroundTask")
                                task.Value.Unregister(true);
                        comboPeriod.IsEnabled = true;
                        comboToast.IsEnabled = true;
                    }
                }
                else
                {
                    OfferPurchase();
                }
            }
            catch (Exception ex)
            {
                MyMessage(ex.Message);
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

        private void BuyButtonController()
        {
            BuyStuff();
        }

        private async void OfferPurchase()
        {
            var dial = new MessageDialog(DataManager.resource.GetString("Unlicensed"));

            dial.Commands.Add(new UICommand(DataManager.resource.GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(DataManager.resource.GetString("UnlicensedButton"),
            new UICommandInvokedHandler((args) =>
            {
                BuyStuff();
            })));
            var command = await dial.ShowAsync();
        }

        private async void BuyStuff()
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
                MyMessage(ex.Message);
            }
        }
        
        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        #endregion

        //private void buttonTheme_Click(object sender, RoutedEventArgs e)
        //{
        //
        //}

        private void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = 600;
        }

        private void googleToggle_Toggled(object sender, RoutedEventArgs e)
        {
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
                    
                    if(DataManager.PersonalData.Root.Element("google").Attribute("isActive").Value == "false")
                        DataManager.ChangeServiceState("google", true);
                }
                catch
                {
                    date = DateTime.Now;
                }
                finally
                {
                    if (date.Day >= DateTime.Now.Day && date.Month >= DateTime.Now.Month && date.Year >= DateTime.Now.Year)
                    {
                        SyncManager.Manager.AddService("google", DateTime.Now, period);
                    }
                }
                comboGooglePeriod.IsEnabled = false;
            }
            else
            {
                DataManager.ChangeServiceState("google", false);
                googleToggle.IsOn = false;
                comboGooglePeriod.IsEnabled = true;
            }
        }
    }
}
