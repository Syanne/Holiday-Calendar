using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkConnector.Connectors
{
    public class OutlookConnector : BaseConnector
    {
        public OutlookConnector(DateTime dateStart, int period):base(dateStart, period)
        {
        }

        protected override string ServiceName
        {
            get
            {
                return "outlook";
            }
        }

        public override Task GetHolidayList()
        {
            return new Task(() => 
            {

            });
        }
    }
}
