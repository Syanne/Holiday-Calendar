﻿using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Calendar.Models
{
    /// <summary>
    /// all chosen holidays (from at least 3 categories of holidays)
    /// </summary>
    public struct HolidayItem
    {
        public int Date { get; set; }
        public string HolidayName { get; set; }
        public string HolidayTag { get; set; }
    }


    /// <summary>
    /// Days in Week
    /// </summary>
    public class Group
    {
        /// <summary>
        /// represents the week
        /// </summary>
        public ObservableCollection<CalendarItem> smallCollection { get; set; }
        public Group()
        {
            smallCollection = new ObservableCollection<CalendarItem>();
        }
    }

    /// <summary>
    /// represents the day (calendar item)
    /// </summary>
    public class CalendarItem
    {
        public Style Style { get; set; }
        public int Content { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
        public Brush BorderBrush { get; set; }
        public Thickness Thickness { get; set; }
    }

    public class LvItem
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
    }

}