using CalendarResources;

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

namespace Calendar
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            //language
            if (ApplicationData.Current.LocalSettings.Values.Count == 0)
                ApplicationData.Current.LocalSettings.Values.Add("Language", ApplicationLanguages.PrimaryLanguageOverride);
             
            this.InitializeComponent();
            PagePreLoader();
                        
            //components
            if (Window.Current.Bounds.Width < 1200)
            {
                calBack.Width = 510;
                noteGridMain.Margin = new Thickness(530, 10, 5, 10);
                gvDecades.Margin = new Thickness(25, 150, 25, 0);
            }
        }              

        #region Calendar controls     
        private void butNext_Click(object sender, RoutedEventArgs e)
        {
            NextButtonController();
        }
        
        private void butPrev_Click(object sender, RoutedEventArgs e)
        {
            PreviousButtonController();
        }
        
        private void GoToDateBtn_Click(object sender, RoutedEventArgs e)
        {
            //have you bought my app?
            //if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            //{
                calBase.Skip(Convert.ToInt32(gviPrev.Content), DatePickerDp.Date.Month, DatePickerDp.Date.Year);

                //Shows month and year in the top of calGrid\
                FillCalendar();
                MarkHolidays();

                DatePickerDp.Date = DateTimeOffset.Now;
            //}
            //else ShoppingManager.BuyThis("Unlicensed", "UnlicensedTitle", "allstuff1");            
        }
        
        private void monthNameButton_Click(object sender, RoutedEventArgs e)
        {
            MonthController();
        }

        private void Decade_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            calBase.Skip(1, Convert.ToInt32((sender as GridViewItem).Tag), calBase.SelectedDate.Year);

            //Shows month and year in the top of calGrid
            monthNameButton.Content = calBase.SelectedDate.ToString("MMMM yyyy");

            calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
            weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;
            gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            FillCalendar();
            MarkHolidays();
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
            List<string> ls = new List<string>();
            foreach (var lv in listOfHolidays.Items)
            {
                if (lv is CheckBox && (lv as CheckBox).IsChecked == true)
                {
                    ls.Add((lv as CheckBox).Content.ToString());
                    ls.Add((lv as CheckBox).Tag.ToString());
                }
            }

            calBase.WriteHolidayXml(ls);

            //-------dupblicates Loaded() --------
            int jj = 2;
            for (int j = 0; j < calBase.HolidayNameCollection.Count(); j++)
            {
                if (calBase.HolidayNameCollection[j].IsChecked == true)
                {
                    (HolidayList.Items.ElementAt(jj) as ListViewItem).Content = calBase.HolidayNameCollection.ElementAt(j).Tag;
                    ToolTip tt = new ToolTip() { Content = calBase.HolidayNameCollection.ElementAt(j).Content, Placement = PlacementMode.Top };
                    ToolTipService.SetToolTip((HolidayList.Items.ElementAt(jj) as ListViewItem), tt);
                    jj++;
                }
                if (jj == 5) break;
            }
            //--------------------
            
            calBase.ReadHolidayXml();
            calBase.FillHolidaysList();
            MarkHolidays();

            SelectedHolidayType.Foreground = Application.Current.Resources["MainFg"] as Brush;
            SelectedHolidayType = All;
            SelectedHolidayType.Foreground = Application.Current.Resources["SelectionFg"] as Brush;
            UpdateNoteList(); 
            
            butHolidayFlyout.Hide();
        }

        /// <summary>
        /// Select type of holidays in the calendar
        /// </summary>
        /// <param name="sender">button</param>
        /// <param name="e">Clicked</param>
        private void GridViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(HolidayList as FrameworkElement);

            listOfHolidays.ItemsSource = calBase.HolidayNameCollection;
            foreach (CheckBox ic in listOfHolidays.Items.Where(i => i is CheckBox))
                ic.Click += cb_Click;
        }

        /// <summary>
        /// check the number of selected themes for holiday list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void cb_Click(object sender, RoutedEventArgs e)
        {
            int counter = listOfHolidays.Items.Count(i => (i as CheckBox).IsChecked == true);
            if (counter > 3) btnHolidays.IsEnabled = false;
            else btnHolidays.IsEnabled = true;
        }

        private void Flyout_Opened(object sender, object e)
        {
            mainBg.Opacity = 0.3;
        }
        private void butHolidayFlyout_Closed(object sender, object e)
        {
            mainBg.Opacity = 1;
        }

        private void btnholCancel_Click(object sender, RoutedEventArgs e)
        {
            butHolidayFlyout.Hide();
        }

        private void holTypes_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as ListViewItem).Content != null)
            {
                SelectedHolidayType.Foreground = Application.Current.Resources["MainFg"] as Brush;
                SelectedHolidayType = sender as ListViewItem;
                SelectedHolidayType.Foreground = Application.Current.Resources["SelectionFg"] as Brush;

                if (SelectedHolidayType.Content.ToString() != All.Content.ToString())
                    noteList.ItemsSource = calBase.HolidayItemCollection.
                        Where(hi => (hi.Date == calBase.SelectedDate.Day || hi.Date == 0) && (hi.HolidayTag == SelectedHolidayType.Content.ToString()));
                else
                    noteList.ItemsSource = calBase.HolidayItemCollection.
                   Where(hi => hi.Date == calBase.SelectedDate.Day || hi.Date == 0);

                MarkHolidays();
            }
        }
        #endregion

        #region AddNote flyout
       
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

    }

}
