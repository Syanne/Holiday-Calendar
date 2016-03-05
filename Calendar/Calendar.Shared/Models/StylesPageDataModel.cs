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
        public string Text { get; set; }
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
            XDocument doc = XDocument.Load(CalendarResources.CalendarResourcesManager.resource.GetString("LocalThemesPath"));

            var collection = doc.Root.Descendants("theme");

            foreach (XElement holiday in collection)
            {
                _itemSource.Add(new LocalFVItem
                {
                    Image = String.Format("/SelectTheme/{0}-700.png", holiday.FirstAttribute.Value),
                    Text = holiday.LastAttribute.Value,
                    Tag = holiday.FirstAttribute.Value
                });
            }
        }
    }
}
