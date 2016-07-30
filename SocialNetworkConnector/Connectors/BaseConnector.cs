using SocialNetworkConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace SocialNetworkConnector.Connectors
{
    public abstract class BaseConnector
    {
        /// <summary>
        /// unified date format for all descentants
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";
        /// <summary>
        /// unified separator for date split
        /// </summary>
        public const char DateSeparator = '-';
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

        public BaseConnector(int period)
        {
            this.Items = new List<ItemBase>();
        }

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
            //DataManager.SetHolidaysFromSocialNetwork(ServiceName, Period, Items);
            SyncManager.Manager.SetIsAnyOperation(-1);
        }
    }
}
