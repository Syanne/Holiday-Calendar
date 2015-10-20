using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CalendarResources;
using Windows.UI.Xaml.Controls;

namespace Calendar.HolidayCalendarMechanism
{
    partial class HolidayCalendarBase
    {
        public int[] Month { get; private set; }

        public DateTime SelectedDate { get; set; }

        //start en end positions of the month (row*10+col)
        private int endPosition, startPosition;
        /// <summary>
        /// row*10+col of the first day in the month
        /// </summary>
        public int Start
        {
            get { return startPosition; }
            private set { startPosition = value; }
        }
        /// <summary>
        /// row*10+col of the last day in the month
        /// </summary>
        public int End
        {
            get { return endPosition; }
            private set { endPosition = value; }
        }

        public ObservableCollection<HolidayItem> HolidayItemCollection { get; private set; }
        public ObservableCollection<CheckBox> HolidayNameCollection { get; private set; }


        private char firstDay;

        public HolidayCalendarBase(char firstDay)
        {
            this.firstDay = firstDay;

            HolidayItemCollection = new ObservableCollection<HolidayItem>();
            HolidayNameCollection = new ObservableCollection<CheckBox>();

            SelectedDate = DateTime.Now;

            startPosition = 0;
            endPosition = 0;

            FillMonth();
        }

        public void AddDay(int day)
        {
            SelectedDate = SelectedDate.AddDays(day);
        }

        #region Standard holidays
        /// <summary>
        /// Source for ListView in Flyout
        /// </summary>
        /// <returns>Collection of holiday's categories</returns>
        public void FillHolidaysList()
        {
            var persCollection = CalendarResourcesManager.PersonalData.Root.Descendants("theme").Descendants("holiday");


            if (HolidayNameCollection.Count == 0)
            {
                var collect = CalendarResourcesManager.doc.Root.Descendants("month").ElementAt(0).Descendants("holiday");
                foreach (XElement x in collect)
                {
                    HolidayNameCollection.Add(new CheckBox
                    {
                        Content = x.FirstAttribute.Value,
                        Tag = x.LastAttribute.Value
                    });
                }
            }

            for (int i = 0; i < HolidayNameCollection.Count; i++)
                foreach (XElement p in persCollection)
                {
                    if (p.LastAttribute.Value == HolidayNameCollection[i].Tag.ToString())
                    {
                        HolidayNameCollection[i].IsChecked = true; break;
                    }
                    else HolidayNameCollection[i].IsChecked = false;
                }
        }
                
        /// <summary>
        /// get all holidays (personal and selected types)
        /// </summary>
        /// <param name="month">shown month</param>
        /// <param name="start">first day of month </param>
        /// <param name="year">shown year</param>
        public void ReadHolidayXml()
        {
            //collections of holidays
            var computational = CalendarResourcesManager.doc.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants("computational");
            var persCollection = CalendarResourcesManager.PersonalData.Root.Descendants("theme").Descendants("holiday");
            var movable = CalendarResourcesManager.doc.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants("movable");

            HolidayItemCollection = new ObservableCollection<HolidayItem>();

            //looking for holidays from selected categories
            foreach (XElement x in CalendarResourcesManager.doc.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants("day"))
            {
                foreach (XElement pers in persCollection)
                    if (x.FirstAttribute.Value != "" && x.Parent.FirstAttribute.Value == pers.FirstAttribute.Value)
                        HolidayItemCollection.Add(new HolidayItem
                        {
                            Date = Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                            HolidayName = x.Attributes().ElementAt(0).Value,
                            HolidayTag = pers.LastAttribute.Value
                        });
            }

            if(computational.Count() != 0)
            foreach (XElement x in computational)
            {
                foreach (XElement pers in persCollection)
                    if (x.FirstAttribute.Value != "" && x.Parent.FirstAttribute.Value == pers.FirstAttribute.Value)
                        HolidayItemCollection.Add(new HolidayItem
                        {
                            Date =
                                ComputeHoliday(Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                                Convert.ToInt32(x.Attributes().ElementAt(2).Value)),
                            HolidayName = x.Attributes().ElementAt(0).Value,
                            HolidayTag = pers.LastAttribute.Value
                        });
            }

            if (movable.Count() != 0)
                foreach (XElement x in movable)
                {
                        if (x.Attribute("year").Value == SelectedDate.Year.ToString())
                        {
                            HolidayItemCollection.Add(new HolidayItem
                            {
                                Date = Convert.ToInt32(x.Attribute("day").Value),
                                HolidayName = x.Attribute("name").Value,
                                HolidayTag = x.Parent.LastAttribute.Value
                            });
                        }
                }

            string mine = CalendarResourcesManager.resource.GetString("MineAsTag");
            foreach (XElement pers in CalendarResourcesManager.PersonalData.Root.Descendants("holidays").Descendants("persDate"))
                if (pers.Attribute("month").Value == SelectedDate.Month.ToString() && (pers.Attribute("year").Value == SelectedDate.Year.ToString() || pers.Attribute("year").Value == "0"))
                    HolidayItemCollection.Add(new HolidayItem 
                    { Date = Convert.ToInt32(pers.Attribute("date").Value), 
                        HolidayName = pers.Attribute("name").Value,
                      HolidayTag = mine
                    });

            HolidayItemCollection.Add(new HolidayItem { Date = 0,
                                                        HolidayName = CalendarResourcesManager.resource.GetString("PersonalNote"),
                                                        HolidayTag = CalendarResourcesManager.resource.GetString("MineAsTag")
            });
        }

        /// <summary>
        /// computes holidays from tag COMPUTATIONAL
        /// </summary>
        /// <param name="dow">day of week (from 0 to 6)</param>
        /// <param name="now">number of week (from 0 to 4)</param>
        /// <param name="start">start position of this month in calenar (row*10+col)</param>
        /// <returns></returns>
        protected int ComputeHoliday(int dow, int now)
        {
            int a;
            if (now != 6)
            {
                int startDay = ((startPosition % 10) == 7) ? 0 : (int)(startPosition % 10);
                a = (int)(startPosition / 10) * 7 + now * 7 + dow + 1 - startDay;
                return a;
            }
            else
            {
                a = (int)(endPosition / 7) * 7 - startPosition - 6;
                if (endPosition % 7 > dow) 
                    a += 7 + dow;
                else a += dow;
                return a;

            }
        }
        #endregion

        #region Personal holidays

        /// <summary>
        /// Save changes in file with personal data
        /// </summary>
        /// <param name="basicHolidays">selected kinds of holidays</param>
        public void WriteHolidayXml(List<string> basicHolidays)
        {
            //collect all descendants
            var collect = CalendarResourcesManager.PersonalData.Root.Descendants("theme").Descendants("holiday");
            if (collect == null) throw new Exception();
            int n = collect.Count();

            //remove all nodes
            CalendarResourcesManager.PersonalData.Root.Descendants("theme").Descendants().Remove();

            //add all nodes
            for (int i = 0; i < basicHolidays.Count(); i += 2)
            {
                using (XmlWriter writer = CalendarResourcesManager.PersonalData.Root.Descendants().ElementAt(0).CreateWriter())
                {
                    CalendarResourcesManager.WriteNode(writer, basicHolidays.ElementAt(i), basicHolidays.ElementAt(i + 1));
                }
            }
            CalendarResourcesManager.SaveDocumentAsync();
        }
        #endregion
    }
}
