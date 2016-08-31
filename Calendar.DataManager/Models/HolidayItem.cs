using Windows.UI.Xaml.Media;

namespace Calendar.Data.Models
{
    /// <summary>
    /// all chosen holidays (from at least 3 categories of holidays)
    /// </summary>
    public class HolidayItem: ItemBase
    {
        public Brush Background { get; set; }
        public double Height { get; set; }
        public double FontSize { get; set; }

        /// <summary>
        /// Clones an object
        /// </summary>
        /// <returns>HolidayItem</returns>
        public HolidayItem Copy()
        {
            return new HolidayItem()
            {
                Day = this.Day,
                Year = this.Year,
                Month = this.Month,
                HolidayName = this.HolidayName,
                HolidayTag = this.HolidayTag,
                Background = this.Background,
                Height = this.Height,
                FontSize = this.FontSize
            };
        }
    }
}