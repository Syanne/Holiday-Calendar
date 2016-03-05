using Windows.UI.Xaml;

namespace Calendar
{

    /// <summary>
    /// Size corrector for mobile app
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
        public double ItemFontSizeCorrector { get; set; }

        public double NoteGridHeight { get; set; }

        public double NoteWidth { get; set; }

        public double CalGridHeight { get; set; }

        public StandardClass()
        {
            ItemSizeCorrector = Window.Current.Bounds.Width / 8;
            MonthTopStringCorrector = ItemSizeCorrector * 7 + 24;
            DecadeSizeCorrector = Window.Current.Bounds.Width / 5;
            ItemFontSizeCorrector = ItemSizeCorrector / 2;
            NoteGridHeight = Window.Current.Bounds.Height - 370;
            NoteWidth = Window.Current.Bounds.Width;
            CalGridHeight = ItemSizeCorrector * 7;
        }
    }
}
