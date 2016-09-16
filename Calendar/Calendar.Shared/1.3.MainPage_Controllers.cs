using System;
using System.Linq;
using Calendar.Services;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.Storage;
using System.Collections.ObjectModel;
using Calendar.Data.Services;
using Calendar.Data.Models;

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

        /// <summary>
        /// Refresh bottom app panel
        /// </summary>
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
                    Content = DataManager.GetStringFromResourceLoader("AllHol"),
                    Foreground = new SolidColorBrush(Colors.White),
                };

                lviPers = new ListViewItem
                {
                    Tag = "Per",
                    Content = DataManager.GetStringFromResourceLoader("MineAsTag"),
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

            var subcollection = LocalDataManager.GetSelectedCategoriesList();

            for (int i = 0; i < subcollection.Count(); i++)
            {
                listOfLVI.Add(new ListViewItem
                {
                    Content = subcollection.ElementAt(i).Key,
                    Foreground = foreg,
                });


                ToolTip tt = new ToolTip()
                {
                    Content = subcollection.ElementAt(i).Value,
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
                int month = LocalDataManager.calBase.SelectedDate.Month;
                LocalDataManager.calBase.Skip(value);
                LocalDataManager.calBase.RefreshDataCollection();

                SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                SelectedHolidayType = lviAll;
                SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

                FillCalendar();
                MarkHolidays();


                int val = LocalDataManager.calBase.SelectedDate.Day * (-1) + 1;
                LocalDataManager.calBase.SelectedDate = LocalDataManager.calBase.SelectedDate.AddDays(val);
                UpdateNoteList();
                
#if !WINDOWS_PHONE_APP
                if (SelectedHolidayType != lviAll && SelectedHolidayType != lviPers)
                {
                    ClickedDayPage.Text = LocalDataManager.calBase.SelectedDate.Date.ToString("MMMM");

                    noteList.ItemsSource = LocalDataManager.calBase.HolidayItemCollection.
                    Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower()).
                    Select(hi => hi = hi.Copy()).
                    Select(hi =>
                    {
                        //change name = add date
                        hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                            hi.Day, LocalDataManager.calBase.SelectedDate.Month, hi.HolidayName);
                        hi.FontSize = sizeCorrection.NoteFontSizeCorrector;
                        return hi;
                    });

                NotesBackground();
                }
#endif
            }
            else monthNameButton.Content = (int)monthNameButton.Content + value;
        }

        /// <summary>
        /// Controller for Month/Year (if decadesGrid is visible) button
        /// </summary>
        private void MonthController()
        {
            if (gvDecades.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                monthNameButton.Content = LocalDataManager.calBase.SelectedDate.Year;

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
                monthNameButton.Content = LocalDataManager.calBase.SelectedDate.ToString("MMMM yyyy");  
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
                    LocalDataManager.calBase.SelectedDate = new DateTime(LocalDataManager.calBase.SelectedDate.Year,
                                                        LocalDataManager.calBase.SelectedDate.Month,
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
                        date = LocalDataManager.calBase.SelectedDate.AddMonths(-1);
                    //next month
                    else date = LocalDataManager.calBase.SelectedDate.AddMonths(1);

                    ChangeDate((int)gvi.Content, date.Month, date.Year);
                }
        }

        /// <summary>
        /// Select another day
        /// </summary>
        /// <param name="gotoDay">day to selected</param>
        /// <param name="gotoMonth">month to select</param>
        /// <param name="gotoYear">year to select</param>
        private void ChangeDate(int gotoDay, int gotoMonth, int gotoYear)
        {
            int month = LocalDataManager.calBase.SelectedDate.Month;
            LocalDataManager.calBase.Skip(gotoDay, gotoMonth, gotoYear);
            if (month != LocalDataManager.calBase.SelectedDate.Month)
                LocalDataManager.calBase.RefreshDataCollection();

            //Shows month and year in the top of calGrid\
            FillCalendar();
            gviPrev = calGrid.Items.ElementAt(LocalDataManager.Start + gotoDay - 1) as GridViewItem;
            MarkHolidays();
            UpdateNoteList();
            gviPrev.BorderBrush = gviPrev.Foreground;
        }

        /// <summary>
        /// Prepare decade panel
        /// </summary>
        /// <param name="sender">sender grid</param>
        private void DecadeItemController(object sender)
        {
            int year = Convert.ToInt32(monthNameButton.Content);
            int month = Convert.ToInt32((sender as GridViewItem).Tag);

            if (month != LocalDataManager.calBase.SelectedDate.Month || year != LocalDataManager.calBase.SelectedDate.Year)
            {
                ChangeDate(1, month, year);

                //Shows month and year in the top of calGrid
                monthNameButton.Content = LocalDataManager.calBase.SelectedDate.ToString("MMMM yyyy");

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
        /// <summary>
        /// Process record selection
        /// </summary>
        /// <param name="sender"></param>
        private void RecordController(object sender)
        {
            if (gviPrev != null)
            {
                //new note
                if (((sender as ListViewItem).Content as TextBlock).Text ==
                                        DataManager.GetStringFromResourceLoader("PersonalNote"))
                {
                    FlyoutBase.ShowAttachedFlyout(noteList as FrameworkElement);
                    addNotetb.Text = "";
                    delChLists.Visibility = Visibility.Collapsed;
                    addRecLists.Visibility = Visibility.Visible;
                    onceCb.IsChecked = true;
                }
                //existing
                else if ((sender as ListViewItem).Tag.ToString() == DataManager.GetStringFromResourceLoader("MineAsTag"))
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
                int index = LocalDataManager.Start + (int)((sender as ListViewItem).Content as TextBlock).Tag - 1;
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
                    LocalDataManager.calBase.SelectedDate = new DateTime(LocalDataManager.calBase.SelectedDate.Year,
                                                        LocalDataManager.calBase.SelectedDate.Month,
                                                        Convert.ToInt32(gvi.Content));

                    gviPrev = gvi;
                    UpdateNoteList();
                    gvi.BorderBrush = gvi.Foreground;

                    noteGridMain.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
            }
        }
        
        /// <summary>
        /// Add new record
        /// </summary>
        /// <param name="recordText"></param>
        private void AddRecordController(string recordText)
        {
            int year = 0, month = 0, day = 0;
            RepeatPeriodChecker(ref month, ref year, ref day);
            if (recordText != String.Empty)
                DataManager.CreateRecord(recordText, day, month, year);            

            LocalDataManager.calBase.RefreshDataCollection();

            UpdateNoteList();

            if (noteList.Items.Count > 1)
            {
                gviPrev.Foreground = Application.Current.Resources["AdditionalColor"] as Brush;
                gviPrev.BorderBrush = gviPrev.Foreground;
            }

            AddNoteFlyout.Hide();
        }

        /// <summary>
        /// Check snooze
        /// </summary>
        /// <param name="month">month</param>
        /// <param name="year">year</param>
        /// <param name="day">day</param>
        private void RepeatPeriodChecker(ref int month, ref int year, ref int day)
        {
            if (everyYear.IsChecked == true)
            {
                year = 0;
                month = LocalDataManager.calBase.SelectedDate.Month;
            }
            else if (everyMonth.IsChecked == true)
            {
                year = 0;
                month = 0;
            }
            else
            {
                year = LocalDataManager.calBase.SelectedDate.Year;
                month = LocalDataManager.calBase.SelectedDate.Month;
            }

            day = LocalDataManager.calBase.SelectedDate.Day;
        }

        /// <summary>
        /// Remove selected record
        /// </summary>
        private void DeleteRecordController()
        {
            int year = 0, month = 0, day = 0;
            RepeatPeriodChecker(ref month, ref year, ref day);
            if (addNotetb.Text != String.Empty)
            {
                DataManager.RemoveRecord(startText.ToString(),
                    day.ToString(),
                    month.ToString(),
                    year.ToString());
            }

            LocalDataManager.calBase.RefreshDataCollection();

            if (gviPrev != null)
                UpdateNoteList();

            if (noteList.Items.Count == 1)
            {
                gviPrev.Foreground = new SolidColorBrush(Colors.White);
                gviPrev.BorderBrush = gviPrev.Foreground;
            }

            AddNoteFlyout.Hide();
        }

        /// <summary>
        /// Change selected record
        /// </summary>
        private void ChangeRecordController()
        {
            if (addNotetb.Text != "")
            {
                int year = 0, month = 0, day = 0;
                RepeatPeriodChecker(ref month, ref year, ref day);
                if (addNotetb.Text != String.Empty)
                {
                    DataManager.ChangeRecord(
                        startText.ToString(), 
                        addNotetb.Text,
                        day.ToString(),
                        month.ToString(),
                        year.ToString());
                }

                LocalDataManager.calBase.RefreshDataCollection();

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
            LocalDataManager.calBase.RefreshBottomPanelAndCalendar(listOfHolidays.Items.Where(item => item is CheckBox));
            PrepareHolidayPanel();

            SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
            SelectedHolidayType = lviAll;
            SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

            MarkHolidays();
            UpdateNoteList();

            HolidayFlyout.Hide();
        }

        /// <summary>
        /// Selected type (category) of holiday
        /// </summary>
        /// <param name="sender"></param>
        private void HolidayTypesController(ListViewItem sender)
        {
            if (sender.Content != null)
            {
                IEnumerable<HolidayItem> buffer;

                SelectedHolidayType.Foreground = Application.Current.Resources["HolidayTitleColor"] as Brush;
                SelectedHolidayType = sender;
                SelectedHolidayType.Foreground = new SolidColorBrush(Colors.White);

                ClickedDayPage.Text = LocalDataManager.calBase.SelectedDate.Date.ToString("MMMM yyyy");

                //special holidays
                if (SelectedHolidayType != lviAll && SelectedHolidayType != lviPers)
                {
                    //notes 
                    buffer = LocalDataManager.calBase.HolidayItemCollection.
                                           Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower());
                }
                else
                {
                    //personal holidays
                    if (SelectedHolidayType != lviAll)
                        buffer = LocalDataManager.calBase.HolidayItemCollection.
                        Where(hi => hi.HolidayTag == SelectedHolidayType.Content.ToString().ToLower() &&
                                    hi.Day != 0);

                    //all holidays
                    else buffer = LocalDataManager.calBase.HolidayItemCollection.Where(hi => hi.Day != 0);
                }

                //add dates
                buffer = buffer.Select(hi => hi = hi.Copy()).
                         Select(hi =>
                         {
                            //change name = add date
                            hi.HolidayName = String.Format("{0:00}.{1:00}. {2}",
                                hi.Day, LocalDataManager.calBase.SelectedDate.Month, hi.HolidayName);

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
            LocalDataManager.calBase.RefreshBottomPanelAndCalendar(null);

            MarkHolidays();
            UpdateNoteList();
        }
    }
}
