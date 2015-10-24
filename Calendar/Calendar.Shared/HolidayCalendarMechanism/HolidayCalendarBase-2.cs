using System;
using Windows.UI.Popups;

namespace Calendar.HolidayCalendarMechanism
{
    partial class HolidayCalendarBase 
    {
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

            int weekDay = FirstDay();
            int days = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);
            //previous
            int prevMonth = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.AddMonths(-1).Month);

            //start position for this month
            int ii = (weekDay == 0) ? 1 : 0;
            startPosition = (ii * 7) + weekDay;
            endPosition = days + startPosition;

            for (int i = 41; i >= 0; i--)
            {
                if (i >= endPosition) Month[i] = i - endPosition + 1; //next month
                else if (i < endPosition && i >= startPosition) //current
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

        /// <summary>
        /// Find a day, that starts selected month
        /// </summary>
        /// <param name="monthNumber">selected month</param>
        /// <param name="yearNumber">selected year</param>
        /// <returns>1th of {selected month} weekday</returns>
        public int FirstDay()
        {
            //start from the first of {selected month}
            int day = 1;
            int a, y, m, R;
            a = (14 - SelectedDate.Month) / 12;
            y = SelectedDate.Year - a;
            m = SelectedDate.Month + 12 * a - 2;
            R = 7000 + (day + y + y / 4 - y / 100 + y / 400 + (31 * m) / 12);
            R %= 7;

            if (firstDay == 0) return R;
            else return R = (R > 0) ? (R - 1) : 6;
        }
    }
}
