using Windows.UI.Xaml.Media;

namespace Calendar.Services
{
    public class ItemBase
    {
        public int Day { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public string HolidayName { get; set; }
        public string HolidayTag { get; set; }
    }
}