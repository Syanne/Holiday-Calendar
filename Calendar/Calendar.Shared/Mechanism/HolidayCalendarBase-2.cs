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

            ReadHolidayXml();
            FillHolidaysList();
        }

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
                foreach (var p in LocalDataManager.GetSelectedCategoriesList())
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
            HolidayItemCollection = LocalDataManager.GetComposedData(SelectedDate, 0);
        }

        /// <summary>
        /// Next or previous month
        /// </summary>
        /// <param name="value">1 or -1</param>
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