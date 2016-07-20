using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialNetworkConnector
{
    public sealed class ItemBase
    {
        public int Day { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public string HolidayName { get; set; }
        public string HolidayTag { get; set; }
    }
}
