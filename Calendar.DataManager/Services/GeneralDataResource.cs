using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Calendar.Data.Models;

namespace Calendar.Data.Services
{
    public class GeneralDataResource : BasicDataResource
    {
        /// <summary>
        /// File basic path
        /// </summary>
        protected override string Path
        {
            get { return "ms-appx:///Strings/Holidays.xml"; }
        }


        public GeneralDataResource()
        {
            Document = LoadFile(Path, null);
        }
        
        /// <summary>
        /// Prepare ollection of elements.
        /// Key - parent.
        /// Value - list of descendants.
        /// </summary>
        /// <param name="parentName">parent node name</param>
        /// <param name="extraKey">extra parameter</param>
        /// <returns>Collection of elements</returns>
        public override IEnumerable<XElement> GetItemList(string parentName, int? extraKey)
        {
            throw new NotImplementedException();
        }

        public List<XElement> GetCollectionFromSourceFile()
        {
            return Document.Root.Descendants("month").ElementAt(0).Descendants("holiday").ToList();
        }
        

        /// <summary>
        /// Get collection of elements from general (localized) data file
        /// </summary>
        /// <param name="SelectedDate">selected date</param>
        /// <param name="collectionType">type of collection (moveable, compytational, day)</param>
        /// <param name="categories">selected holiday types</param>
        /// <returns>collection of pairs category/XElement</returns>
        public List<DocElement> GetCollectionFromSourceFile(DateTime SelectedDate, string collectionType, Dictionary<string, string> categories)
        {
            var holidayCollection = new List<DocElement>();

            foreach (var element in Document.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants(collectionType))
            {
                //category
                string theme = element.Parent.Attribute("name").Value;
                bool value = (categories.Keys.Count(val => val == theme) == 1) ? true : false;

                if (element.FirstAttribute.Value != "" && value == true)
                    holidayCollection.Add(new DocElement
                    {
                        Key = theme,
                        Value = element
                    });
            }

            return holidayCollection;
        }

    }
}
