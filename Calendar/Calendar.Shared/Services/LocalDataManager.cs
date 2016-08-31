using Calendar.Data.Services;
using Calendar.Data.Models;
using Calendar.Mechanism;
using Calendar.SocialNetworkConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Calendar.Services
{
    public class LocalDataManager: DataManager
    {
        public static HolidayCalendarBase calBase { get; set; }
        public static List<string> Services { get; private set; }

        public static void StartLoad()
        {
            LoadPersonalData();
            #if !WINOWS_PHONE_APP
                EnableService();
            #endif
        }

        #region Social network services
        /// <summary>
        /// Save holidays from service 
        /// </summary>
        /// <param name="serviceName">service name as parent attribute name</param>
        /// <param name="period">sync repeat period</param>
        /// <param name="items">colection of records</param>
        public static void SetHolidaysFromSocialNetwork(string serviceName, int period, List<ItemBase> items)
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
                serviceRoot.Attribute("nextSyncDate").Value = nextSyncDate.Date.ToString(DateFormat);
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
                    writer.WriteString(nextSyncDate.Date.ToString(DateFormat));
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
            try
            {
                PersonalData.Root.Element(serviceName).Attribute("isActive").Value = value.ToString().ToLower();
                if (!value)
                {
                    Services.Remove(serviceName);
                    PersonalData.Root.Element("google").Attribute("nextSyncDate").Value = DateTime.Now.Date.ToString(DateFormat);
                }
                SaveDocumentAsync();
            }
            catch
            {
                Service serv = SyncManager.Manager.GetServiceByName(serviceName);
                if (serv != null)
                    SetHolidaysFromSocialNetwork(serviceName, serv.Period, null);
            }
        }
        
        public static void SetService(string serviceName, int period)
        {
            DateTime date = DateTime.Now;

            try
            {
                var array = LocalDataManager.PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(LocalDataManager.DateSeparator);
                date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                if (PersonalData.Root.Element("google").Attribute("isActive").Value == "false")
                    ChangeServiceState("google", true);
            }
            catch
            {
                date = DateTime.Now;
            }
            finally
            {
                if (date.Day >= DateTime.Now.Day && date.Month >= DateTime.Now.Month && date.Year >= DateTime.Now.Year)
                    SyncManager.Manager.AddService("google", DateTime.Now, period);
            }
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
                    var period = int.Parse(PersonalData.Root.Element("google").Attribute("period").Value);
                    var array = PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(DateSeparator);
                    var date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                    if (date.Day <= DateTime.Now.Day && date.Month <= DateTime.Now.Month && date.Year <= DateTime.Now.Year)
                    {
                        SyncManager.Manager.AddService("google", DateTime.Now, period);
                    }
                }
        }
        #endregion

        /// <summary>
        /// Save changes in file with personal data
        /// </summary>
        /// <param name="basicHolidays">selected kinds of holidays</param>
        public static void WriteHolidayXml(List<string> basicHolidays)
        {
            //remove all nodes
            DataManager.PersonalData.Root.Descendants("theme").Descendants().Remove();

            //add all nodes
            for (int i = 0; i < basicHolidays.Count(); i += 2)
            {
                using (XmlWriter writer = DataManager.PersonalData.Root.Descendants().ElementAt(0).CreateWriter())
                {
                    DataManager.WriteNode(writer, basicHolidays.ElementAt(i), basicHolidays.ElementAt(i + 1));
                }
            }

            SetSelectedCategoriesList();
            SaveDocumentAsync();
        }
    }
}
