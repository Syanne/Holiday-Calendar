using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Calendar.SocialNetworkConnector
{
    public partial class SyncManager
    {
        public bool IsAnyOperation { get; private set; }
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
            IsAnyOperation = false;
        }

        public void SetIsAnyOperation(bool value, Type typename)
        {
            if (typename == typeof(BaseConnector))
                IsAnyOperation = value;
        }

        /// <summary>
        /// Sync with services.
        /// Sync can be started, if there is no other operations 
        /// (property IsAnyOperation == false)
        /// </summary>
        /// <param name="serverName">name of service</param>
        /// <param name="start">start date</param>
        /// <param name="period">repeat period</param>
        public async void SyncWith(string serverName, DateTime start, int period)
        {
            if (!IsAnyOperation)
            {
                //set connector
                BaseConnector connector = null;

                switch (serverName)
                {
                    case "google": connector = new GoogleCalendarConnector(start, period); break;
                    case "facebook": break;
                    case "outlook": break;
                    case "vk": break;
                    default: break;
                }

                //do operation with sync
                IsAnyOperation = true;
                await connector.GetHolidayList();
            }
        }
    }
}
