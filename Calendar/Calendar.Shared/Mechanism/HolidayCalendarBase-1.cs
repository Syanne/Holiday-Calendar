using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using CalendarResources;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Calendar.Mechanism
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
        private int firstDay;

        public HolidayCalendarBase(int firstDay)
        {
            this.firstDay = firstDay;

            HolidayItemCollection = new ObservableCollection<HolidayItem>();
            HolidayNameCollection = new ObservableCollection<CheckBox>();

            SelectedDate = DateTime.Now;

            startPosition = 0;
            endPosition = 0;

            FillMonth();
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
                        Content = x.FirstAttribute.Value.ToLower(),
                        Tag = x.LastAttribute.Value,
                        FontSize = 18,
                        Padding = new Windows.UI.Xaml.Thickness(10, 0, 10, 10)
                    });
                }
            }

            for (int i = 0; i < HolidayNameCollection.Count; i++)
                foreach (XElement p in persCollection)
                {
                    if (p.LastAttribute.Value.ToLower() == HolidayNameCollection[i].Tag.ToString().ToLower())
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
            var computational = CalendarResourcesManager.doc.Root.Descendants("month").
                ElementAt(SelectedDate.Month - 1).Descendants("computational");
            var persCollection = CalendarResourcesManager.PersonalData.
                Root.Descendants("theme").Descendants("holiday");
            var movable = CalendarResourcesManager.doc.Root.Descendants("month").
                ElementAt(SelectedDate.Month - 1).Descendants("movable");

            HolidayItemCollection = new ObservableCollection<HolidayItem>();

            //looking for holidays from selected categories
            foreach (XElement x in CalendarResourcesManager.doc.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants("day"))
            {
                foreach (XElement pers in persCollection)
                    if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                        HolidayItemCollection.Add(new HolidayItem
                        {
                            Day = Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                            HolidayName = x.Attributes().ElementAt(0).Value,
                            HolidayTag = pers.LastAttribute.Value,
                            Background = new SolidColorBrush(Colors.Transparent),
                            FontSize = 20, 
                            Height = 50
                        });
            }

            if(computational.Count() != 0)
                foreach (XElement pers in persCollection)
                    foreach (XElement x in computational)
                    {
                        if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                            HolidayItemCollection.Add(new HolidayItem
                            {
                                Day =
                                    ComputeHoliday(Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                                    Convert.ToInt32(x.Attributes().ElementAt(2).Value)),
                                HolidayName = x.Attributes().ElementAt(0).Value,
                                HolidayTag = pers.LastAttribute.Value,
                                Background = new SolidColorBrush(Colors.Transparent),
                                FontSize = 20,
                                Height = 50
                            });
                    }

            if (movable.Count() != 0)
                foreach (XElement x in movable)
                {
                        if (x.Attribute("year").Value == SelectedDate.Year.ToString())
                        {
                            HolidayItemCollection.Add(new HolidayItem
                            {
                                Day = Convert.ToInt32(x.Attribute("day").Value),
                                HolidayName = x.Attribute("name").Value,
                                HolidayTag = x.Parent.LastAttribute.Value,
                                Background = new SolidColorBrush(Colors.Transparent),
                                FontSize = 20,
                                Height = 50
                            });
                        }
                }

            string mine = CalendarResourcesManager.resource.GetString("MineAsTag");
            foreach (XElement pers in CalendarResourcesManager.PersonalData.Root.Descendants("holidays").Descendants("persDate"))
                if (pers.Attribute("month").Value == SelectedDate.Month.ToString() && (pers.Attribute("year").Value == SelectedDate.Year.ToString() || pers.Attribute("year").Value == "0"))
                    HolidayItemCollection.Add(new HolidayItem
                    {
                        Day = Convert.ToInt32(pers.Attribute("date").Value),
                        Year = Convert.ToInt32(pers.LastAttribute.Value),
                        HolidayName = pers.Attribute("name").Value,
                        HolidayTag = mine,
                        Background = new SolidColorBrush(Colors.Transparent),
                        FontSize = 20,
                        Height = 50
                    });

            HolidayItemCollection.Add(new HolidayItem { Day = 0,
                                                        HolidayName = CalendarResourcesManager.resource.GetString("PersonalNote"),
                                                        HolidayTag = CalendarResourcesManager.resource.GetString("MineAsTag"),
                                                        Background = new SolidColorBrush(Colors.Transparent),
                                                        FontSize = 20,
                                                        Height = 50
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
