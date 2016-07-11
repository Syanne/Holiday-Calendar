using System;
using System.Collections.Generic;
using System.Linq;
using CalendarResources;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.Globalization;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=391641

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool prevSender = false;
        public MainPage()
        {             
            //initialize page
            this.InitializeComponent();
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            //load calendar
            PagePreLoader();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PageLoadedController();
        }

        /// <summary>
        /// back button pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                //close noteList, if it's visible
                if (noteGridMain.Visibility == Windows.UI.Xaml.Visibility.Visible)
                {
                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }

                e.Handled = true;
            }
        }

        #region Calendar controls

        /// <summary>
        /// Show next month
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butPrev_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (prevSender != true)
                ArrowButtonController(-1);
            else prevSender = false;
        }
        /// <summary>
        /// Show previous month
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void butNext_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (prevSender != true)
                ArrowButtonController(1);
            else prevSender = false;
        }

        /// <summary>
        /// show or hide decades gridView
        /// </summary>
        /// <param name="sender">Button with showing month name</param>
        /// <param name="e">click</param>
        private void monthNameButton_Click(object sender, RoutedEventArgs e)
        {
            MonthController();
        }

        private void gvItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DayController(sender);
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
        }

        private void m1_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DecadeItemController(sender);
        }

        #endregion

        #region Notes

        private void note_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NoteController(sender);
        }
        private void butPrev1_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NoteGridArrowsController(-1);
        }

        private void butNext1_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NoteGridArrowsController(1);
        }

        private void NoteGridArrowsController(int value)
        {
            if (ClickedDayPage.Text != DataManager.calBase.SelectedDate.Date.ToString("MMMM"))
            {
                int month = DataManager.calBase.SelectedDate.Month;
                DataManager.calBase.SelectedDate = DataManager.calBase.SelectedDate.AddDays(value);
                if (month != DataManager.calBase.SelectedDate.Month)
                {
                    DataManager.calBase.ReadHolidayXml();

                    FillCalendar();
                    MarkHolidays();
                    gviPrev = calGrid.Items.ElementAt(DataManager.calBase.Start + DataManager.calBase.SelectedDate.Day - 1) as GridViewItem;

                    gviPrev.BorderBrush = gviPrev.Foreground;
                }

                UpdateNoteList();
            }
            else
            {
                ArrowButtonController(value);

                ClickedDayPage.Text = DataManager.calBase.SelectedDate.Date.ToString("MMMM");

                noteList.ItemsSource = DataManager.calBase.HolidayItemCollection.
                Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower() && hi.Day != 0).
                Select(hi => hi = hi.Copy()).
                Select(hi =>
                {
                        //change name = add date
                        hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                        hi.Day, DataManager.calBase.SelectedDate.Month, hi.HolidayName);
                    return hi;
                });

                NotesBackground();

            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            DataManager.calBase.Skip(DatePickerDp.Date.Day, DatePickerDp.Date.Month, DatePickerDp.Date.Year);

            //Shows month and year in the top of calGrid\
            if (DatePickerDp.Date.Month != DataManager.calBase.SelectedDate.Date.Month &&
                DatePickerDp.Date.Year != DataManager.calBase.SelectedDate.Date.Year)
            {
                FillCalendar();
                MarkHolidays();
            }
            UpdateNoteList();

            GridViewItem gvi = calGrid.Items.ElementAt(DatePickerDp.Date.Day + DataManager.calBase.Start - 1) as GridViewItem;
            //highlight selected day 
            if (gviPrev != gvi)
            {
                gvi.BorderThickness = new Thickness(3);
                gvi.BorderBrush = gvi.Foreground;
                gviPrev.BorderThickness = new Thickness(0);
                gviPrev = gvi;
            }

            DatePickerDp.Date = DateTimeOffset.Now;
        }

        #region AddNote flyout

        private void AddNoteFlyout_Opened(object sender, object e)
        {
            addNotetb.Focus(FocusState.Keyboard);
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

                startText.Clear();
                startText.Append(addNotetb.Text);
            }
        }

        private void reclineButton_Click(object sender, RoutedEventArgs e)
        {
            AddNoteFlyout.Hide();
        }
        #endregion

        #endregion

        #region Bottom Menu as AppBarButtons

        private void SettinngsAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private async void RateAppButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("ms-windows-store:reviewapp?appid=" + CurrentApp.AppId);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void HolidaysAppButton_Click(object sender, RoutedEventArgs e)
        {
            if (gvDecades.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                if (HolidayList.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
                {
                    HolidayList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    HolidayList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
        }

        private void StyleAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StylesPage));
        }

        private void ShopAppButton_Click(object sender, RoutedEventArgs e)
        {
            ShoppingManager.BuyThis("BuyMeTextMP", "BuyMeTitleMP", "allstuff1");
        }
        #endregion

        #region Kind of Holiday menu & flyout

        private void ListViewItem_Tapped_1(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(HolidayList as FrameworkElement);
        }


        /// <summary>
        /// Saves selected themes in xml-file
        /// </summary>
        /// <param name="sender">button in Flyout</param>
        /// <param name="e">Clicked</param>
        private void btnHolidays_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveHolidayTypes();
            }
            catch
            {

            }
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
            foreach (CheckBox ic in listOfHolidays.Items.Where(i => i is CheckBox))
            {
                ic.Style = (Style)this.Resources["CbHolidayStyleWP"];
            }
        }

        private void btnholCancel_Click(object sender, RoutedEventArgs e)
        {
            HolidayFlyout.Hide();
        }

        private void holTypes_Tapped(object sender, TappedRoutedEventArgs e)
        {
            HolidayTypesController(sender as ListViewItem);
        }
        #endregion

        private void HolidayFlyout_Opened(object sender, object e)
        {
            listOfHolidays.Height = Window.Current.Bounds.Height - 60;
        }

        //private void AdControl_ErrorOccurred(object sender, Microsoft.Advertising.Mobile.Common.AdErrorEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine("Ad Error : ({0}) {1}", e.ErrorCode, e.Error);
        //}

        private void nextGVI_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            ArrowButtonController(1);
            prevSender = true;
        }

        private void prevGVI_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            ArrowButtonController(-1);
            prevSender = true;
        }

    }

}
