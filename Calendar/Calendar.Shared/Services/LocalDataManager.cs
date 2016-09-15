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
            LoadPersonalData(true);
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
        public static void SetHolidaysFromSocialNetwork(string serviceName, int period, List<HolidayItem> items)
        {
            //set service
            if (Services == null)
                Services = new List<string>();
            Services.Add(serviceName);

            //set collection
            persDataResource.SetHolidaysFromSocialNetwork(serviceName, period, items, Services);
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
                if (!value)
                    Services.Remove(serviceName);
                persDataResource.SetServiceState(serviceName, value);
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
                var dateString = persDataResource.GetRootStraightDescendantAttributeValue(serviceName, "nextSyncDate");
                var array = dateString.Split(DateSeparator);
                date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                var isActiveString = persDataResource.GetRootStraightDescendantAttributeValue(serviceName, "isActive");
                if (isActiveString == "false")
                    ChangeServiceState(serviceName, true);
            }
            catch
            {
                date = DateTime.Now;
            }
            finally
            {
                if (date.Day >= DateTime.Now.Day && date.Month >= DateTime.Now.Month && date.Year >= DateTime.Now.Year)
                    SyncManager.Manager.AddService(serviceName, DateTime.Now, period);
            }
        }

        /// <summary>
        /// Find all active services and enable it
        /// </summary>
        private static void EnableService()
        {
            //find services
            var subcollection = persDataResource.GetItemList(null, -1);
            if (subcollection.Count() > 0)
            {
                Services = new List<string>();
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
                    var period = int.Parse(persDataResource.GetRootStraightDescendantAttributeValue("google", "period"));
                    var stringToArray = persDataResource.GetRootStraightDescendantAttributeValue("google", "nextSyncDate");
                    var array = stringToArray.Split(DateSeparator);
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
            persDataResource.WriteHolidayTypes(basicHolidays);
        }
    }
}
