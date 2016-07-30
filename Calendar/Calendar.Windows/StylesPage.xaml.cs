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
    public sealed partial class StylesPage : Page
    {
        public StylesPage()
        {
            this.InitializeComponent();

            //FlipViews (small and full-screen)
            SampleDataSource sds = new SampleDataSource();
            myFlip.ItemsSource = sds.Items;
        }             
          
        #region themes        
        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            {
                string temp = (myFlip.SelectedItem as LocalFVItem).Tag;

                ApplicationData.Current.LocalSettings.Values["AppTheme"] =
                    String.Format("ms-appx:///themes/{0}.xaml", temp);
                Application.Current.Resources.Source =
                   new Uri("ms-appx:///themes/" + temp + ".xaml");
            }
            else
            {
                OfferPurchase();
            }
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

        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }
        

        private void myFlip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myFlip.Items != null)
                SlideNumberTb.Text = String.Format("{0}/{1}", myFlip.SelectedIndex + 1, myFlip.Items.Count);
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

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void SettingsFlyout_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
        /// <summary>
        /// Changing property Height for gvMain and choosing, what to show -list or grid
        /// </summary>
        private void ShowHide()
        { 
            myFlip.Width = Window.Current.Bounds.Width / 1.3;
        }

        private void cancelBth_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
