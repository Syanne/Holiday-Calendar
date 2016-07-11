using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Globalization;

using CalendarResources;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Calendar.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Calendar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        //private ObservableCollection<LvItem> itemSource;

        public SettingsPage()
        {
            this.InitializeComponent();

            //enable toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                    tileToggle.IsOn = true;
                if (task.Value.Name == "ToastBackgroundTask")
                    toastToggle.IsOn = true;
            }            

            //FlipViews (small and full-screen)
            SampleDataSource sds = new SampleDataSource();
            myFlip.ItemsSource = sds.Items;
            smallThemesPreview.ItemsSource = sds.Items;
        }             
          
        #region themes        
        private void cancelBth_Click(object sender, RoutedEventArgs e)
        {
            themesFullScreen.Visibility = Visibility.Collapsed;
        }

        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            {
                string temp;
                if (themesFullScreen.Visibility == Visibility.Visible)
                    temp = (myFlip.SelectedItem as LocalFVItem).Tag;
                else temp = (smallThemesPreview.SelectedItem as LocalFVItem).Tag;

                ApplicationData.Current.LocalSettings.Values["AppTheme"] =
                    String.Format("ms-appx:///themes/{0}.xaml", temp);
                Application.Current.Resources.Source =
                   new Uri("ms-appx:///themes/" + temp + ".xaml");

                if (themesFullScreen.Visibility == Visibility.Visible)
                    themesFullScreen.Visibility = Visibility.Collapsed;
                else this.Frame.Navigate(typeof(MainPage));
            }
            else
            {
                OfferPurchase();
            }
        }

        private void buttonTheme_Click(object sender, RoutedEventArgs e)
        {
            themesFullScreen.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void myFlip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myFlip.Items != null)
                SlideNumberTb.Text = String.Format("{0}/{1}", myFlip.SelectedIndex + 1, myFlip.Items.Count);
        }
        
        #endregion

        private void tileToggle_Toggled(object sender, RoutedEventArgs e)
        {
            TileToggle("TileBackgroundTask", "BackgroundUpdater.TileBackgroundTask");
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToastToggle("ToastBackgroundTask", "BackgroundUpdater.ToastBackgroundTask");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BuyStuff();
        }
        
        private void buy_Click(object sender, RoutedEventArgs e)
        {
            BuyButtonController();
        }
        
        private void SettingsFlyout_Unloaded(object sender, RoutedEventArgs e)
        {
            var rootFrame = new Frame();
            rootFrame.Navigate(typeof(MainPage));
        }
        
        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }        

        private void smallThemesPreview_Tapped(object sender, TappedRoutedEventArgs e)
        {
            myFlip.SelectedIndex = smallThemesPreview.SelectedIndex;
            SlideNumberTb.Text = myFlip.SelectedIndex + 1 + @"/" + myFlip.Items.Count;

            themesFullScreen.Visibility = Visibility.Visible;
        }

        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        #region BgTasks

        private void TileToggle(string name, string entryPoint)
        {
            if (tileToggle.IsOn)
            {
                BackgroundTaskCreator(name, entryPoint, 15);
            }
            else
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                    if (task.Value.Name == "TileBackgroundTask")
                        task.Value.Unregister(true);
            }
        }

        private void ToastToggle(string name, string entryPoint)
        {
            try
            {
                if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
                {
                    if (toastToggle.IsOn)
                    {
                        DataManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                        //save changes
                        DataManager.SaveDocumentAsync();

                        //set period
                        uint period = Convert.ToUInt32((comboPeriod.SelectedItem as ComboBoxItem).Content);
                        BackgroundTaskCreator(name, entryPoint, period * 60);
                    }
                    else
                    {
                        foreach (var task in BackgroundTaskRegistration.AllTasks)
                            if (task.Value.Name == "ToastBackgroundTask")
                                task.Value.Unregister(true);
                    }
                }
                else
                {
                    OfferPurchase();
                }
            }
            catch(Exception ex)
            {
                MyMessage(ex.Message);
            }
        }

        public async void BackgroundTaskCreator(string name, string entryPoint, uint time)
        {
            //clean
            foreach (var task in BackgroundTaskRegistration.AllTasks)
                if (task.Value.Name == name)
                    task.Value.Unregister(true);

            //register
            var timerTrigger = new TimeTrigger(time, false);
            await BackgroundExecutionManager.RequestAccessAsync();

            var builder = new BackgroundTaskBuilder();

            builder.Name = name;
            builder.TaskEntryPoint = entryPoint;
            builder.SetTrigger(timerTrigger);
            builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));

            BackgroundTaskRegistration tTask = builder.Register();
        }

        #endregion

        #region Purchasing

        private void BuyButtonController()
        {
            BuyStuff();
        }

        private async void OfferPurchase()
        {
            var dial = new MessageDialog(DataManager.resource.GetString("Unlicensed"));

            dial.Commands.Add(new UICommand(DataManager.resource.GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(DataManager.resource.GetString("UnlicensedButton"),
            new UICommandInvokedHandler((args) =>
            {
                BuyStuff();
            })));
            var command = await dial.ShowAsync();
        }

        private async void BuyStuff()
        {
            try
            {
                LicenseInformation license = CurrentApp.LicenseInformation;
                if (!license.ProductLicenses["allstuff1"].IsActive)
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
            }
            catch (Exception ex)
            {
                MyMessage(ex.Message);
            }
        }

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged;
            ShowHide();
        }

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
            if (Window.Current.Bounds.Width > 1200)
            {
                rightSide.Visibility = Windows.UI.Xaml.Visibility.Visible;
                rightSide.Width = Window.Current.Bounds.Width - 600;
                smallThemesPreview.Width = rightSide.Width - 200;
                ThemeStack.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else 
            {
                rightSide.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                leftSide.Width = 600;
                ThemeStack.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            myFlip.Width = Window.Current.Bounds.Width / 1.3;
        }
    }
}
