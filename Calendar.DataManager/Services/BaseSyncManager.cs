using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calendar.DataManager.Services
{
    public abstract class BaseSyncManager
    {
        /// <summary>
        /// a list of active services
        /// </summary>
        public List<Service> Services { get; set; }

        private static BaseSyncManager _manager;
        public static BaseSyncManager Manager
        {
            get
            {
                if (_manager == null)
                    _manager = new BaseSyncManager();

                return _manager;
            }
        }

        public abstract void AddService(string serverName, DateTime start, int period);
        public abstract int GetServicesCount();
        public abstract void SetIsAnyOperation(int value);
        public abstract Service GetServiceByName(string serviceName);
        public abstract void StartSync(int start);
    }
}
