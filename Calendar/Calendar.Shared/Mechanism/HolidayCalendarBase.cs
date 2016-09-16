using Calendar.Data.Models;
using Calendar.Services;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;

namespace Calendar.Mechanism
{
    public partial class HolidayCalendarBase
    {
        public int[] Month { get; private set; }

        public DateTime SelectedDate { get; set; }

        public List<HolidayItem> HolidayItemCollection { get; private set; }
        public List<CheckBox> HolidayNameCollection { get; private set; }

        public HolidayCalendarBase()
        {
            HolidayItemCollection = new List<HolidayItem>();
            HolidayNameCollection = new List<CheckBox>();

            SelectedDate = DateTime.Now;
            
            FillMonth();
            RefreshBottomPanelAndCalendar(null);
        }

        /// <summary>
        /// Source for ListView in Flyout
        /// </summary>
        /// <param name="collection">null - if empty, else - need to refresh</param>
        /// <returns>Collection of holiday's categories</returns>
        public void RefreshBottomPanelAndCalendar(IEnumerable<object> collection)
        {
            //read collection from file
            if (collection == null)
            {
                var dict = LocalDataManager.GetCollectionOfCategories();
                foreach (var x in dict)
                {
                    HolidayNameCollection.Add(new CheckBox
                    {
                        Content = x.Value,
                        Tag = x.Key,
                        FontSize = 18,
                        Padding = new Windows.UI.Xaml.Thickness(10, 0, 10, 10)
                    });
                }

                var selectedCats = LocalDataManager.GetSelectedCategoriesList();
                for (int i = 0; i < HolidayNameCollection.Count; i++)
                {
                    if (selectedCats.ContainsKey(HolidayNameCollection[i].Tag.ToString().ToLower()))
                        HolidayNameCollection[i].IsChecked = true;
                    else HolidayNameCollection[i].IsChecked = false;
                }
                    
            }
            //refresh application and data in file
            else
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                HolidayNameCollection = new List<CheckBox>();

                foreach (var element in collection)
                {
                    if (element is CheckBox)
                    {
                        var current = element as CheckBox;
                        HolidayNameCollection.Add(current);

                        if (current.IsChecked == true)
                        {
                            dictionary.Add(current.Tag.ToString(), current.Content.ToString());
                        }
                    }
                }

                LocalDataManager.WriteHolidayXml(dictionary);

            }
            //refresh collection
            RefreshDataCollection();
        }

        /// <summary>
        /// get all holidays (personal and selected types)
        /// </summary>
        public void RefreshDataCollection()
        {
            HolidayItemCollection = LocalDataManager.GetComposedData(SelectedDate, 0);
        }

        /// <summary>
        /// Next or previous month
        /// </summary>
        /// <param name="value">1 for next, -1 for previous</param>
        public void Skip(int value)
        {
            SelectedDate = SelectedDate.AddMonths(value);
            FillMonth();
        }

        /// <summary>
        /// Go to selected M and Y
        /// </summary>
        /// <param name="month">month</param>
        /// <param name="year">year</param>
        public void Skip(int day, int month, int year)
        {
            SelectedDate = new DateTime(year, month, day);
            FillMonth();
        }

        /// <summary>
        /// fills array of days in month
        /// </summary>
        /// <param name="monthNumber">selected month</param>
        /// <param name="yearNumber">selected year</param>
        public void FillMonth()
        {
            Month = new int[42];
            
            int days = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);
            //previous
            int prevMonth = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.AddMonths(-1).Month);
            LocalDataManager.ResetMonth(SelectedDate, 0);

            for (int i = 41; i >= 0; i--)
            {
                if (i >= LocalDataManager.End) Month[i] = i - LocalDataManager.End + 1; //next month
                else if (i < LocalDataManager.End && i >= LocalDataManager.Start) //current
                {
                    Month[i] = days;
                    days--;
                }
                else //previous
                {
                    Month[i] = prevMonth;
                    prevMonth--;
                }
            }
        }
    }
}