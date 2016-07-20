using Calendar.Models;
using CalendarResources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Popups;

namespace Calendar.SocialNetworkConnector
{
    abstract class BaseConnector
    {
        protected List<ItemBase> Items;
        protected virtual DateTime DateStart { get; set; }
        protected virtual DateTime DateEnd { get; set; }

        public BaseConnector(List<ItemBase> items)
        {
            Items = items;
        }

        public BaseConnector(DateTime dateStart, DateTime dateEnd, ref List<ItemBase> items)
        {
            DateStart = dateStart;
            DateEnd = dateEnd;
            this.Items = items;
        }

        public abstract Task GetHolidayList();

        protected async void Message()
        {
            var dial = new MessageDialog(Items.Count.ToString(),"OK");

            dial.Commands.Add(new UICommand("OK", cmd=> 
            {
                //DataManager.PersonalData.Root.Add(new XNode());
            }));

            var command = await dial.ShowAsync();
        }
    }
}
