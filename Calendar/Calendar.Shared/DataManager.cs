﻿using Calendar.Mechanism;
using Calendar.Models;
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
        public static HolidayCalendarBase calBase;

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
            });
        }
        
        /// <summary>
        /// Log exceptions
        /// </summary>
        /// <param name="e">exception message</param>
        public static async void Logging(Exception e)
        {
            StorageFile sampleFile = await ApplicationData.Current.RoamingFolder.
                 CreateFileAsync("Log.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(sampleFile, e.Message);
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
        public static void SavePersonal(string name, string day, string month, string year, bool needSave)
        {
            try
            {
                //add all nodes
                using (XmlWriter writer = PersonalData.Root.Element("holidays").CreateWriter())
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
                if(needSave) SaveDocumentAsync();
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
                    Where(p => (p.Attribute("name").Value == name)).
                    Where(p => (p.Attribute("date").Value == day)).
                    Where(p => (p.Attribute("month").Value == month)).
                    Where(p => (p.Attribute("year").Value == year)).
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
        /// Save holidays from service 
        /// </summary>
        /// <param name="serviceName">service name as parent attribute name</param>
        /// <param name="period">sync repeat period</param>
        /// <param name="items">colection of items</param>
        public static void SetHolidaysFromSocialNetwork(string serviceName, int period, System.Collections.Generic.List<ItemBase> items)
        {
            //get parent
            XElement serviceRoot = null;
            DateTime nextSyncDate = DateTime.Now.AddDays(period);
            try
            {
                serviceRoot = PersonalData.Root.Element(serviceName);
                serviceRoot.Attribute("nextSyncDate").Value = nextSyncDate.Date.ToString();
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
                    writer.WriteString(nextSyncDate.Date.ToString());
                    writer.WriteEndAttribute();

                    writer.WriteString("");
                    writer.WriteEndElement();
                }
            }

            //set holidays
            if (items != null)
                foreach (var item in items)
                {
                    SavePersonal(item.HolidayName, item.Day.ToString(), item.Month.ToString(), item.Year.ToString(), false);
                }

            SaveDocumentAsync();
        }
        

        private static async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
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
