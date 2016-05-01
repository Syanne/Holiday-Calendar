using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Calendar
{
    /// <summary>
    /// Size corrector for windows app
    /// Makes the application's GUI scalable
    /// </summary>
    public class WindowsStandardClass
    {
        private double height;
        public double ApplicationHeight
        {
            get { return height; }
            set { height = (value < 724) ? 724 : value; }
        }

        //calendar
        public double ItemSizeCorrector { get; set; }
        public double ItemFontSizeCorrector { get; set; }
        public double MonthTopStringWidth { get; set; }

        //decades
        public double DecadeHeightCorrector { get; set; }
        public double DecadeWidthCorrector { get; set; }

        /// <summary>
        /// Font Size for notes
        /// </summary>
        public double NoteFontSizeCorrector { get; set; }
        
        /// <summary>
        /// counts new sizes for items
        /// </summary>
        /// <param name="height">application height</param>
        /// <returns>is app height changed</returns>
        public bool Count(double height)
        {
            if (height != this.ApplicationHeight)
            {
                this.ApplicationHeight = height;
                ItemSizeCorrector = this.ApplicationHeight / 12 ;
                ItemFontSizeCorrector = ItemSizeCorrector / 2 - 8;
                MonthTopStringWidth = ItemSizeCorrector * 7 + 24;
                DecadeWidthCorrector = MonthTopStringWidth / 3 - 4;
                DecadeHeightCorrector = ItemSizeCorrector * 4 / 3;
                NoteFontSizeCorrector = (Window.Current.Bounds.Height / 36 > 30) ? 30 : Window.Current.Bounds.Height / 36;

                return true;
            }
            else return false;
        }
    }

}
