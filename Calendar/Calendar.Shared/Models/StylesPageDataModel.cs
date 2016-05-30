using System;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Calendar
{
    /// <summary>
    /// Item in FlipView for styles 
    /// </summary>
    public class LocalFVItem
    {
        public string Image { get; set; }
        public string Tag { get; set; }
    }

    /// <summary>
    /// Days in calendar
    /// </summary>
    public sealed class SampleDataSource
    {
        private ObservableCollection<LocalFVItem> _itemSource = new ObservableCollection<LocalFVItem>();
        public ObservableCollection<LocalFVItem> Items
        {
            get { return this._itemSource; }
        }

        public SampleDataSource()
        {
            XDocument doc = XDocument.Load(CalendarResources.DataManager.resource.GetString("LocalThemesPath"));
            string pictureFolder = CalendarResources.DataManager.resource.GetString("themePictures");

            var collection = doc.Root.Descendants("theme");

            foreach (XElement holiday in collection)
            {
                _itemSource.Add(new LocalFVItem
                {
#if WINDOWS_PHONE_APP
                    Image = String.Format("{0}/{1}.png", pictureFolder, holiday.FirstAttribute.Value),
#else
                    Image = String.Format("{0}/{1}-700.png", pictureFolder, holiday.FirstAttribute.Value),
#endif
                    Tag = holiday.FirstAttribute.Value
                });
            }

        }
    }
}
