using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Calendar.Data.Models;
using Windows.Storage;
using System.Xml;

namespace Calendar.Data.Services
{
    public class PersonalDataResource : BasicDataResource
    {
        protected override string Path
        {
            get { return "PersData.xml"; }
        }

        private string SparePath { get { return "ms-appx:///Strings/PersData.xml"; } }

        public PersonalDataResource()
        {
            try
            {
                var storageFolder = ApplicationData.Current.RoamingFolder;
                Document = LoadFile(Path, storageFolder);

                if (Document == null)
                    throw new Exception();
            }
            //if it's the fist launch - load basic file
            catch
            {
                Document = LoadFile(SparePath, null);
            }
        }

        /// <summary>
        /// Prepare collection of elements.
        /// </summary>
        /// <param name="parentName">parent node name</param>
        /// <param name="extraKey">1 - find by key, anything else - reverses collection</param>
        /// <returns>Collection of elements</returns>
        public override IEnumerable<XElement> GetItemList(string parentName, int? extraKey)
        {
            IEnumerable<XElement> subcollection;

            if (extraKey == 1)
                subcollection = Document.Root.Descendants().Where(x => x.Name.LocalName == parentName);

            //else - find services
            else subcollection = Document.Root.Descendants().Where(x => x.Name.LocalName != "holiday" &&
                                                                           x.Name.LocalName != "holidays" &&
                                                                           x.Name.LocalName != "theme" &&
                                                                           x.Name.LocalName != "persDate");
            return subcollection;
        }
        
        public List<XElement> GetCollectionFromSourceFile(string collectionType)
        {
            if (collectionType == "theme")
                return Document.Root.Descendants("theme").Descendants("holiday").ToList();
            else if (collectionType == "persDate")
                return Document.Root.Descendants("holidays").Descendants("persDate").ToList();
            else return Document.Root.Element(collectionType).Descendants().ToList();
        }

        #region note manipulations

        /// <summary>
        /// add new holiday
        /// </summary>
        /// <param name="name">text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        /// <param name="needSave">do we need to save document now?</param>
        /// <param name="parentTag">parent tag</param>
        public void CreateRecord(string name, int day, int month, int year, bool needSave, string parentNode)
        {
            try
            {
                //add all nodes
                using (XmlWriter writer = Document.Root.Element(parentNode).CreateWriter())
                {
                    writer.WriteStartElement("persDate");

                    writer.WriteStartAttribute("name");
                    writer.WriteString(name);
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("date");
                    writer.WriteString(day.ToString());
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("month");
                    writer.WriteString(month.ToString());
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("year");
                    writer.WriteString(year.ToString());
                    writer.WriteEndAttribute();

                    writer.WriteEndElement();
                }
                //save changes
                if (needSave) SaveDocument();
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }

        /// <summary>
        /// Change note
        /// </summary>
        /// <param name="oldValue">old text</param>
        /// <param name="newValue">changed text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        public void ChangeRecord(string oldValue, string newValue, string day, string month, string year)
        {
            try
            {
                Document.Root.Descendants("holidays").Descendants("persDate").
                    Where(p => (p.Attribute("name").Value == oldValue)).
                    Where(p => (p.Attribute("date").Value == day)).
                    FirstOrDefault().
                    ReplaceAttributes(new XAttribute("name", newValue),
                    new XAttribute("date", day),
                    new XAttribute("month", month),
                    new XAttribute("year", year));

                //save changes
                SaveDocument();
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
        public void RemoveRecord(string name, string day, string month, string year)
        {
            Windows.Data.Xml.Dom.XmlDocument docs = new Windows.Data.Xml.Dom.XmlDocument();
            docs.SelectNodes("sprite");

            try
            {
                //try to load PersData.xml            
                Document.Root.Descendants("holidays").
                    Descendants("persDate").
                    Where(p => (p.Attribute("name").Value == name &&
                    p.Attribute("date").Value == day &&
                    p.Attribute("month").Value == month &&
                    p.Attribute("year").Value == year)).
                        Remove();

                //save changes
                SaveDocument();
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
        private Task RemoveDeprecated()
        {
            return Task.Run(() =>
            {
                //get current date and year
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                foreach (var descendant in Document.Root.Descendants().Skip(1))
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
        /// change the list of holidays
        /// </summary>
        /// <param name="writer">XML writer</param>
        /// <param name="name">name of holiday</param>
        /// <param name="tag">type of holiday</param>
        public void WriteNode(XmlWriter writer, string name, string tag)
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

        #endregion

        #region bg operations

        /// <summary>
        /// Set snooze value
        /// </summary>
        /// <param name="value">snooze</param>
        public void SetToastSnoozeValue(int value)
        {
            Document.Root.Attribute("toast").Value = value.ToString();

            //save changes
            SaveDocument();
        }

        public int GetToastSnoozeValue()
        {
            return Convert.ToInt32(Document.Root.Attribute("toast").Value);
        }

        #endregion

        #region services

        /// <summary>
        /// Save holidays from service 
        /// </summary>
        /// <param name="serviceName">service name as parent attribute name</param>
        /// <param name="period">sync repeat period</param>
        /// <param name="items">colection of records</param>
        /// <param name="Services">list of service's names</param>
        public void SetHolidaysFromSocialNetwork(string serviceName, int period, List<HolidayItem> items, List<string> Services)
        {
            //get parent
            XElement serviceRoot = null;
            DateTime nextSyncDate = DateTime.Now.AddDays(period);
            try
            {
                serviceRoot = Document.Root.Element(serviceName);
                serviceRoot.Attribute("nextSyncDate").Value = nextSyncDate.Date.ToString(DataManager.DateFormat);
                serviceRoot.Attribute("period").Value = period.ToString();
            }
            catch
            {
                using (XmlWriter writer = Document.Root.CreateWriter())
                {
                    writer.WriteStartElement(serviceName);

                    writer.WriteStartAttribute("period");
                    writer.WriteString(period.ToString());
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("nextSyncDate");
                    writer.WriteString(nextSyncDate.Date.ToString(DataManager.DateFormat));
                    writer.WriteEndAttribute();
                    writer.WriteStartAttribute("isActive");
                    writer.WriteString("true");
                    writer.WriteEndAttribute();

                    writer.WriteString("");
                    writer.WriteEndElement();
                }
            }

            //write collectio
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
                            CreateRecord(item.HolidayName, item.Day, item.Month, item.Year, false, serviceName);

                    }
                }
                catch
                {
                    foreach (var item in items)
                        CreateRecord(item.HolidayName, item.Day, item.Month, item.Year, false, serviceName);
                }

            SaveDocument();
        }

        /// <summary>
        /// Disable service sync
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <param name="value">true or false</param>
        public bool SetServiceState(string serviceName, bool value)
        {
            try
            {
                Document.Root.Element(serviceName).Attribute("isActive").Value = value.ToString().ToLower();
                if (!value)
                {
                    //Services.Remove(serviceName);
                    Document.Root.Element(serviceName).Attribute("nextSyncDate").Value = DateTime.Now.Date.ToString(DataManager.DateFormat);
                }
                SaveDocument();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void WriteHolidayTypes(Dictionary<string, string> basicHolidays)
        {
            //remove all nodes
            Document.Root.Descendants("theme").Descendants().Remove();

            //add all nodes
            foreach(var element in basicHolidays)
            {
                using (XmlWriter writer = Document.Root.Descendants().ElementAt(0).CreateWriter())
                {
                    DataManager.AddHolidayCategory(writer, element.Value, element.Key);
                }
            }

            SaveDocument();
        }

        public string GetRootStraightDescendantAttributeValue(string straightDescendantName, string attributeName)
        {
            return Document.Root.Element(straightDescendantName).Attribute(attributeName).Value;
        }
        
        #endregion
        /// <summary>
        /// Saves changed personal data
        /// </summary>
        protected async override void SaveDocument()
        {
            try
            {
                StorageFile sampleFile = await ApplicationData.Current.RoamingFolder.
                     CreateFileAsync(Path, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(sampleFile, Document.ToString());
            }
            catch (Exception e)
            {
                MyMessage(e.Message);
            }
        }
    }
}
