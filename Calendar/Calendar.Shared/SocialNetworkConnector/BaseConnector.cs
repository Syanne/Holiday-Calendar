using Calendar.Models;
using CalendarResources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.UI.Popups;

namespace Calendar.SocialNetworkConnector
{
    abstract class BaseConnector
    {
        protected List<ItemBase> Items { get; set; }
        protected int Period { get; set; }
        protected virtual DateTime DateStart { get; set; }
        protected virtual DateTime DateEnd { get; set; }
        protected abstract string ServiceName { get; }  

        public BaseConnector(int period)
        {
            this.Items = new List<ItemBase>();
        }

        public BaseConnector(DateTime dateStart, int period)
        {
            DateStart = dateStart;
            Period = period;
            DateEnd = dateStart.Date.AddDays(period);
            this.Items = new List<ItemBase>();
        }

        public abstract Task GetHolidayList();

        protected async void Message(string serviceName, int period)
        {
            var dial = new MessageDialog(Items.Count.ToString(),"OK");

            dial.Commands.Add(new UICommand("OK", cmd=> 
            {
                DataManager.SetHolidaysFromSocialNetwork(serviceName, period, Items);
                SyncManager.Manager.SetIsAnyOperation(-1, this.GetType());
            }));

            var command = await dial.ShowAsync();
        }
    }
}
