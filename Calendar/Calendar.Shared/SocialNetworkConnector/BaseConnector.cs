using Calendar.Data.Models;
using Calendar.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Calendar.SocialNetworkConnector
{
    public abstract class BaseConnector
    {
        /// <summary>
        /// Records in calendar
        /// </summary>
        protected List<ItemBase> Items { get; set; }
        /// <summary>
        /// repeat each
        /// </summary>
        protected int Period { get; set; }
        /// <summary>
        /// sync date 
        /// </summary>
        protected virtual DateTime DateStart { get; set; }
        /// <summary>
        /// next sync date
        /// </summary>
        protected virtual DateTime DateEnd { get; set; }
        /// <summary>
        /// name of a service to connect
        /// </summary>
        protected abstract string ServiceName { get; }

        /// <summary>
        /// Creates a new object of BaseConnector 
        /// </summary>
        /// <param name="period">snooze time</param>
        public BaseConnector(int period)
        {
            this.Items = new List<ItemBase>();
        }

        /// <summary>  
        /// Creates a new object of BaseConnector 
        /// </summary>
        /// <param name="dateStart">sync start date</param>
        /// <param name="period">snooze time</param>
        public BaseConnector(DateTime dateStart, int period)
        {
            DateStart = dateStart;
            Period = period;
            DateEnd = dateStart.Date.AddDays(period - 1);
            this.Items = new List<ItemBase>();
        }

        /// <summary>
        /// Gets records from service
        /// </summary>
        /// <returns>requires async/await</returns>
        public abstract Task GetHolidayList();

        /// <summary>
        /// Test method for finalizing operation
        /// </summary>
        protected async void Message()
        {
            var dial = new MessageDialog(Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("Resources").GetString("ServiceMessage"));

            dial.Commands.Add(new UICommand("OK", cmd=> 
            {
                FinalizeSync();
            }));

            var command = await dial.ShowAsync();
        }

        /// <summary>
        /// Saves records
        /// </summary>
        protected void FinalizeSync()
        {
            LocalDataManager.SetHolidaysFromSocialNetwork(ServiceName, Period, Items);
            SyncManager.Manager.SetIsAnyOperation(-1);
        }
    }
}
