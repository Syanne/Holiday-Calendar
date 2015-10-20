using System;
using CalendarResources;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {

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
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
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
                }
            }
            else
            {
                BuyThis();
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
