using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Calendar.Services;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Calendar.Data.Models;

namespace Calendar.Mechanism
{
    public partial class HolidayCalendarBase
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

        public List<HolidayItem> HolidayItemCollection { get; private set; }
        public List<CheckBox> HolidayNameCollection { get; private set; }
        public int Weekend { get; private set; }

        public HolidayCalendarBase(int weekend)
        {
            this.Weekend = weekend;

            HolidayItemCollection = new List<HolidayItem>();
            HolidayNameCollection = new List<CheckBox>();

            SelectedDate = DateTime.Now;

            startPosition = 0;
            endPosition = 0;

            FillMonth();

            ReadHolidayXml();
            FillHolidaysList();
        }


        #region Standard holidays
        /// <summary>
        /// Source for ListView in Flyout
        /// </summary>
        /// <returns>Collection of holiday's categories</returns>
        public void FillHolidaysList()
        {            
            if (HolidayNameCollection.Count == 0)
            {
                var collect = LocalDataManager.GetCollectionFromSourceFile("categories");
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
                foreach (var p in LocalDataManager.SelectedCategories)
                {
                    if (p.Key.ToLower() == HolidayNameCollection[i].Tag.ToString().ToLower())
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
            HolidayItemCollection = new List<HolidayItem>();
            List<DocElement> tempCollection = new List<DocElement>();

            //looking for holidays from selected categories
            tempCollection = LocalDataManager.GetCollectionFromSourceFile(SelectedDate, "day");
            foreach (var x in tempCollection)
            {
                HolidayItemCollection.Add(new HolidayItem
                {
                    Day = Convert.ToInt32(x.Value.Attribute("date").Value),
                    HolidayName = x.Value.Attribute("name").Value,
                    HolidayTag = x.Key,
                    Background = new SolidColorBrush(Colors.Transparent),
                    FontSize = 20,
                    Height = 50
                });
            }

            //computationals
            tempCollection = LocalDataManager.GetCollectionFromSourceFile(SelectedDate, "computational");
            foreach (var x in tempCollection)
            {
                HolidayItemCollection.Add(new HolidayItem
                {
                    Day = ComputeHoliday(Convert.ToInt32(x.Value.Attributes().ElementAt(1).Value), Convert.ToInt32(x.Value.Attributes().ElementAt(2).Value)),
                    HolidayName = x.Value.Attributes().ElementAt(0).Value,
                    HolidayTag = x.Key,
                    Background = new SolidColorBrush(Colors.Transparent),
                    FontSize = 20,
                    Height = 50
                });
            }

            //movables
            tempCollection = LocalDataManager.GetCollectionFromSourceFile(SelectedDate, "movable");
            if (tempCollection != null)
                foreach (var x in tempCollection)
                {
                    HolidayItemCollection.Add(new HolidayItem
                    {
                        Day = Convert.ToInt32(x.Value.Attribute("date").Value),
                        HolidayName = x.Value.Attribute("name").Value,
                        HolidayTag = x.Key,
                        Background = new SolidColorBrush(Colors.Transparent),
                        FontSize = 20,
                        Height = 50
                    });
                }

            //personal
            string mine = LocalDataManager.Resource.GetString("MineAsTag");
            var list = LocalDataManager.GetCollectionFromSourceFile("persDate");
            foreach (var pers in list)
            {
                //get year
                int year = -1;
                if (!int.TryParse(pers.Attribute("year").Value, out year))
                    year = -1;

                //check month
                bool isCurrenMonth = (pers.Attribute("month").Value == SelectedDate.Month.ToString() ||
                                      pers.Attribute("month").Value == "0") ?
                                      true : false;

                if (isCurrenMonth && year != -1)
                    HolidayItemCollection.Add(new HolidayItem
                    {
                        Day = Convert.ToInt32(pers.Attribute("date").Value),
                        Year = year,
                        Month = Convert.ToInt32(pers.Attribute("month").Value),
                        HolidayName = pers.Attribute("name").Value,
                        HolidayTag = mine,
                        Background = new SolidColorBrush(Colors.Transparent),
                        FontSize = 20,
                        Height = 50
                    });
            }

            //services
            if (LocalDataManager.Services != null)
                foreach (var service in LocalDataManager.Services)
                {
                    var collection = LocalDataManager.GetCollectionFromSourceFile(service);
                    foreach (var holiday in collection)
                    {
                        int year = int.Parse(holiday.Attribute("year").Value);
                        int month = int.Parse(holiday.Attribute("month").Value);

                        if (year == SelectedDate.Year && month == SelectedDate.Month)
                            HolidayItemCollection.Add(new HolidayItem
                            {
                                Day = int.Parse(holiday.Attribute("date").Value),
                                Year = year,
                                Month = month,
                                HolidayName = holiday.Attribute("name").Value,
                                HolidayTag = mine,
                                Background = new SolidColorBrush(Colors.Transparent),
                                FontSize = 20,
                                Height = 50
                            });
                    }
                }

            HolidayItemCollection.Add(new HolidayItem
            {
                Day = 0,
                HolidayName = LocalDataManager.Resource.GetString("PersonalNote"),
                HolidayTag = LocalDataManager.Resource.GetString("MineAsTag"),
                Background = new SolidColorBrush(Colors.Transparent),
                FontSize = 20,
                Height = 50
            });
        }

        /// <summary>
        /// computes holidays from tag COMPUTATIONAL
        /// </summary>
        /// <param name="dow">day of week (from 0 to 6)</param>
        /// <param name="now">number of week (from 0 to 4 (or 6 if last week))</param>
        /// <param name="start">start position of this month in calenar (row*10+col)</param>
        /// <returns></returns>
        protected int ComputeHoliday(int dow, int now)
        {
            int a;
            if (now != 6)
            {
                int startDay = ((startPosition % 10) == 7) ? 0 : (int)(startPosition % 10);
                a = (int)(startPosition / 10) * 7 + now * 7 + dow + 1 - startDay;
                
                //if week starts from Sun, add 7 days
                if (Weekend == 0) return a + 7;
                return a;
            }
            //last week
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
        
    }
}
