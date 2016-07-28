using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Calendar.SocialNetworkConnector
{
    public class SyncManager
    {
        public int OnSyncServiceNumber { get; private set; }
        private List<Service> Services { get; set; }
        private static SyncManager _manager;
        public static SyncManager Manager
        {
            get
            {
                if (_manager == null)
                    _manager = new SyncManager();

                return _manager;
            }
        }

        private SyncManager()
        {
            OnSyncServiceNumber = -1;
            Services = new List<Service>();
        }

        public void SetIsAnyOperation(int value)
        {
            OnSyncServiceNumber = value;
        }

        public void AddService(string serverName, DateTime start, int period)
        {
            Services.Add(new Service
            {
                ServerName = serverName,
                DateStart = start,
                Period = period
            });
            StartSync(Services.Count - 1);
        }

        public int GetServicesCount()
        {
            return Services.Count;
        }

        /// <summary>
        /// Sync with services.
        /// Sync can be started, if there is no other operations 
        /// (property IsAnyOperation == false)
        /// </summary>
        public async void StartSync(int start)
        {
            if (OnSyncServiceNumber == -1)
            {
                OnSyncServiceNumber = start;

                //set connector
                BaseConnector connector = null;
                switch (Services[OnSyncServiceNumber].ServerName)
                {
                    case "google":
                        connector = new GoogleCalendarConnector(Services[OnSyncServiceNumber].DateStart,
                                                                Services[OnSyncServiceNumber].Period);
                        break;
                    case "facebook": /*connector = new FacebookConnector(Services[OnSyncServiceNumber].Period);*/ break;
                    case "outlook": break;
                    case "vk": break;
                    default: break;
                }

                //do sync
                await connector.GetHolidayList();
            }
        }
        class Service
        {
            public string ServerName;
            public int Period;
            public DateTime DateStart;
        }
    }
}
