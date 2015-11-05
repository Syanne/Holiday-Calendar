﻿using System;
using Windows.ApplicationModel.Store;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону элемента пустой страницы см. по адресу http://go.microsoft.com/fwlink/?LinkID=390556

namespace Calendar
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class StylesPage : Page
    {
        public StylesPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Disabled;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Вызывается перед отображением этой страницы во фрейме.
        /// </summary>
        /// <param name="e">Данные события, описывающие, каким образом была достигнута эта страница.
        /// Этот параметр обычно используется для настройки страницы.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SampleDataSource sds = new SampleDataSource();
            myFlip.ItemsSource = sds.Items;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            //if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            //{
                var data = (myFlip.SelectedItem as LocalFVItem).Tag;
                ApplicationData.Current.RoamingSettings.Values["AppTheme"] =
                    String.Format("ms-appx:///themes/{0}.xaml", data);

                this.Frame.Navigate(typeof(MainPage));
            //}
            //else
            //{
            //    CalendarResources.ShoppingManager.BuyThis("Unlicensed", "UnlicensedTitle", "allstuff1");
            //}
        }

        private void myFlip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(myFlip.Items != null)
            SlideNumberTb.Text = String.Format("{0}/{1}",myFlip.SelectedIndex + 1, myFlip.Items.Count);

            if (myFlip.SelectedIndex == 0)
                leftImg.Visibility = Visibility.Collapsed;
            else if (myFlip.SelectedIndex + 1 == myFlip.Items.Count)
                rightImg.Visibility = Visibility.Collapsed;
            else
            {
                rightImg.Visibility = Visibility.Visible;
                leftImg.Visibility = Visibility.Visible;
            }            
        }
        

    }
}
