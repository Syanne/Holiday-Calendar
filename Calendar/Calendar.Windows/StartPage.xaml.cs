using CalendarResources;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Globalization;

namespace Calendar
{
    public sealed partial class StartPage : Page
    {      
        public StartPage()
        {
            this.InitializeComponent();
        }
                
        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            ResourcesLoaderHere();
        }

        private async void ResourcesLoaderHere()
        {
            //resources
            CalendarResourcesManager.resource = ResourceLoader.GetForCurrentView("Resources");
            try
            {
                Application.Current.Resources.Source =
                    new Uri(ApplicationData.Current.LocalSettings.Values["AppTheme"].ToString());
            }
            catch
            {
                Application.Current.Resources.Source =
                    new Uri("ms-appx:///Themes/Default.xaml");
            }                                  
            CalendarResourcesManager.PersonalData = await CalendarResourcesManager.LoadPersonalData();
                                                                              
            //if (CalendarResourcesManager.PersonalData != null)
                this.Frame.Navigate(typeof(MainPage));
        }
    }
}
