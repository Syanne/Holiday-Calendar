using Calendar.Mechanism;
using Calendar.Models;
using Calendar.SocialNetworkConnector;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.UI.Popups;

namespace CalendarResources
{
    public class DataManager
    {
        public static XDocument PersonalData { get; private set; }
        public static XDocument doc { get; private set; }
        public static ResourceLoader resource { get; set; }
        public static HolidayCalendarBase calBase { get; set; }
        public static System.Collections.Generic.List<string> Services { get; private set; }

        /// <summary>
        /// Load Holidays Data (general, then personal)
        /// </summary>
        public static Task LoadPersonalData()
        {
            return Task.Run(() =>
            {
                //basic collection
                Uri uri = new Uri("ms-appx:///Strings/Holidays.xml");
#if !WINDOWS_PHONE_APP
                var holFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                doc = XDocument.Load(holFile.Path);
#else

                var holFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                var holidaysFile = FileIO.ReadTextAsync(holFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                doc = XDocument.Parse(holidaysFile);
#endif
                //personal
                try
                {
                    var storageFolder = ApplicationData.Current.RoamingFolder;
                    var file = storageFolder.GetFileAsync("PersData.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                    var personalDoc = XDocument.Parse(text);

                    PersonalData = personalDoc;
                }
                //if it's the fist launch - load basic file
                catch
                {
                    var persUri = new Uri("ms-appx:///Strings/PersData.xml");
                    var persFile = StorageFile.GetFileFromApplicationUriAsync(persUri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    var persRead = FileIO.ReadTextAsync(persFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    PersonalData = XDocument.Parse(persRead);
                }

            #if !WINOWS_PHONE_APP
                EnableService();
            #endif

            });
        }

        /// <summary>
        /// Saves changed personal data
        /// </summary>
        public static async void SaveDocumentAsync()
        {
            try
            {
                StorageFile sampleFile = await ApplicationData.Current.RoamingFolder.
                     CreateFileAsync("PersData.xml", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(sampleFile, PersonalData.ToString());
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }

        /// <summary>
        /// change the list of holidays
        /// </summary>
        /// <param name="writer">XML writer</param>
        /// <param name="name">name of holiday</param>
        /// <param name="tag">type of holiday</param>
        public static void WriteNode(XmlWriter writer, string name, string tag)
        {
            writer.WriteStartElement("holiday");

            writer.WriteStartAttribute("desc");
            writer.WriteString(name);
            writer.WriteEndAttribute();
            writer.WriteStartAttribute("name");
            writer.WriteString(tag);
            writer.WriteEndAttribute();

            writer.WriteEndElement();
        }

        /// <summary>
        /// add new holiday
        /// </summary>
        /// <param name="name">text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        /// <param name="needSave">do we need to save document now?</param>
        /// <param name="parentTag">parent tag</param>
        public static void SavePersonal(string name, string day, string month, string year, bool needSave, string parentTag)
        {
            try
            {
                //add all nodes
                using (XmlWriter writer = PersonalData.Root.Element(parentTag).CreateWriter())
                {
                    writer.WriteStartElement("persDate");

                    writer.WriteStartAttribute("name");
                    writer.WriteString(name);
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("date");
                    writer.WriteString(day);
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("month");
                    writer.WriteString(month);
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("year");
                    writer.WriteString(year);
                    writer.WriteEndAttribute();

                    writer.WriteEndElement();
                }
                //save changes
                if (needSave) SaveDocumentAsync();
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }

        /// <summary>
        /// Change note
        /// </summary>
        /// <param name="oldName">old text</param>
        /// <param name="newName">changed text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        public static void ChangePersonal(string oldName, string newName, string day, string month, string year)
        {
            try
            {
                PersonalData.Root.Descendants("holidays").Descendants("persDate").
                    Where(p => (p.Attribute("name").Value == oldName)).
                    Where(p => (p.Attribute("date").Value == day)).
                    FirstOrDefault().
                    ReplaceAttributes(new XAttribute("name", newName),
                    new XAttribute("date", day),
                    new XAttribute("month", month),
                    new XAttribute("year", year));

                //save changes
                SaveDocumentAsync();
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }

        /// <summary>
        /// Remove note
        /// </summary>
        /// <param name="startText"></param>
        public static void RemoveHoliday(string name, string day, string month, string year)
        {
            try
            {
                //try to load PersData.xml            
                PersonalData.Root.Descendants("holidays").
                    Descendants("persDate").
                    Where(p => (p.Attribute("name").Value == name &&
                    p.Attribute("date").Value == day &&
                    p.Attribute("month").Value == month &&
                    p.Attribute("year").Value == year)).
                        Remove();

                //save changes
                SaveDocumentAsync();
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }

        /// <summary>
        /// Removes records where year and month are less then current
        /// </summary>
        /// <returns>asyn operation</returns>
        private static Task RemoveDeprecated()
        {
            return Task.Run(() =>
            {
                //get current date and year
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                foreach (var descendant in PersonalData.Root.Descendants().Skip(1))
                {
                    descendant.Descendants().Where(p =>
                        int.Parse(p.Attribute("month").Value) < month &&
                        int.Parse(p.Attribute("year").Value) < year &&
                        int.Parse(p.Attribute("year").Value) != 0).
                            Remove();
                }
            });
        }


        /// <summary>
        /// Save holidays from service 
        /// </summary>
        /// <param name="serviceName">service name as parent attribute name</param>
        /// <param name="period">sync repeat period</param>
        /// <param name="items">colection of records</param>
        public static void SetHolidaysFromSocialNetwork(string serviceName, int period, System.Collections.Generic.List<ItemBase> items)
        {
            //set service
            if (Services == null)
                Services = new System.Collections.Generic.List<string>();
            Services.Add(serviceName);

            //get parent
            XElement serviceRoot = null;
            DateTime nextSyncDate = DateTime.Now.AddDays(period);
            try
            {
                serviceRoot = PersonalData.Root.Element(serviceName);
                serviceRoot.Attribute("nextSyncDate").Value = nextSyncDate.Date.ToString(Calendar.SocialNetworkConnector.BaseConnector.DateFormat);
                serviceRoot.Attribute("period").Value = period.ToString();
            }
            catch
            {
                using (XmlWriter writer = PersonalData.Root.CreateWriter())
                {
                    writer.WriteStartElement(serviceName);

                    writer.WriteStartAttribute("period");
                    writer.WriteString(period.ToString());
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("nextSyncDate");
                    writer.WriteString(nextSyncDate.Date.ToString(Calendar.SocialNetworkConnector.BaseConnector.DateFormat));
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("isActive");
                    writer.WriteString("true");
                    writer.WriteEndAttribute();

                    writer.WriteString("");
                    writer.WriteEndElement();
                }
            }

            if (items != null)
                try
                {
                    //set holidays
                    foreach (var item in items)
                    {
                        //first - check if there any same record
                        int count = serviceRoot.Descendants("persDate").
                            Where(p => (p.Attribute("name").Value == item.HolidayName) &&
                            p.Attribute("date").Value == item.Day.ToString() &&
                            p.Attribute("month").Value == item.Month.ToString() &&
                            p.Attribute("year").Value == item.Year.ToString()).
                            Count();

                        if (count == 0)
                            SavePersonal(item.HolidayName, item.Day.ToString(), item.Month.ToString(), item.Year.ToString(), false, serviceName);

                    }
                }
                catch
                {
                    foreach (var item in items)
                        SavePersonal(item.HolidayName, item.Day.ToString(), item.Month.ToString(), item.Year.ToString(), false, serviceName);
                }

            SaveDocumentAsync();
        }

        /// <summary>
        /// Disable service sync
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <param name="value">true or false</param>
        public static void ChangeServiceState(string serviceName, bool value)
        {
            PersonalData.Root.Element(serviceName).Attribute("isActive").Value = value.ToString().ToLower();
            if (!value)
            {
                Services.Remove(serviceName);
                PersonalData.Root.Element("google").Attribute("nextSyncDate").Value = DateTime.Now.Date.ToString(BaseConnector.DateFormat);
            }
            SaveDocumentAsync();
        }

        private static async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        /// <summary>
        /// Find all active services and enable it
        /// </summary>
        private static void EnableService()
        {
            //find services
            var subcollection = PersonalData.Root.Descendants().Where(x => x.Name.LocalName != "holiday" &&
                                                                               x.Name.LocalName != "holidays" &&
                                                                               x.Name.LocalName != "theme" &&
                                                                               x.Name.LocalName != "persDate");
            if (subcollection.Count() > 0)
            {
                Services = new System.Collections.Generic.List<string>();
                foreach (var item in subcollection)
                {
                    if (item.Attribute("isActive").Value == "true")
                        Services.Add(item.Name.LocalName);
                }
            }
            
            //enable active
            if (Services != null)
                if (Services.Contains("google"))
                {
                    var period = int.Parse(DataManager.PersonalData.Root.Element("google").Attribute("period").Value);
                    var array = DataManager.PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(Calendar.SocialNetworkConnector.BaseConnector.DateSeparator);
                    var date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                    if (date.Day <= DateTime.Now.Day && date.Month <= DateTime.Now.Month && date.Year <= DateTime.Now.Year)
                    {
                        SyncManager.Manager.AddService("google", DateTime.Now, period);
                    }
                }
        }
    }
    

    /// <summary>
    /// Buy this application, please!
    /// </summary>
    public class ShoppingManager
    {
        public static async void BuyThis(string content, string title, string packageName)
        {            
            var dial = new MessageDialog(ResourceLoader.GetForCurrentView("Resources").GetString(content),
                ResourceLoader.GetForCurrentView("Resources").GetString(title));

            dial.Commands.Add(new UICommand(ResourceLoader.GetForCurrentView("Resources").GetString("UnlicensedCancel")));
            dial.Commands.Add(new UICommand(ResourceLoader.GetForCurrentView("Resources").GetString("UnlicensedButton"),
            new UICommandInvokedHandler((args) =>
            {
                CalendarResources.ShoppingManager.BuyStuff(packageName);
            })));
            var command = await dial.ShowAsync();
        }

        public static async void BuyStuff(string packageName)
        {
            LicenseInformation license = CurrentApp.LicenseInformation;
            if (!license.ProductLicenses[packageName].IsActive)
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

        private static async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }
    }
}
