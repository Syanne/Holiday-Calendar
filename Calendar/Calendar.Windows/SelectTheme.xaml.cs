using Calendar.Common;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using Windows.Storage;
using System.Globalization;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using Windows.ApplicationModel.Store;
using Windows.ApplicationModel.Resources;
using Calendar.Models;


// Шаблон элемента страницы сведений об элементе задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234232

namespace Calendar
{
    /// <summary>
    /// Страница, на которой отображаются сведения об отдельном элементе внутри группы; при этом можно с помощью жестов
    /// перемещаться между другими элементами из этой группы.
    /// </summary>
    public sealed partial class SelectTheme : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private ObservableCollection<LvItem> itemSource;


        /// <summary>
        /// Эту настройку можно изменить на модель строго типизированных представлений.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper используется на каждой странице для облегчения навигации и 
        /// управление жизненным циклом процесса
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public SelectTheme()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;

            LoadDocument();

            Items.ItemsSource = itemSource;
            namePreview.Text = (Items.Items[0] as LvItem).Text;
        }

        private void LoadDocument()
        {
            itemSource = new ObservableCollection<LvItem>();

            var language = Windows.System.UserProfile.GlobalizationPreferences.Languages.ToList();
            var single = (language.Where(l => l.Contains(CultureInfo.CurrentCulture.TwoLetterISOLanguageName)).Count() == 0) ?
                CultureInfo.DefaultThreadCurrentCulture.TwoLetterISOLanguageName :
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            XDocument doc = XDocument.Load(@"Holidays/" + single + "/Themes.xml");

            var collection = doc.Root.Descendants("theme");

            foreach (XElement holiday in collection)
                itemSource.Add(new LvItem
                {
                    Name = String.Format("/SelectTheme/{0}.png", holiday.FirstAttribute.Value),
                    Text = holiday.LastAttribute.Value,
                    Tag = holiday.FirstAttribute.Value
                });
        }

        /// <summary>
        /// Заполняет страницу содержимым, передаваемым в процессе навигации.  Также предоставляется любое сохраненное состояние
        /// при повторном создании страницы из предыдущего сеанса.
        /// </summary>
        /// <param name="sender">
        /// Источник события; как правило, <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Данные события, предоставляющие параметр навигации, который передается
        /// <see cref="Frame.Navigate(Type, Object)"/> при первоначальном запросе этой страницы и
        /// словарь состояний, сохраненных этой страницей в ходе предыдущего
        /// сеанса.  Это состояние будет равно NULL при первом посещении страницы.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            object navigationParameter;
            if (e.PageState != null && e.PageState.ContainsKey("SelectedItem"))
            {
                navigationParameter = e.PageState["SelectedItem"];
            }
        }

        #region Регистрация NavigationHelper

        /// Методы, предоставленные в этом разделе, используются исключительно для того, чтобы
        /// NavigationHelper мог откликаться на методы навигации страницы.
        /// 
        /// Логика страницы должна быть размещена в обработчиках событий для 
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// и <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// Параметр навигации доступен в методе LoadState 
        /// в дополнение к состоянию страницы, сохраненному в ходе предыдущего сеанса.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentApp.LicenseInformation.ProductLicenses["allstuff1"].IsActive)
            {
                ApplicationData.Current.LocalSettings.Values["AppTheme"] =
                    String.Format("ms-appx:///themes/{0}.xaml", chooseBtn.Tag.ToString());

                this.Frame.Navigate(typeof(MainPage));
            }
            else
            {
                BuyThis();
            }
        }

        private async void BuyThis()
        {
            var dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString("Unlicensed"));

            dial.Commands.Add(new UICommand(ResourceLoader.GetForCurrentView("Resources").GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(ResourceLoader.GetForCurrentView("Resources").GetString("UnlicensedButton"),
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
                    //if(license.ProductLicenses["all"].IsActive)

                }
                catch (Exception ex)
                {
                    MyMessage(ex.Message);

                }
            }
        }

        private async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("Назад"));
            var command = await dial.ShowAsync();
        }


        private void StackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var panel = sender as StackPanel;

            themePreview.Source = ImageFromRelativePath(this, String.Format(@"SelectTheme/{0}-700.png", panel.Tag.ToString()));
            chooseBtn.Tag = panel.Tag;
            namePreview.Text = (panel.Children[1] as TextBox).Text;
        }

        public static BitmapImage ImageFromRelativePath(FrameworkElement parent, string path)
        {
            var uri = new Uri(parent.BaseUri, path);
            BitmapImage result = new BitmapImage();
            result.UriSource = uri;
            return result;
        }
        
    }
    
}
