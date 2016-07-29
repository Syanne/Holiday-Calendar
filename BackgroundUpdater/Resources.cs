using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Windows.Storage;

namespace BackgroundUpdater
{
    class DataManager
    {
        public static XDocument PersonalData { get; set; }
        public static XDocument doc { get; set; }
        public static List<string> Services { get; set; }
        /// <summary>
        /// unified separator for date split
        /// </summary>
        public const char DateSeparator = '-';


        public static void BgTaskHelper()
        {
            Uri uri = new Uri("ms-appx:///Strings/Holidays.xml");
            var holFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var read = FileIO.ReadTextAsync(holFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            doc = XDocument.Parse(read);

            LoadPersonalData();
        }


        /// <summary>
        /// Load Personal Data
        /// </summary>
        public static void LoadPersonalData()
        {
            try
            {
                var storageFolder = ApplicationData.Current.RoamingFolder;
                var file = storageFolder.GetFileAsync("PersData.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                PersonalData = XDocument.Parse(text);
            }
            //if it's the fist launch - load basic file
            catch
            {
                var persUri = new Uri("ms-appx:///Strings/PersData.xml");
                var persFile = StorageFile.GetFileFromApplicationUriAsync(persUri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                var persRead = FileIO.ReadTextAsync(persFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                PersonalData = XDocument.Parse(persRead);
            }

            //services
            SetServices();
        }

        /// <summary>
        /// Find enabled services
        /// </summary>
        private static void SetServices()
        {
            var subcollection = PersonalData.Root.Descendants().Where(x => x.Name.LocalName != "holiday" &&
                                                                               x.Name.LocalName != "holidays" &&
                                                                               x.Name.LocalName != "theme" &&
                                                                               x.Name.LocalName != "persDate");
            if (subcollection.Count() > 0)
            {
                Services = new List<string>();
                foreach (var item in subcollection)
                {
                    if (item.Attribute("isActive").Value == "true")
                        Services.Add(item.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// Get collecton of personal events and services
        /// </summary>
        /// <param name="m">month</param>
        /// <param name="y">year</param>
        /// <returns>collection of events</returns>
        public static List<Event> PersonalAndServices(int m, int y)
        {
            var collection = new List<Event>();
            try
            {
                //personal
                var persDates = PersonalData.Root.Descendants("holidays").Descendants("persDate").ToList();

                if(persDates.Count > 1)
                foreach (XElement pers in persDates)
                    if (pers.Attribute("month").Value == m.ToString() && (pers.Attribute("year").Value == y.ToString() || pers.Attribute("year").Value == "0"))
                        collection.Add(new Event
                        {
                            Day = Convert.ToInt32(pers.Attribute("date").Value),
                            Name = pers.Attribute("name").Value,
                            Month = m
                        });

                //services
                if (Services != null)
                {
                    //check sync date
                    if (Services != null)
                        if (Services.Contains("google"))
                        {
                            var period = int.Parse(PersonalData.Root.Element("google").Attribute("period").Value);
                            var array = PersonalData.Root.Element("google").Attribute("nextSyncDate").Value.Split(DateSeparator);
                            var date = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));

                            if (date.Day <= DateTime.Now.Day && date.Month <= DateTime.Now.Month && date.Year <= DateTime.Now.Year)
                                collection.Add(new Event
                                {
                                    Day = DateTime.Now.Day,
                                    Month = DateTime.Now.Month,
                                    Name = "google calendar",
                                });
                        }
                    foreach (var service in DataManager.Services)
                    {
                        var servCollection = DataManager.PersonalData.Root.Element(service).Descendants();
                        foreach (var holiday in servCollection)
                        {
                            int year = int.Parse(holiday.Attribute("year").Value);
                            int month = int.Parse(holiday.Attribute("month").Value);
                            int day = int.Parse(holiday.Attribute("date").Value);

                            if (year == y && month == m)
                                collection.Add(new Event
                                {
                                    Day = day,
                                    Month = month,
                                    Name = holiday.Attribute("name").Value,
                                });
                        }
                    }
                }
            }
            catch
            {

            }
            return collection;
        }
    }

    struct Event
    {
        public string Name;
        public int Day;
        public int Month;
    }
}
