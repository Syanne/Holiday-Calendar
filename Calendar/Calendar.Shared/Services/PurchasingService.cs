using System;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Calendar.Services
{
    /// <summary>
    /// Purchasing services and data
    /// </summary>
    public partial class PurchasingService
    {
        /// <summary>
        /// includes all services
        /// </summary>
        public const string ALL_STUFF = "allstuff1";

        /// <summary>
        /// smartTile and toast
        /// </summary>
        public const string BACKGROUND_SERVICES = "bgservices";
                
        /// <summary>
        /// social network services
        /// </summary>
        public const string SOCIAL_NETWORK = "socialnetworkplus";

        public bool? toggle;

        public LicenseInformation License { get; private set; }


        public PurchasingService()
        {
            License = CurrentApp.LicenseInformation;
            toggle = null;
        }

        /// <summary>
        /// Offer user to purchase the app
        /// </summary>
        /// <param name="content">content key in resources file</param>
        /// <param name="title">title key in resources file or null</param>
        public async void OfferPurchase(string content, string title)
        {
            MessageDialog dial;
            if (title != null)
                dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString(content),
                    ResourceLoader.GetForCurrentView("Resources").GetString(title));
            else dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString(content));

            dial.Commands.Add(new UICommand("OK"));            
            var command = await dial.ShowAsync();
        }

        public async void BuyStuff(string packageName)
        {
            toggle = false;
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses[packageName].IsActive)
            {
                try
                {
                    await CurrentApp.RequestProductPurchaseAsync(packageName);
                    toggle = true;
                }
                catch (Exception ex)
                {
                    toggle = false;
                    MyMessage(ex.Message);
                }

            }
            else toggle = true;
        }

        public async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }  
    }
}
