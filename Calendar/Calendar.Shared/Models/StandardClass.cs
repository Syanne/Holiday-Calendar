using Windows.UI.Xaml;

namespace Calendar
{

    /// <summary>
    /// Size corrector, actually.
    /// Makes the application's GUI scalable
    /// </summary>
    public class StandardClass
    {
        /// <summary>
        /// Width and Height of calendar's item
        /// </summary>
        public double ItemSizeCorrector { get; set; }

        public double MonthTopStringCorrector { get; set; }
        /// <summary>
        /// Width and Height of decade's item
        /// </summary>
        public double DecadeSizeCorrector { get; set; }
        /// <summary>
        /// FontSize of calendar's item
        /// </summary>
        //public double ItemFontSizeCorrector { get; set; }

        public StandardClass()
        {
            ItemSizeCorrector = Window.Current.Bounds.Width / 8;
            MonthTopStringCorrector = ItemSizeCorrector * 7 + 24;
            DecadeSizeCorrector = Window.Current.Bounds.Width / 5;
            //ItemFontSizeCorrector = Window.Current.Bounds.Width / 12;
        }
    }
}
