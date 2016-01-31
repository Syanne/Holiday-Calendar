using Calendar.Models;
using System.Collections.Generic;

namespace Calendar.Mechanism
{
    class HolidayNameComparer : IEqualityComparer<HolidayItem>
    {
        public string GetComparablePart(HolidayItem item)
        {
            return item.HolidayName;
        }
        public bool Equals(HolidayItem x, HolidayItem y)
        {
            return GetComparablePart(x).Equals(GetComparablePart(y));
        }

        public int GetHashCode(HolidayItem obj)
        {
            return GetComparablePart(obj).GetHashCode();
        }
    }
}
