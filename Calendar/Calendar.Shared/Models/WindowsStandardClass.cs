using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Calendar
{
    /// <summary>
    /// Size corrector, actually.
    /// Makes the application's GUI scalable
    /// </summary>
    public class WindowsStandardClass
    {
        private double height;
        public double ApplicationHeight {
            get
            {
                return height;
            }
            set
            {
                if (value < 724)
                    height = 724;
                else height = value;
            }
        }

        //calendar
        public double ItemSizeCorrector { get; set; }
        public double ItemFontSizeCorrector { get; set; }
        public double MonthTopStringWidth { get; set; }

        //decades
        public double DecadeHeightCorrector { get; set; }
        public double DecadeWidthCorrector { get; set; }

        public double NoteFontSizeCorrector { get; set; }
        
        public bool Count(double height)
        {
            if (height != this.ApplicationHeight)
            {
                this.ApplicationHeight = height;
                ItemSizeCorrector = this.ApplicationHeight / 12;
                ItemFontSizeCorrector = ItemSizeCorrector / 2;
                MonthTopStringWidth = ItemSizeCorrector * 7 + 24;
                DecadeWidthCorrector = MonthTopStringWidth / 3 - 20;
                DecadeHeightCorrector = ItemSizeCorrector * 2 - ItemFontSizeCorrector;
                NoteFontSizeCorrector = (Window.Current.Bounds.Height / 36 > 30) ? 30 : Window.Current.Bounds.Height / 36;

                return true;
            }
            else return false;
        }
    }


    public class ResizeFlipView
    {
        public double MinWidth { get; set; }
        public double MinHeight { get; set; }
        public double StackPanelWidth { get; set; }
        public ResizeFlipView()
        {
            if (Window.Current.Bounds.Width > 1000)
                MinWidth = Window.Current.Bounds.Width;
            else MinWidth = 1000;

            if (Window.Current.Bounds.Height > 700)
                MinHeight = Window.Current.Bounds.Height;
            else MinHeight = 700;

            if (MinWidth > 1500)
                StackPanelWidth = MinWidth / 2;
            else StackPanelWidth = 950;

        }
    }
}
