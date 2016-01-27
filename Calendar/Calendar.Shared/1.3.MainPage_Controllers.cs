using System;
using System.Linq;
using CalendarResources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            calBase.ReadHolidayXml();
            calBase.FillHolidaysList();

            MarkHolidays();

            //change data in HolidayList 
            for (int i = 2; i < 5; i++)
            {
                (HolidayList.Items.ElementAt(i) as ListViewItem).Content = "";
                ToolTipService.SetToolTip((HolidayList.Items.ElementAt(i) as ListViewItem), "");
            }

            //change foreground and tooltip for each HolidayList's item
            var color = (SolidColorBrush)Application.Current.Resources["AdditionalColor"];
            M.Foreground = color;
            int jj = 2;
            for (int j = 0; j < calBase.HolidayNameCollection.Count(); j++)
            {
                if (calBase.HolidayNameCollection[j].IsChecked == true)
                {
                    var element = (HolidayList.Items.ElementAt(jj) as ListViewItem);
                    element.Content = calBase.HolidayNameCollection.ElementAt(j).Tag;
                    ToolTip tt = new ToolTip() { Content = calBase.HolidayNameCollection.ElementAt(j).Content, Placement = PlacementMode.Top };
                    ToolTipService.SetToolTip(element, tt);
                    element.Foreground = color;
                    jj++;
                }
                if (jj == 5) break;
            }

            //end select "all"
            SelectedHolidayType = All;
            SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);
            UpdateNoteList();            

            #if !WINDOWS_PHONE_APP                
                Window.Current.SizeChanged += Current_SizeChanged;
                ShowHide();
            #endif
        }
        
        void gvItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            GridViewItem gvi = sender as GridViewItem;
            if (gvi.Style != (Style)this.Resources["AdjMonthStyle"])
            {
                calBase.SelectedDate = new DateTime(calBase.SelectedDate.Year,
                                                    calBase.SelectedDate.Month,
                                                    Convert.ToInt32(gvi.Content));

                UpdateNoteList();

                //highlight selected day 
                if (gviPrev != gvi)
                {
                    gvi.BorderThickness = new Thickness(3);
                    gvi.BorderBrush = gvi.Foreground;
                    gviPrev.BorderThickness = new Thickness(0);
                }
                
                gviPrev = gvi;

                noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            //move to previous or next month?
            else
            {
                if (Convert.ToInt32(gvi.Content) > 20)
                    calBase.Skip(-1);
                else calBase.Skip(1);

                FillCalendar();
                MarkHolidays();
            }
        }


        private void m1_Tapped(object sender, TappedRoutedEventArgs e)
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


        private void note_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((sender as ListViewItem).Content as TextBlock).Text
                == CalendarResourcesManager.resource.GetString("PersonalNote"))
            {
                FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                addNotetb.Text = "";
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else if ((sender as ListViewItem).Tag.ToString() == CalendarResourcesManager.resource.GetString("MineAsTag"))
            {
                FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                addNotetb.Text = ((sender as ListViewItem).Content as TextBlock).Text;
                startText.Clear().Append(addNotetb.Text);
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }


        #region month string controllers
        private void PreviousButtonController()
        {
            if (calGrid.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                calBase.Skip(-1);
                FillCalendar();
                MarkHolidays();
            }
            else if (calBase.SelectedDate.Year >= 1920)
            {
                calBase.Skip(1, calBase.SelectedDate.Month, calBase.SelectedDate.Year - 1);
                monthNameButton.Content = calBase.SelectedDate.Year;
            }
        }

        private void NextButtonController()
        {
            if (calGrid.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                calBase.Skip(1);
                FillCalendar();
                MarkHolidays();
            }
            else if (calBase.SelectedDate.Year <= 2050)
            {
                calBase.Skip(1, calBase.SelectedDate.Month, calBase.SelectedDate.Year + 1);
                monthNameButton.Content = calBase.SelectedDate.Year;
            }
        }

        private void MonthController()
        {
            if (gvDecades.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                monthNameButton.Content = calBase.SelectedDate.Year;

                if (gvDecades.Items.Count == 0)
                    InitializeDecades();

                calGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                monthNameButton.Content = calBase.SelectedDate.ToString("MMMM yyyy");  
                calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
        #endregion

        #region notes controller
        private void AddNoteController()
        {
            if (addNotetb.Text != String.Empty)
            {
                CalendarResourcesManager.SavePersonal(addNotetb.Text,
                    calBase.SelectedDate.Day.ToString(),
                    calBase.SelectedDate.Month.ToString(),
                    YearChecker());
            }

            calBase.ReadHolidayXml();

            //noteList.ItemsSource = calBase.HolidayItemCollection.Where(hi => hi.Date == calBase.SelectedDate.Day || hi.Date == 0);
            UpdateNoteList();

            if (noteList.Items.Count > 1)
                gviPrev.Foreground = Application.Current.Resources["AdditionalColor"] as Brush;

            AddNoteFlyout.Hide();
        }        

        /// <summary>
        /// SelectedDate year or 0 (every year)
        /// </summary>
        /// <returns>year</returns>
        private string YearChecker()
        {
            RadioButton year = radioBtParent.Items.FirstOrDefault(item => (item as RadioButton).IsChecked == true) as RadioButton;

            if (year.Tag.ToString() == "0")
                return "0";
            else return calBase.SelectedDate.Year.ToString();
        }

        private void DeleteNoteController()
        {
            CalendarResourcesManager.RemoveHoliday(startText.ToString(),
            calBase.SelectedDate.Day.ToString(),
            calBase.SelectedDate.Month.ToString(),
            YearChecker());

            calBase.ReadHolidayXml();

            if (gviPrev != null)
                UpdateNoteList();

            if (noteList.Items.Count == 1)
                gviPrev.Foreground = new SolidColorBrush(Colors.White);

            AddNoteFlyout.Hide();
        }

        private void ChangeNoteController()
        {
            if (addNotetb.Text != "")
            {
                CalendarResourcesManager.ChangePersonal(startText.ToString(),
                    addNotetb.Text,
                calBase.SelectedDate.Day.ToString(),
                calBase.SelectedDate.Month.ToString(),
                YearChecker());

                calBase.ReadHolidayXml();

                UpdateNoteList();
            }
            AddNoteFlyout.Hide();
        }
        #endregion
    }
}
