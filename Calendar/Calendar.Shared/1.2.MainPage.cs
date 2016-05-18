﻿using Calendar.Mechanism;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Resources;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        #if !WINDOWS_PHONE_APP
        WindowsStandardClass sizeCorrection;
        #endif

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
        int Weekend { get; set; }

        void PagePreLoader()
        {
            SelectedHolidayType = (HolidayList.Items[0] as ListViewItem);

            try
            {
                string theDay = ResourceLoader.GetForCurrentView("Resources").GetString("Weekend");
                Weekend = Convert.ToInt32(theDay);
            }
            catch
            {
                Weekend = 5;
            }
            startText = new StringBuilder(50);
            calBase = new HolidayCalendarBase(Weekend);


            FillCalendar();
            try
            {
                Windows.ApplicationModel.Store.LicenseInformation license = Windows.ApplicationModel.Store.CurrentApp.LicenseInformation;
                if (license.ProductLicenses["allstuff1"].IsActive)
                {
                    adControl.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            finally
            {
                gviPrev = calGrid.Items.ElementAt(calBase.SelectedDate.Day + calBase.Start - 1) as GridViewItem;                
            }

        }

        /// <summary>
        /// Update list of holidays for chosen day
        /// </summary>
        /// <param name="day">Chosen day</param>
        private void UpdateNoteList()
        {
#if WINDOWS_PHONE_APP
            ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("d").ToLower();
#else
            ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("D");
#endif
            
            //fill list of holidays
            if (SelectedHolidayType == (HolidayList.Items[0] as ListViewItem))
                noteList.ItemsSource = calBase.HolidayItemCollection.Where(hi =>
                    hi.Day == calBase.SelectedDate.Day || hi.Day == 0).
                    Distinct(new HolidayNameComparer());

            else noteList.ItemsSource = calBase.HolidayItemCollection.
                                      Where(hi => (hi.Day == calBase.SelectedDate.Day || hi.Day == 0) &&
                                      (hi.HolidayTag == SelectedHolidayType.Content.ToString()));

            //background color of every note
            NotesBackground();
        }

        /// <summary>
        /// alternating backgrounds for notes
        /// </summary>
        private void NotesBackground()
        {
            var transp = new SolidColorBrush(Colors.Transparent);
            double fSize = (Window.Current.Bounds.Height / 36 > 30) ? 30 : Window.Current.Bounds.Height / 36;

            for (int i = 0; i < noteList.Items.Count; i++)
            {
                if (i % 2 == 0)
                    (noteList.Items[i] as HolidayItem).Background = DarkNoteBackground;
                else (noteList.Items[i] as HolidayItem).Background = TransparentBrush;
            #if !WINDOWS_PHONE_APP
                (noteList.Items[i] as HolidayItem).FontSize = fSize;
            #endif
            }
        }
        
        #region Fill calendar

        /// <summary>
        /// Fill calendar (days only)
        /// </summary>
        public void FillCalendar()
        {
#if WINDOWS_PHONE_APP
            StandardClass standard = new StandardClass();
#else
            //a flag for initial sizing
            bool flag = false;
            if (sizeCorrection == null)
            {
                sizeCorrection = new WindowsStandardClass();
                flag = sizeCorrection.Count(Window.Current.Bounds.Height);
            }
#endif
            monthNameButton.Content = calBase.SelectedDate.Date.ToString("MMMM yyyy").ToLower();

            //from first to last day in a month
            int jj = (calBase.Start == 7) ? calBase.Start : 0;

            //styles and brushes for day-items
            Style adjStyle = (Style)this.Resources["AdjMonthStyle"];
            Style dayStyle = (Style)this.Resources["ThisMonthStyle"];
            Brush DayFg = new SolidColorBrush(Colors.White);

            //fill calendar
            ObservableCollection<GridViewItem> gviCalSource = new ObservableCollection<GridViewItem>();
            GridViewItem gvItem;
            
            for (int i = 0; i < 42; i++)
            {
                //a day
                gvItem = new GridViewItem()
                {
                    Content = calBase.Month[i],

                    //sizing
                #if WINDOWS_PHONE_APP
                    Height = standard.ItemSizeCorrector,
                    Width = standard.ItemSizeCorrector,
                    FontSize = standard.ItemFontSizeCorrector
                #else
                    Height = sizeCorrection.ItemSizeCorrector,
                    Width = sizeCorrection.ItemSizeCorrector,
                    FontSize = sizeCorrection.ItemFontSizeCorrector,
                    Padding = new Thickness(0, sizeCorrection.ItemSizeCorrector / 5, 0, sizeCorrection.ItemSizeCorrector / 5)
                #endif
                };

#if WINDOWS_PHONE_APP
                gvItem.Tapped += gvItem_Tapped;
#else
                gvItem.Tapped += Day_Tapped;
#endif
                //adjMonths
                if (i < calBase.Start || i >= calBase.End)
                    gvItem.Style = adjStyle;
                else
                {
                    gvItem.Style = dayStyle;
                    gvItem.Foreground = DayFg;
                }

                gviCalSource.Add(gvItem);
            }
            calGrid.ItemsSource = gviCalSource;

#if !WINDOWS_PHONE_APP
            if (flag)
                sizeCorrection = null;
#endif
        }

        /// <summary>
        /// Day Styles (today or holiday)
        /// </summary>
        private void MarkHolidays()
        {
            //collection of HolidayItems
            IEnumerable<HolidayItem> holItemSource;
            if (SelectedHolidayType == (HolidayList.Items[0] as ListViewItem))
                holItemSource = calBase.HolidayItemCollection;
            else holItemSource = calBase.HolidayItemCollection.Where(
                h => h.HolidayTag.ToLower() == SelectedHolidayType.Content.ToString().ToLower());

            SolidColorBrush standard = new SolidColorBrush(Colors.White);
            SolidColorBrush hol = (SolidColorBrush)Application.Current.Resources["AdditionalColor"];
                
            //holidays
            for (int i = calBase.Start; i < calBase.End; i++)
            {
                int current = Convert.ToInt32((calGrid.Items[i] as GridViewItem).Content);
               
                if (holItemSource.Count(item => item.Day == current) != 0)
                    (calGrid.Items[i] as GridViewItem).Foreground = hol;
                else (calGrid.Items[i] as GridViewItem).Foreground = standard;
            }

            //today
            if (calBase.SelectedDate.Month == DateTime.Now.Month && calBase.SelectedDate.Year == DateTime.Now.Year)
            {
                int x = calBase.Start - 1 + DateTime.Now.Day;
                (calGrid.Items[x] as GridViewItem).Style = (Style)this.Resources["TodayStyle"];

                if(gviPrev != null)
                if (gviPrev.Content.ToString() == (calGrid.Items[x] as GridViewItem).Content.ToString())
                {
                    (calGrid.Items[x] as GridViewItem).BorderThickness = new Thickness(3);
                    (calGrid.Items[x] as GridViewItem).BorderBrush = (calGrid.Items[x] as GridViewItem).Foreground;
                }
            }
            if(gviPrev != null)
            gviPrev.BorderBrush = gviPrev.Foreground;
        }

        /// <summary>
        /// fill month list
        /// </summary>
        private void InitializeDecades()
        {
            DateTime dt = new DateTime(2000, 1, 1);            
            var decadeList = new List<GridViewItem>();

            //fill the list
            for (int i = 1; i <= 12; i++)
            {
                dt = new DateTime(2000, i, 1);
                decadeList.Add( new GridViewItem()
                {
                    Content = dt.Date.ToString("MMM"),
                    Tag = i,
                    //sizing
#if WINDOWS_PHONE_APP
                    Height = Window.Current.Bounds.Width / 4 - Window.Current.Bounds.Width / 16,
                    Width = Window.Current.Bounds.Width / 4,
                    FontSize = Window.Current.Bounds.Width / 16,
                    Margin = new Thickness(5)
#else
                    Height = sizeCorrection.DecadeHeightCorrector,
                    Width = sizeCorrection.DecadeWidthCorrector,
                    FontSize = sizeCorrection.ItemFontSizeCorrector,
#endif
                });
#if WINDOWS_PHONE_APP
                decadeList.ElementAt(i - 1).Tapped += m1_Tapped;
#else
                decadeList.ElementAt(i - 1).Tapped += DecadeGridItem_Tapped;
#endif
            }
            gvDecades.ItemsSource = decadeList;
        }

#endregion
        /// <summary>
        /// alert
        /// </summary>
        /// <param name="text">message</param>
        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("Назад"));
            var command = await dial.ShowAsync();
        }

        /// <summary>
        /// The message about a new version
        /// </summary>
        private async void NewVersionMessage()
        {
            var dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString("msgBody"),
               ResourceLoader.GetForCurrentView("Resources").GetString("msgTitle"));

            dial.Commands.Add(new UICommand("OK"));
            
            var command = await dial.ShowAsync();
        }
    }
}
