using CalendarResources;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace Calendar
{
    public sealed partial class StartPage : Page
    {      
        public StartPage()
        {
            this.InitializeComponent();
            ResourcesLoaderHere();
        }
        private async void ResourcesLoaderHere()
        {
            //resources
            DataManager.resource = ResourceLoader.GetForCurrentView("Resources");
            try
            {
                Application.Current.Resources.Source = new Uri(ApplicationData.Current.LocalSettings.Values["AppTheme"].ToString());
            }
            catch
            {
                Application.Current.Resources.Source = new Uri("ms-appx:///Themes/Default.xaml");
            }                                  
            DataManager.PersonalData = await DataManager.LoadPersonalData();

            //mechanism
            int Weekend;
            try
            {
                string theDay = ResourceLoader.GetForCurrentView("Resources").GetString("Weekend");
                Weekend = Convert.ToInt32(theDay);
            }
            catch
            {
                Weekend = 5;
            }
            DataManager.calBase = new Mechanism.HolidayCalendarBase(Weekend);

            this.Frame.Navigate(typeof(MainPage));
        }
    }
}
