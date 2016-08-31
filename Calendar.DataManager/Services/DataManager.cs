using Calendar.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Popups;

namespace Calendar.Data.Services
{
    public partial class DataManager
    {
        /// <summary>
        /// unified date format for all descentants
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// unified separator for date split
        /// </summary>
        public const char DateSeparator = '-';

        protected static XDocument PersonalData { get; private set; }
        protected static XDocument doc { get; private set; }
        public static ResourceLoader Resource { get; set; }

        public static Dictionary<string, string> SelectedCategories { get; private set; }

        /// <summary>
        /// Load Holidays Data (general, then personal)
        /// </summary>
        protected static void LoadPersonalData()
        {
            //basic collection
            doc = LoadFile("ms-appx:///Strings/Holidays.xml", null);

            //personal
            try
            {
                var storageFolder = ApplicationData.Current.RoamingFolder;
                PersonalData = LoadFile("PersData.xml", storageFolder);

                if (PersonalData == null)
                    throw new Exception();
            }
            //if it's the fist launch - load basic file
            catch
            {
                PersonalData = LoadFile("ms-appx:///Strings/PersData.xml", null);
            }

            SetSelectedCategoriesList();
        }

        protected static void SetSelectedCategoriesList()
        {
            SelectedCategories = new Dictionary<string, string>();

            //get themes (categories)
            var collection = GetCollectionFromSourceFile("theme");
            var keys = collection.Select(x => x.Attribute("name").Value).ToList();
            var values = collection.Select(x => x.Attribute("desc").Value).ToList();

            //set dictionary
            for (int i = 0; i < keys.Count; i++)
                SelectedCategories.Add(keys[i], values[i]);
        }

        public static void SetToastSnoozeValue(string value)
        {
            PersonalData.Root.Attribute("toast").Value = value; 
            
            //save changes
            SaveDocumentAsync();
        }

        public static string GetToastSnoozeValue()
        {
            return PersonalData.Root.Attribute("toast").Value;
        }

        /// <summary>
        /// Loads file from path
        /// </summary>
        /// <param name="filepath">path to a file</param>
        /// <param name="folder">folder or null (to load from application folder)</param>
        /// <returns></returns>
        private static XDocument LoadFile(string filepath, StorageFolder folder)
        {
            try
            {
                StorageFile file;

                //load file
                if (folder == null)
                {
                    var uri = new Uri(filepath);
                    file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    file = folder.GetFileAsync(filepath).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }

                //parse and return
                var result = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                return XDocument.Parse(result);
            }
            catch
            {
                return null;
            }
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
        
        private static async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        #region SmartTile
        public static bool SmartTileFile(string snooze)
        {
            XDocument SmartTileFie = new XDocument();

            //try load
            try
            {
                var storageFolder = ApplicationData.Current.LocalFolder;
                var file = storageFolder.GetFileAsync("SmartTileFile.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                SmartTileFie = XDocument.Parse(text);

                SmartTileFie.Root.Attribute("refreshmentDate").SetValue(DateTime.Now.Date.AddDays(-1).ToString(DateFormat));
                SmartTileFie.Root.Attribute("daysAmount").SetValue(snooze);

                //save changes
                //save changes
                SaveSmartTileFile(SmartTileFie);

                return true;
            }
            //elseway - try create
            catch
            {
                try
                {
                    //create file
                    SmartTileFie = new XDocument();
                    SmartTileFie = new XDocument(new XElement("root", SmartTileFie.Root));
                    SmartTileFie.Root.Value = "";

                    using (XmlWriter writer = SmartTileFie.Root.CreateWriter())
                    {
                        writer.WriteStartAttribute("firstElement");
                        writer.WriteString("0");
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("refreshmentDate");
                        writer.WriteString(DateTime.Now.Date.AddDays(-1).ToString(DataManager.DateFormat));
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("daysAmount");
                        writer.WriteString(snooze);
                        writer.WriteEndAttribute();
                    }

                    //save changes
                    SaveSmartTileFile(SmartTileFie);

                    return true;
                }
                catch
                {
                    SmartTileFie = null;
                    return false;
                }
            }
        }
        
        private static async void SaveSmartTileFile(XDocument document)
        {
            if(document != null)
            try
            {
                StorageFile sampleFile = await ApplicationData.Current.LocalFolder.
                     CreateFileAsync("SmartTileFile.xml", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(sampleFile, document.ToString());
            }
            catch { }
        }
        #endregion


        public static List<DocElement> GetCollectionFromSourceFile(DateTime SelectedDate, string collectionType)
        {
            var holidayCollection = new List<DocElement>();

            foreach (var element in doc.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants(collectionType))
            {
                //category
                string theme = element.Parent.Attribute("name").Value;
                bool value = (SelectedCategories.Keys.Count(val => val == theme) == 1) ? true : false;

                if (element.FirstAttribute.Value != "" && value == true)
                    holidayCollection.Add(new DocElement
                    {
                        Key = theme,
                        Value = element
                    });
            }
            
            return holidayCollection;
        }

        public static List<XElement> GetCollectionFromSourceFile(string collectionType)
        {
            List<XElement> holidayCollection = new List<XElement>();

            if (collectionType == "categories")
                holidayCollection = doc.Root.Descendants("month").ElementAt(0).Descendants("holiday").ToList();
            else if (collectionType == "theme")
                holidayCollection = PersonalData.Root.Descendants("theme").Descendants("holiday").ToList();
            else if (collectionType == "persDate")
                DataManager.PersonalData.Root.Descendants("holidays").Descendants("persDate");
            else holidayCollection = PersonalData.Root.Element(collectionType).Descendants().ToList();

            return holidayCollection;
        }

    }
}
