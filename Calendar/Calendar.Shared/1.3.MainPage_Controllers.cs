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
using System.Collections.ObjectModel;

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page
    {
        private void PageLoadedController()
        {
            PrepareHolidayPanel();
            MarkHolidays();

#if !WINDOWS_PHONE_APP
            Window.Current.SizeChanged += Current_SizeChanged;
            ShowHide();

            try
            {
                var version = ApplicationData.Current.RoamingSettings.Values["AppVersion"].ToString();
                if (version != Windows.ApplicationModel.Package.Current.Id.Version.ToString())
                    NewVersionMessage();
            }
            catch
            {
                ApplicationData.Current.RoamingSettings.Values["AppVersion"] = Windows.ApplicationModel.Package.Current.Id.Version.ToString();
                NewVersionMessage();
            }
#endif
        }

        private void PrepareHolidayPanel()
        {
            Style style = (Style)this.Resources["HolidayFlyoutStyle"]; 
            Brush foreg = (SolidColorBrush)Application.Current.Resources["HolidayTitleColor"];
            Brush bg = (SolidColorBrush)Application.Current.Resources["MainColor"];
            var listOfLVI = new List<ListViewItem>();

            //prepare static holidays, if it's null
            if (lviAll == null) {
                //all and personal
                lviAll = new ListViewItem
                {
                    Tag = "All",
                    Content = DataManager.resource.GetString("AllHol"),
                    Foreground = new SolidColorBrush(Colors.White),
                };

                lviPers = new ListViewItem
                {
                    Tag = "Per",
                    Content = DataManager.resource.GetString("MineAsTag"),
                    Foreground = foreg,
                };

                lviEtc = new ListViewItem
                {
                    Content = "...",
                    Foreground = foreg
                };
                lviEtc.Tapped += GridViewItem_Tapped;
            }

            listOfLVI.Add(lviAll);
            listOfLVI.Add(lviPers);

            var subcollection = DataManager.calBase.HolidayNameCollection.Where(el => el.IsChecked == true);

            for(int i = 0; i < subcollection.Count(); i++)
            {
                listOfLVI.Add(new ListViewItem
                {
                    Content = subcollection.ElementAt(i).Tag,
                    Foreground = foreg,
                });


                ToolTip tt = new ToolTip()
                {
                    Content = subcollection.ElementAt(i).Content,
                    Placement = PlacementMode.Top,
                    FontSize = 16
                };
                ToolTipService.SetToolTip(listOfLVI[i + 2], tt);
            }

            listOfLVI.Add(lviEtc);

            //tapped
            for (int i = 0; i < listOfLVI.Count; i++)
            {
                listOfLVI[i].Background = bg;
                listOfLVI[i].Style = style;

#if !WINDOWS_PHONE_APP
                if(sizeCorrection != null)
                {
                    listOfLVI[i].Height = sizeCorrection.ItemSizeCorrector;
                    listOfLVI[i].Width = sizeCorrection.ItemSizeCorrector;
                    listOfLVI[i].FontSize = sizeCorrection.ItemSizeCorrector / 3;
                }
#else
                listOfLVI[i].Height = Window.Current.Bounds.Width / 8;
                listOfLVI[i].Width = Window.Current.Bounds.Width / 8;
                listOfLVI[i].FontSize = Window.Current.Bounds.Width / 18;
                listOfLVI[i].Margin = new Thickness(5, 0, 5, 10);
#endif
                if (i != listOfLVI.Count - 1)
                    listOfLVI[i].Tapped += holTypes_Tapped;
               // else listOfLVI[i].FontSize *= 2;
            }

            //set source
            HolidayList.ItemsSource = listOfLVI;
            //end select "all"
            SelectedHolidayType = lviAll;
            
            UpdateNoteList();
        }
#region calendar controllers

        /// <summary>
        /// Actions for arrows
        /// </summary>
        /// <param name="value">previous (1) or next (-1)</param>
        private void ArrowButtonController(int value)
        {
            gviPrev = null;
            if (gvDecades.Visibility != Visibility.Visible)
            {
                int month = DataManager.calBase.SelectedDate.Month;
                DataManager.calBase.Skip(value);
                DataManager.calBase.ReadHolidayXml();

                SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                SelectedHolidayType = lviAll;
                SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

                FillCalendar();
                MarkHolidays();


                int val = DataManager.calBase.SelectedDate.Day * (-1) + 1;
                DataManager.calBase.SelectedDate = DataManager.calBase.SelectedDate.AddDays(val);
                UpdateNoteList();
                
#if !WINDOWS_PHONE_APP
                if (SelectedHolidayType != lviAll && SelectedHolidayType != lviPers)
                {
                    ClickedDayPage.Text = DataManager.calBase.SelectedDate.Date.ToString("MMMM");

                    noteList.ItemsSource = DataManager.calBase.HolidayItemCollection.
                    Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower()).
                    Select(hi => hi = hi.Copy()).
                    Select(hi =>
                    {
                        //change name = add date
                        hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                            hi.Day, DataManager.calBase.SelectedDate.Month, hi.HolidayName);
                        hi.FontSize = sizeCorrection.NoteFontSizeCorrector;
                        return hi;
                    });

                NotesBackground();
                }
#endif
            }
            else 
            {
                monthNameButton.Content = (int)monthNameButton.Content + value;
            }
        }

        /// <summary>
        /// Controller for Month/Year (if decadesGrid is visible) button
        /// </summary>
        private void MonthController()
        {
            if (gvDecades.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                monthNameButton.Content = DataManager.calBase.SelectedDate.Year;

                if (gvDecades.Items.Count == 0)
                    InitializeDecades();

                calGrid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                HolidayList.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                monthNameButton.Content = DataManager.calBase.SelectedDate.ToString("MMMM yyyy");  
                calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;

#if !WINDOWS_PHONE_APP
                HolidayList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
#endif
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void DayController(object sender)
        {
            GridViewItem gvi = sender as GridViewItem;

#if !WINDOWS_PHONE_APP
            if (gvi != gviPrev)
#endif
                if (gvi.Style != (Style)this.Resources["AdjMonthStyle"])
                {
                    DataManager.calBase.SelectedDate = new DateTime(DataManager.calBase.SelectedDate.Year,
                                                        DataManager.calBase.SelectedDate.Month,
                                                        Convert.ToInt32(gvi.Content));

                    if (gviPrev == null)
                        gviPrev = gvi;
                    else gviPrev.BorderBrush = TransparentBrush;

                    //highlight selected day
                    //if - for WP application, do not delete it
                    UpdateNoteList();
                    gvi.BorderBrush = gvi.Foreground;
                    gviPrev = gvi;

                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                //move to previous or next month?
                else
                {
                    DateTime date;
                    //prev. monts
                    if (Convert.ToInt32(gvi.Content) > 20)
                        date = DataManager.calBase.SelectedDate.AddMonths(-1);
                    //next month
                    else date = DataManager.calBase.SelectedDate.AddMonths(1);

                    ChangeDate((int)gvi.Content, date.Month, date.Year);
                }
        }

        private void ChangeDate(int gotoDay, int gotoMonth, int gotoYear)
        {
            int month = DataManager.calBase.SelectedDate.Month;
            DataManager.calBase.Skip(gotoDay, gotoMonth, gotoYear);
            if (month != DataManager.calBase.SelectedDate.Month)
                DataManager.calBase.ReadHolidayXml();

            //Shows month and year in the top of calGrid\
            FillCalendar();
            gviPrev = calGrid.Items.ElementAt(DataManager.calBase.Start + gotoDay - 1) as GridViewItem;
            MarkHolidays();
            UpdateNoteList();
            gviPrev.BorderBrush = gviPrev.Foreground;
        }

        private void DecadeItemController(object sender)
        {
            int year = Convert.ToInt32(monthNameButton.Content);
            int month = Convert.ToInt32((sender as GridViewItem).Tag);

            if (month != DataManager.calBase.SelectedDate.Month || year != DataManager.calBase.SelectedDate.Year)
            {
                ChangeDate(1, month, year);

                //Shows month and year in the top of calGrid
                monthNameButton.Content = DataManager.calBase.SelectedDate.ToString("MMMM yyyy");

                calGrid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                weekDayNames.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gvDecades.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

#if !WINDOWS_PHONE_APP
                HolidayList.Visibility = Windows.UI.Xaml.Visibility.Visible;
                HolidayTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
#endif
            }
            else
            {
                MonthController();
            }
        }
#endregion

#region notes controller
        private void NoteController(object sender)
        {
            if (gviPrev != null)
            {
                //new note
                if (((sender as ListViewItem).Content as TextBlock).Text ==
                                        DataManager.resource.GetString("PersonalNote"))
                {
                    FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                    addNotetb.Text = "";
                    delChLists.Visibility = Visibility.Collapsed;
                    addRecLists.Visibility = Visibility.Visible;
                    onceCb.IsChecked = true;
                }
                //existing
                else if ((sender as ListViewItem).Tag.ToString() == DataManager.resource.GetString("MineAsTag"))
                {
                    FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                    addNotetb.Text = ((sender as ListViewItem).Content as TextBlock).Text;
                    startText.Clear().Append(addNotetb.Text);
                    delChLists.Visibility = Visibility.Visible;
                    addRecLists.Visibility = Visibility.Collapsed;

                    foreach (var item in noteList.Items)
                    {
                        HolidayItem converted = item as HolidayItem;
                        if (converted.HolidayName == addNotetb.Text)
                        {
                            if (converted.Year == converted.Month)
                                everyMonth.IsChecked = true;
                            else if (converted.Year == 0)
                                everyYear.IsChecked = true;
                            else onceCb.IsChecked = true;
                        }
                    }
                }

                gviPrev.Tag = null;
            }
            //note from one of the types (if its type is selected)
            else
            {
                //change selected day
                int index = DataManager.calBase.Start + (int)((sender as ListViewItem).Content as TextBlock).Tag - 1;
                GridViewItem gvi = calGrid.Items[index] as GridViewItem;

                //if type "All" selected - show whole list of holidays
                if (SelectedHolidayType == lviAll)
                    HolidayTypesController(SelectedHolidayType);
                else
                {
                    SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                    SelectedHolidayType = lviAll;
                    SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);
                }

                if (gvi.Style != (Style)this.Resources["AdjMonthStyle"])
                {
                    DataManager.calBase.SelectedDate = new DateTime(DataManager.calBase.SelectedDate.Year,
                                                        DataManager.calBase.SelectedDate.Month,
                                                        Convert.ToInt32(gvi.Content));

                    gviPrev = gvi;
                    UpdateNoteList();
                    gvi.BorderBrush = gvi.Foreground;

                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
        }
        
        private void AddNoteController()
        {
            int year = 0, month = 0, day = 0;
            RepeatPeriodChecker(ref month, ref year, ref day);
            if (addNotetb.Text != String.Empty)
            {
                DataManager.SavePersonal(addNotetb.Text,
                    day.ToString(),
                    month.ToString(),
                    year.ToString(),
                    true,
                    "holidays");
            }

            DataManager.calBase.ReadHolidayXml();

            UpdateNoteList();

            if (noteList.Items.Count > 1)
            {
                gviPrev.Foreground = Application.Current.Resources["AdditionalColor"] as Brush;
                gviPrev.BorderBrush = gviPrev.Foreground;
            }

            AddNoteFlyout.Hide();
        }        

        /// <summary>
        /// Gets month and year
        /// </summary>
        /// <returns>year</returns>
        private void RepeatPeriodChecker(ref int month, ref int year, ref int day)
        {
            if (everyYear.IsChecked == true)
            {
                year = 0;
                month = DataManager.calBase.SelectedDate.Month;
                day = DataManager.calBase.SelectedDate.Day;
            }
            else if (everyMonth.IsChecked == true)
            {
                year = 0;
                month = 0;
                day = DataManager.calBase.SelectedDate.Day;
            }
            else
            {
                year = DataManager.calBase.SelectedDate.Year;
                month = DataManager.calBase.SelectedDate.Month;
                day = DataManager.calBase.SelectedDate.Day;
            }
        }

        private void DeleteNoteController()
        {
            int year = 0, month = 0, day = 0;
            RepeatPeriodChecker(ref month, ref year, ref day);
            if (addNotetb.Text != String.Empty)
            {
                DataManager.RemoveHoliday(startText.ToString(),
                    day.ToString(),
                    month.ToString(),
                    year.ToString());
            }

            DataManager.calBase.ReadHolidayXml();

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
                int year = 0, month = 0, day = 0;
                RepeatPeriodChecker(ref month, ref year, ref day);
                if (addNotetb.Text != String.Empty)
                {
                    DataManager.ChangePersonal(
                        startText.ToString(), 
                        addNotetb.Text,
                        day.ToString(),
                        month.ToString(),
                        year.ToString());
                }

                DataManager.calBase.ReadHolidayXml();

                UpdateNoteList();
            }
            AddNoteFlyout.Hide();
        }
        #endregion

        #region holiday panel controllers

        /// <summary>
        /// Save selected holiday types into the xml-file
        /// </summary>
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

            DataManager.calBase.WriteHolidayXml(ls);

            PrepareHolidayPanel();

            DataManager.calBase.ReadHolidayXml();
            DataManager.calBase.FillHolidaysList();

            SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
            SelectedHolidayType = lviAll;
            SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

            MarkHolidays();
            UpdateNoteList();

            HolidayFlyout.Hide();
        }

        private void HolidayTypesController(ListViewItem sender)
        {
            if (sender.Content != null)
            {
                IEnumerable<HolidayItem> buffer;

                SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                SelectedHolidayType = sender;
                SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

                ClickedDayPage.Text = DataManager.calBase.SelectedDate.Date.ToString("MMMM yyyy");

                //special holidays
                if (SelectedHolidayType != lviAll && SelectedHolidayType != lviPers)
                {
                    //notes 
                    buffer = DataManager.calBase.HolidayItemCollection.
                                           Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower());
                }
                else
                {
                    //personal holidays
                    if (SelectedHolidayType != lviAll)
                        buffer = DataManager.calBase.HolidayItemCollection.
                        Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower() &&
                                    hi.Day != 0);

                    //all holidays
                    else buffer = DataManager.calBase.HolidayItemCollection.Where(hi => hi.Day != 0);
                }

                //add dates
                buffer = buffer.Select(hi => hi = hi.Copy()).
                         Select(hi =>
                         {
                            //change name = add date
                            hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                                hi.Day, DataManager.calBase.SelectedDate.Month, hi.HolidayName);

#if !WINDOWS_PHONE_APP
                            hi.FontSize = sizeCorrection.NoteFontSizeCorrector;
#endif

                            return hi;
                         }).
                         OrderBy(hi => hi.Day);

                noteList.ItemsSource = buffer;
                buffer = null;

                NotesBackground();
                MarkHolidays();

#if WINDOWS_PHONE_APP
                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
#endif
                if (gviPrev != null)
                {
                    gviPrev.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    gviPrev = null;
                }
            }
        }
#endregion

        private void messageBtn_Click(object sender, RoutedEventArgs e)
        {
            NewVersionMessage();
        }

        private async void support_Click(object sender, RoutedEventArgs e)
        {
            var mailto = new Uri("mailto:?to=syanne.red@gmail.com&amp;subject=Calendar+Holidays&amp;body=Hello,&nbsp;Syanne!");
            await Windows.System.Launcher.LaunchUriAsync(mailto);
        }

        private void RefreshPage()
        {
            DataManager.calBase.ReadHolidayXml();
            DataManager.calBase.FillHolidaysList();

            MarkHolidays();
            UpdateNoteList();
        }
    }
}
