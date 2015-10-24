using Calendar.HolidayCalendarMechanism;
using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using CalendarResources;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Deselect previously selected gvi in calGrid
        /// </summary>
        GridViewItem gviPrev { get; set; }

        /// <summary>
        /// Selected holidays ( <= 3 )
        /// </summary>
        ListViewItem SelectedHolidayType { get; set; }

        /// <summary>
        /// Mechanism for calendar designing
        /// </summary>
        HolidayCalendarBase calBase { get; set; }

        /// <summary>
        /// Text in the note
        /// </summary>
        StringBuilder startText { get; set; }

        /// <summary>
        /// first day of week
        /// </summary>
        int fDay { get; set; }

        void PagePreLoader()
        {
            SelectedHolidayType = All;

            fDay = 5;
            gviPrev = new GridViewItem() { Content = DateTime.Now.Day };
            startText = new StringBuilder(50);
            calBase = new HolidayCalendarBase(fDay);

            //Weekend styles
            SetWeekends();

            FillCalendar();
        }

        /// <summary>
        /// Update list of holidays for chosen day
        /// </summary>
        /// <param name="day">Chosen day</param>
        private void UpdateNoteList()
        {
#if WINDOWS_PHONE_APP
            ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("ddd d MMM yyyy");
#else
            ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("D");
#endif
            //fill list of holidays
            if (SelectedHolidayType.Content.ToString() == All.Content.ToString())
                noteList.ItemsSource = calBase.HolidayItemCollection.Where(hi =>
                    hi.Date == calBase.SelectedDate.Day || hi.Date == 0);

            else
                noteList.ItemsSource = calBase.HolidayItemCollection.
                                      Where(hi => (hi.Date == calBase.SelectedDate.Day || hi.Date == 0) &&
                                      (hi.HolidayTag == SelectedHolidayType.Content.ToString()));
        }
        
        #region Fill calendar

        /// <summary>
        /// Color of weekday name items
        /// </summary>
        void SetWeekends()
        {
            Brush day = (Brush)Application.Current.Resources["DayBg"];
            foreach (var item in weekDayNames.Items)
                (item as GridViewItem).Background = day;

            (weekDayNames.Items[fDay] as GridViewItem).Background = (Brush)Application.Current.Resources["WeekendBg"];
            (weekDayNames.Items[6] as GridViewItem).Background = (Brush)Application.Current.Resources["WeekendBg"];
        }

        /// <summary>
        /// Fill calendar (days only)
        /// </summary>
        public void FillCalendar()
        {
            monthNameButton.Content = calBase.SelectedDate.Date.ToString("MMMM yyyy");

            //from first to last day in a month
            int jj = (calBase.Start == 7) ? calBase.Start : 0;

            //styles and brushes
            Style adjStyle = (Style)this.Resources["AdjMonthStyle"];
            Style weekndStyle = (Style)this.Resources["WeekendStyle"];
            Style dayStyle = (Style)this.Resources["ThisMonthStyle"];
            Brush wBgBrush = (Brush)Application.Current.Resources["WeekendBg"];
            Brush mBgBrush = (Brush)Application.Current.Resources["MainFg"];
            Brush DayBg = (Brush)Application.Current.Resources["DayBg"];
            Brush DayFg = (Brush)Application.Current.Resources["DayFg"];
                        
            //fill calendar
            ObservableCollection<GridViewItem> gviCalSource = new ObservableCollection<GridViewItem>();
            GridViewItem gvItem;
            
            for (int i = 0; i < 42; i++)
            {
                gvItem = new GridViewItem()
                {
                    Content = calBase.Month[i]
                };
                gvItem.Tapped += gvItem_Tapped;

                //adjMonths
                if (i < calBase.Start || i >= calBase.End)
                    gvItem.Style = adjStyle;
                //weekends
                else if (i - jj == fDay || i - jj == 6)
                {
                    gvItem.Style = weekndStyle;
                    gvItem.Background = wBgBrush;
                    gvItem.Foreground = mBgBrush;
                    if (i - jj == 6) jj += 7;
                }
                else
                {
                    gvItem.Style = dayStyle;
                    gvItem.Background = DayBg;
                    gvItem.Foreground = DayFg;
                }

                gviCalSource.Add(gvItem);
            }
            calGrid.ItemsSource = gviCalSource;            
        }

        /// <summary>
        /// Day Styles (weekend, today, holiday)
        /// </summary>
        private void MarkHolidays()
        {
            IEnumerable<HolidayItem> holItemSource;
            if (SelectedHolidayType.Content.ToString() == All.Content.ToString())
                holItemSource = calBase.HolidayItemCollection;
            else holItemSource = calBase.HolidayItemCollection.Where(
                h => h.HolidayTag == SelectedHolidayType.Content.ToString());
            
            SolidColorBrush standard = new SolidColorBrush(Colors.WhiteSmoke);
            SolidColorBrush hol = new SolidColorBrush(Color.FromArgb(255, 48, 48, 48));
                
            //holidays
            for (int i = calBase.Start; i < calBase.End; i++)
            {
                int current = Convert.ToInt32((calGrid.Items[i] as GridViewItem).Content);
               
                if (holItemSource.Count(item => item.Date == current) != 0)
                    (calGrid.Items[i] as GridViewItem).Foreground = standard;
                else (calGrid.Items[i] as GridViewItem).Foreground = hol;
            }

            //today
            if (calBase.SelectedDate.Month == DateTime.Now.Month && calBase.SelectedDate.Year == DateTime.Now.Year)
            {
                Brush brush = (Brush)Application.Current.Resources["DayBg"];
                int x = calBase.Start - 1 + DateTime.Now.Day;
                (calGrid.Items[x] as GridViewItem).Style = (Style)this.Resources["TodayStyle"];
                (calGrid.Items[x] as GridViewItem).Background = new SolidColorBrush(Color.FromArgb(200, 55, 55, 55));
                (calGrid.Items[x] as GridViewItem).Foreground = brush;
                (calGrid.Items[x] as GridViewItem).BorderBrush = brush;
            }
        }

        /// <summary>
        /// fill month list
        /// </summary>
        private void InitializeDecades()
        {
            DateTime dt = new DateTime(2000, 1, 1);

            //decade items
            var newType = new { Content = dt.Date.ToString("MMM"), Tag = 1 };
            var decadeList = (new[] { newType }).ToList();

            //fill the list
            for (int i = 2; i <= 12; i++)
            {
                dt = new DateTime(2000, i, 1);
                decadeList.Add(new { Content = dt.Date.ToString("MMM"), Tag = i });
            }

            gvDecades.ItemsSource = decadeList;
        }

#endregion
        
        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("Назад"));
            var command = await dial.ShowAsync();
        }
    }

}
