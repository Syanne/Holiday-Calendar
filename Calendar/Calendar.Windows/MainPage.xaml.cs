﻿using CalendarResources;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.Globalization;
using Calendar.Models;
using Calendar.SocialNetworkConnector;

namespace Calendar
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            PagePreLoader();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PageLoadedController();
        }

        #region Calendar controls
        private void butNext_Click(object sender, RoutedEventArgs e)
        {
            ArrowButtonController(1);
        }
        
        private void butPrev_Click(object sender, RoutedEventArgs e)
        {
            ArrowButtonController(-1);
        }
        private void Day_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DayController(sender);

            //test
            int period = 0;
            DateTime date = DateTime.Now;
            try
            {
                period = int.Parse(DataManager.PersonalData.Root.Element("google").Attribute("period").Value);
                var array = DataManager.PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(BaseConnector.DateSeparator);
                date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
            }
            catch
            {
                period = 7;
                date = DateTime.Now;

            }
            finally
            {
                if (date.Day >= DateTime.Now.Day && date.Month >= DateTime.Now.Month && date.Year >= DateTime.Now.Year)
                {
                    SyncManager.Manager.AddService("google", DateTime.Now, period);
                }
            }
            
        }

        private void GoToDateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DatePickerDp.Date.Month != DataManager.calBase.SelectedDate.Month ||
                DatePickerDp.Date.Year != DataManager.calBase.SelectedDate.Year)
            {
                ChangeDate(1, DatePickerDp.Date.Month, DatePickerDp.Date.Year);
                DatePickerDp.Date = DateTimeOffset.Now;
            }
        }
        
        private void monthNameButton_Click(object sender, RoutedEventArgs e)
        {
            MonthController();
        }

        private void Decade_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DataManager.calBase.Skip(1, Convert.ToInt32((sender as GridViewItem).Tag), DataManager.calBase.SelectedDate.Year);

            //Shows month and year in the top of calGrid
            monthNameButton.Content = DataManager.calBase.SelectedDate.ToString("MMMM yyyy");

            calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;
            gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            FillCalendar();
            MarkHolidays();
        }

        private void DecadeGridItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DecadeItemController(sender);
        }
        #endregion

        #region Kind of Holiday menu & flyout
        /// <summary>
        /// Saves selected themes in xml-file
        /// </summary>
        /// <param name="sender">button in Flyout</param>
        /// <param name="e">Clicked</param>
        private void btnHolidays_Click(object sender, RoutedEventArgs e)
        {
            SaveHolidayTypes();
        }

        /// <summary>
        /// Select type of holidays in the calendar
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">Clicked</param>
        private void GridViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(HolidayList as FrameworkElement);

            listOfHolidays.ItemsSource = DataManager.calBase.HolidayNameCollection;
        }


        private void HolidayFlyout_Opened(object sender, object e)
        {
            mainBg.Opacity = 0.3;
        }
        private void HolidayFlyout_Closed(object sender, object e)
        {
            mainBg.Opacity = 1;
        }

        private void HolidayFlyoutCancel_Click(object sender, RoutedEventArgs e)
        {
            HolidayFlyout.Hide();
        }

        private void holTypes_Tapped(object sender, TappedRoutedEventArgs e)
        {
            HolidayTypesController(sender as ListViewItem);
        }
        #endregion

        #region Notes
        private void note_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NoteController(sender);
        }

        private void addNote_Click(object sender, RoutedEventArgs e)
        {
            AddNoteController();
        }

        private void delNote_Click(object sender, RoutedEventArgs e)
        {
            DeleteNoteController();
        }

        private void changeNote_Click(object sender, RoutedEventArgs e)
        {
            ChangeNoteController();
        }

        private void AddNoteFlyout_Opened(object sender, object e)
        {
            mainBg.Opacity = 0.3;
            addNotetb.Focus(FocusState.Keyboard);
        }

        private void AddNoteFlyout_Closed(object sender, object e)
        {
            mainBg.Opacity = 1;
        }

        private void AddNoteFlyout_Opening(object sender, object e)
        {
            if (addNotetb.Text == "")
            {
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void reclineButton_Click(object sender, RoutedEventArgs e)
        {
            AddNoteFlyout.Hide();
        }
        #endregion

        #region AppBar

        private async void assesBtn_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("ms-windows-store:PDP?PFN=36856Syanne.29333A1B8D628_x48427g2pbxee");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private async void testMeBtn_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("ms-windows-store:PDP?PFN=36856Syanne.TestMe_x48427g2pbxee");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }        

        private void settingAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }
        #endregion

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }
        void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            ShowHide();
        }

        /// <summary>
        /// Changing property Height for gvMain and choosing, what to show -list or grid
        /// </summary>
        private void ShowHide()
        {
            if (sizeCorrection == null)
                sizeCorrection = new WindowsStandardClass();

            if (sizeCorrection.Count(Window.Current.Bounds.Height))
            {
                //days
                for (int i = 0; i < calGrid.Items.Count; i++)
                {
                    (calGrid.Items[i] as GridViewItem).Height = sizeCorrection.ItemSizeCorrector;
                    (calGrid.Items[i] as GridViewItem).Width = sizeCorrection.ItemSizeCorrector;
                    (calGrid.Items[i] as GridViewItem).FontSize = sizeCorrection.ItemFontSizeCorrector;

                    //holiday panel's items
                    if (i < HolidayList.Items.Count)
                    {
                        (HolidayList.Items[i] as ListViewItem).Height = sizeCorrection.ItemSizeCorrector;
                        (HolidayList.Items[i] as ListViewItem).Width = sizeCorrection.ItemSizeCorrector;
                        (HolidayList.Items[i] as ListViewItem).FontSize = sizeCorrection.ItemSizeCorrector / 3;
                    }

                    //weekdays
                    if (i < weekDayNames.Items.Count)
                    {
                        (weekDayNames.Items[i] as GridViewItem).Height = sizeCorrection.ItemSizeCorrector;
                        (weekDayNames.Items[i] as GridViewItem).Width = sizeCorrection.ItemSizeCorrector;
                        (weekDayNames.Items[i] as GridViewItem).FontSize = sizeCorrection.ItemSizeCorrector / 3;
                    }

                    //decade's items
                    if (gvDecades.Items.Count > 0 && i < 12)
                    {
                        (gvDecades.Items[i] as GridViewItem).Height = sizeCorrection.DecadeHeightCorrector;
                        (gvDecades.Items[i] as GridViewItem).Width = sizeCorrection.DecadeWidthCorrector;
                        (gvDecades.Items[i] as GridViewItem).FontSize = sizeCorrection.ItemFontSizeCorrector;
                    }
                }
                
                monthTopString.Width = sizeCorrection.MonthTopStringWidth;
                monthTopString.Height = sizeCorrection.ItemSizeCorrector;
                monthTopString.Margin = new Thickness(0, sizeCorrection.ItemSizeCorrector/2, 0, 0);
                monthNameButton.FontSize = sizeCorrection.ItemSizeCorrector / 3;
                
                calGrid.Width = sizeCorrection.MonthTopStringWidth;
                calGrid.Height = calGrid.Width;
                gvDecades.Width = sizeCorrection.MonthTopStringWidth;
                
                //NOTES PANEL---------------------------------------------------------
                HolidayTitle.FontSize = sizeCorrection.ItemSizeCorrector / 3;
                HolidayList.Height = sizeCorrection.ItemSizeCorrector + 20;
                scrollHolidays.Width = sizeCorrection.MonthTopStringWidth - sizeCorrection.ItemSizeCorrector;

                ClickedDayPage.Height = sizeCorrection.ItemSizeCorrector + 20;
                ClickedDayPage.Margin = monthTopString.Margin;
                ClickedDayPage.FontSize = monthNameButton.FontSize;
                noteList.Margin = new Thickness(0, sizeCorrection.ItemSizeCorrector/3, 0, 10);
                noteList.Height = sizeCorrection.ItemSizeCorrector * 8 - 90;

                DatePickerDp.FontSize = sizeCorrection.ItemFontSizeCorrector / 2.5;
                GoToDateBtn.FontSize = DatePickerDp.FontSize;
                GoToDateBtn.Height = GoToDate.ActualHeight - 4;
                GoToDateBtn.MinWidth = DatePickerDp.ActualWidth / 1.9;
                AdStack.Margin = new Thickness(0, sizeCorrection.ItemSizeCorrector / 2, 0, sizeCorrection.ItemSizeCorrector / 2);   
            }

                //of width
            if (Window.Current.Bounds.Width / 3 > sizeCorrection.MonthTopStringWidth)
                {
                    calBack.Width = Window.Current.Bounds.Width / 3;
                    noteGridMain.Width = calBack.Width * 2;
                    noteGridMain.Margin = new Thickness(calBack.Width, 0, 0, 0);
                    support.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            else if (Window.Current.Bounds.Width / 2 > sizeCorrection.MonthTopStringWidth)
                {
                    calBack.Width = Window.Current.Bounds.Width / 2;
                    noteGridMain.Width = calBack.Width;
                    noteGridMain.Margin = new Thickness(calBack.Width, 0, 0, 0);
                    support.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    calBack.Width = monthTopString.Width + 50;
                    noteGridMain.Width = calBack.Width * 2;
                    noteGridMain.Margin = new Thickness(calBack.Width, 0, 0, 0);
                    support.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }

            calBack.Height = Window.Current.Bounds.Height;
            noteGridMain.Height = Window.Current.Bounds.Height;      
        }

    }
}
