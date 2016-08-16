﻿using System;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.ApplicationModel.Background;

namespace Calendar.Services
{
    /// <summary>
    /// includes Background and Shopping services
    /// </summary>
    public partial class ExtraServices
    {
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

        #region purchase
        /// <summary>
        /// Offer user to purchase the app
        /// </summary>
        /// <param name="content">content key in resources file</param>
        /// <param name="title">title key in resources file or null</param>
        public async void OfferPurchase(string content, string title)
        {
            MessageDialog dial;
            if (title != null)
                dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString(content),
                    ResourceLoader.GetForCurrentView("Resources").GetString(title));
            else dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString(content));


            dial.Commands.Add(new UICommand("OK"));            
            var command = await dial.ShowAsync();
        }

        public async void BuyStuff(string packageName)
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
                    MyMessage(ex.Message);
                }
            }
        }

        public async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        #endregion

    }
}
