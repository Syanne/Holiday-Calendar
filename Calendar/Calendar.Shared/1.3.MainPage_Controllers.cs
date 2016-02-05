using System;
using System.Linq;
using CalendarResources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Calendar.Models;
using System.Collections.Generic;
using Windows.Storage;

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
            var holColor = (SolidColorBrush)Application.Current.Resources["HolidayTitleColor"];
            M.Foreground = holColor;
            int jj = 2;
            for (int j = 0; j < calBase.HolidayNameCollection.Count(); j++)
            {
                if (calBase.HolidayNameCollection[j].IsChecked == true)
                {
                    var element = (HolidayList.Items.ElementAt(jj) as ListViewItem);
                    element.Content = calBase.HolidayNameCollection.ElementAt(j).Tag;
                    ToolTip tt = new ToolTip()
                    {
                        Content = calBase.HolidayNameCollection.ElementAt(j).Content,
                        Placement = PlacementMode.Top,
                        FontSize = 16
                    };
                    ToolTipService.SetToolTip(element, tt);
                    element.Foreground = holColor;
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

                try
                {
                    var version = "alla";
                    if (version != Windows.ApplicationModel.Package.Current.Id.Version.ToString())
                        FlyoutBase.ShowAttachedFlyout(bottomAppBarGrid as FrameworkElement);
                }
                catch
                {
                    ApplicationData.Current.RoamingSettings.Values["AppVersion"] = Windows.ApplicationModel.Package.Current.Id.Version.ToString();
                    FlyoutBase.ShowAttachedFlyout(mainBg);

                }

            #endif
        }

        #region calendar controllers      

        private void ArrowButtonController(int value)
        {
            if (gvDecades.Visibility != Windows.UI.Xaml.Visibility.Visible)
            {
                int month = calBase.SelectedDate.Month;
                calBase.Skip(value);
                if (month != calBase.SelectedDate.Month)
                    calBase.ReadHolidayXml();
                FillCalendar();
                MarkHolidays();
                
#if !WINDOWS_PHONE_APP
                if (SelectedHolidayType.Content.ToString() != All.Content.ToString() &&
                    SelectedHolidayType.Content.ToString() != M.Content.ToString())
                {
                    ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("MMMM");

                    noteList.ItemsSource = calBase.HolidayItemCollection.
                    Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower()).
                    Select(hi => hi = hi.Copy()).
                    Select(hi =>
                    {
                        //change name = add date
                        hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                            hi.Day, calBase.SelectedDate.Month, hi.HolidayName);
                        hi.FontSize = sizeCorrection.NoteFontSizeCorrector;
                        return hi;
                    });

                NotesBackground();
                }
#endif
            }
            else 
            {
                calBase.Skip(1, calBase.SelectedDate.Month, calBase.SelectedDate.Year + value);
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
                HolidayList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                #if !WINDOWS_PHONE_APP
                HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                #endif
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                monthNameButton.Content = calBase.SelectedDate.ToString("MMMM yyyy");  
                calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;

        #if !WINDOWS_PHONE_APP
                HolidayList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
        #endif
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
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
        #endregion

        #region notes controller
        private void note_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((sender as ListViewItem).Content as TextBlock).Text
                == CalendarResourcesManager.resource.GetString("PersonalNote"))
            {
                FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                addNotetb.Text = "";
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
                onceCb.IsChecked = true;
            }
            else if ((sender as ListViewItem).Tag.ToString() == CalendarResourcesManager.resource.GetString("MineAsTag"))
            {
                FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                addNotetb.Text = ((sender as ListViewItem).Content as TextBlock).Text;
                startText.Clear().Append(addNotetb.Text);
                delChLists.Visibility = Windows.UI.Xaml.Visibility.Visible;
                addRecLists.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                foreach (var item in noteList.Items)
                {
                    if ((item as HolidayItem).HolidayName == addNotetb.Text)
                    {
                        if ((item as HolidayItem).Year == 0)
                            everyCb.IsChecked = true;
                        else onceCb.IsChecked = true;
                        break;
                    }
                }
            }
        }
        
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

            UpdateNoteList();

            if (noteList.Items.Count > 1)
            {
                gviPrev.Foreground = Application.Current.Resources["AdditionalColor"] as Brush;
                gviPrev.BorderBrush = gviPrev.Foreground;
            }

            AddNoteFlyout.Hide();
        }        

        /// <summary>
        /// SelectedDate year or 0 (every year)
        /// </summary>
        /// <returns>year</returns>
        private string YearChecker()
        {
            RadioButton year = radioBtParent.Items.FirstOrDefault(item => 
                                (item as RadioButton).IsChecked == true) as RadioButton;

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
            {
                gviPrev.Foreground = new SolidColorBrush(Colors.White);
                gviPrev.BorderBrush = gviPrev.Foreground;
            }

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

        #region holiday flyout controllers

        private void SaveHolidayTypes()
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
                if (jj > 5) break;
            }
            //--------------------

            calBase.ReadHolidayXml();
            calBase.FillHolidaysList();

            SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
            SelectedHolidayType = All;
            SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

            MarkHolidays();
            UpdateNoteList();

            butHolidayFlyout.Hide();
        }

        private void HolidayTypesController(object sender)
        {
            if ((sender as ListViewItem).Content != null)
            {
                SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                SelectedHolidayType = sender as ListViewItem;
                SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);
                
                //special holidays
                if (SelectedHolidayType.Content.ToString() != All.Content.ToString() &&
                    SelectedHolidayType.Content.ToString() != M.Content.ToString())
                {
                    ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("MMMM");

                    noteList.ItemsSource = calBase.HolidayItemCollection.
                    Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower()).
                    Select(hi => hi = hi.Copy()).
                    Select(hi =>
                    {
                        //change name = add date
                        hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                            hi.Day, calBase.SelectedDate.Month, hi.HolidayName);
            #if !WINDOWS_PHONE_APP
                        hi.FontSize = sizeCorrection.NoteFontSizeCorrector;
            #endif
                        return hi;
                    });

                #if WINDOWS_PHONE_APP
                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
                #endif
                }
                else
                {
                    ClickedDayPage.Text = calBase.SelectedDate.Date.ToString("D");

                    //personal holidays
                    if (SelectedHolidayType.Content.ToString() != All.Content.ToString())
                        noteList.ItemsSource = calBase.HolidayItemCollection.
                        Where(hi => (hi.Day == calBase.SelectedDate.Day || hi.Day == 0) &&
                             (hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower()));

                    //all holidays
                    else
                        noteList.ItemsSource = calBase.HolidayItemCollection.
                       Where(hi => hi.Day == calBase.SelectedDate.Day || hi.Day == 0);
                }

                NotesBackground();
                MarkHolidays();
            }
        }
        #endregion
    }
}
