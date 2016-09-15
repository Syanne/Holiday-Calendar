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
        protected override string Path
        {
            get { return "ms-appx:///Strings/Holidays.xml"; }
        }

        public Dictionary<string, string> SelectedCategories { get; private set; }

        public GeneralDataResource()
        {
            Document = LoadFile(Path, null);
            SelectedCategories = new Dictionary<string, string>();
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


        public void SetSelectedCategoriesList(List<XElement> collection)
        {
            SelectedCategories = new Dictionary<string, string>();

            //get themes (categories)
            var keys = collection.Select(x => x.Attribute("name").Value).ToList();
            var values = collection.Select(x => x.Attribute("desc").Value).ToList();

            //set dictionary
            for (int i = 0; i < keys.Count; i++)
                SelectedCategories.Add(keys[i], values[i]);
        }

        public List<DocElement> GetCollectionFromSourceFile(DateTime SelectedDate, string collectionType)
        {
            var holidayCollection = new List<DocElement>();

            foreach (var element in Document.Root.Descendants("month").ElementAt(SelectedDate.Month - 1).Descendants(collectionType))
            {
                //category
                string theme = element.Parent.Attribute("name").Value;
                bool value = (SelectedCategories.Keys.Count(val => val == theme) == 1) ? true : false;

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
