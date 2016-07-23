using System;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;

using CalendarResources;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Calendar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            styleTitle.Foreground = Light;

            //enable task toggles
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "TileBackgroundTask")
                    tileToggle.IsOn = true;
                if (task.Value.Name == "ToastBackgroundTask")
                {
                    toastToggle.IsOn = true;
                    comboPeriod.IsEnabled = false;
                    comboToast.IsEnabled = false;
                }
            }            
        }             
          
        #region themes        
        private void cancelBth_Click(object sender, RoutedEventArgs e)
        {
            HideStylesPanel();
        }

        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            {
                string temp = (myFlip.SelectedItem as LocalFVItem).Tag;

                ApplicationData.Current.LocalSettings.Values["AppTheme"] =
                    String.Format("ms-appx:///themes/{0}.xaml", temp);
                Application.Current.Resources.Source =
                   new Uri("ms-appx:///themes/" + temp + ".xaml");

                HideStylesPanel();
            }
            else
            {
                OfferPurchase();
            }
        }

        private void buttonTheme_Click(object sender, RoutedEventArgs e)
        {
            themesFullScreen.Visibility = Visibility.Visible;
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
            this.Frame.Navigate(typeof(MainPage));
        }
        
        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
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
                        comboPeriod.IsEnabled = false;
                        comboToast.IsEnabled = false;
                    }
                    else
                    {
                        foreach (var task in BackgroundTaskRegistration.AllTasks)
                            if (task.Value.Name == "ToastBackgroundTask")
                                task.Value.Unregister(true);
                        comboPeriod.IsEnabled = true;
                        comboToast.IsEnabled = true;
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

        private void topString_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if((sender as GridViewItem).Name.CompareTo(pageTitle.Name) == 0)
            {
                HideStylesPanel();
            }
            else
            {
                //load themes and panel
                if (myFlip.Items.Count == 0)
                {
                    //FlipViews (small and full-screen)
                    SampleDataSource sds = new SampleDataSource();
                    myFlip.ItemsSource = sds.Items;
                }

                themesFullScreen.Visibility = Visibility.Visible;
                pageTitle.Foreground = Light;
                styleTitle.Foreground = Dark;
            }
        }

        private void HideStylesPanel()
        {
            themesFullScreen.Visibility = Visibility.Collapsed;
            pageTitle.Foreground = Dark;
            styleTitle.Foreground = Light;
        }
    }
}
