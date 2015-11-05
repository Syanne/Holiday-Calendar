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

            if (Window.Current.Bounds.Width < 1200)
            {
                rightSide.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                ThemeStack.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

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
            //if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            //{
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
            //}
            //else
            //{
            //    BuyThis();
            //}
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
            TileToggle("TileBackgroundTask", "BackgroundTasks.TileBackgroundTask");
        }

        private void toastToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToastToggle("ToastBackgroundTask", "BackgroundTasks.ToastBackgroundTask");
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses["allstuff1"].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplicationLanguages.PrimaryLanguageOverride = (comboLang.SelectedItem as ComboBoxItem).Content.ToString();
            ApplicationData.Current.RoamingSettings.Values["Language"] = (comboLang.SelectedItem as ComboBoxItem).Content;

            MyMessage(CalendarResourcesManager.resource.GetString("Restart"));
        }

        //private void comboDOW_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ApplicationData.Current.RoamingSettings.Values["Weekend"] = (comboDOW.SelectedItem as ComboBoxItem).Tag;
            
        //    MyMessage(CalendarResourcesManager.resource.GetString("Restart"));
        //}
        
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
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            {
                if (toastToggle.IsOn)
                {
                    CalendarResourcesManager.PersonalData.Root.Attribute("toast").Value = (comboToast.SelectedIndex + 1).ToString();
                    //save changes
                    CalendarResourcesManager.SaveDocumentAsync();

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
                BuyThis();
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

        private async void BuyButtonController()
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses["allstuff1"].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);
                }
            }
        }


        private async void BuyThis()
        {
            var dial = new MessageDialog(CalendarResourcesManager.resource.GetString("Unlicensed"));

            dial.Commands.Add(new UICommand(CalendarResourcesManager.resource.GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(CalendarResourcesManager.resource.GetString("UnlicensedButton"),
            new UICommandInvokedHandler((args) =>
            {
                BuyStuff();
            })));
            var command = await dial.ShowAsync();
        }

        private async void BuyStuff()
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses["allstuff1"].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync("allstuff1");
                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);
                }
            }
        }

        #endregion


    }
}
