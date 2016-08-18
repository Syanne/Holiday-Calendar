using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;

namespace BackgroundUpdater
{
    class DataBakgroundManager
    {
        public const string XML_TEMPLATE = @"<tile>      
                            <visual>       
                            <binding template=""TileSquareText02"">        
                            <text id=""1"">{0}</text>       
                            <text id=""2"">{1}</text>     
                            </binding> 
                            <binding template=""TileWideText09"">       
                            <text id=""1"">{00}</text>       
                            <text id=""2"">{1}</text>      
                            </binding> 
                            </visual>  
                            </tile>";
        private XDocument SmartTileFile { get; set; }
        private XDocument PersonalData { get; set; }
        private XDocument doc { get; set; }
        public List<string> Services { get; private set; }

        /// <summary>
        /// unified date format for all descentants
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";
        /// <summary>
        /// unified separator for date split
        /// </summary>
        public const char DateSeparator = '-';

        public List<Event> LoadSmartTileFile()
        {
            DateTime refreshmentDate, endDate;
            int firstElement, lastElement, daysAmount;
            List<Event> events = new List<Event>();

            //load files
            var storageFolder = ApplicationData.Current.LocalFolder;
            var file = storageFolder.GetFileAsync("SmartTileFile.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            SmartTileFile = XDocument.Parse(text);

            LoadResourceHolidays();

            //--------------
            //set variables
            var array = SmartTileFile.Root.Attribute("refreshmentDate").Value.Split(DateSeparator);
            refreshmentDate = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
            daysAmount = int.Parse(SmartTileFile.Root.Attribute("daysAmount").Value);
            
            //-------------------------
            //set collection, if it isn't in file
            if(refreshmentDate.CompareTo(DateTime.Now) != 0)
            {
                //set variables
                refreshmentDate = DateTime.Now;
                endDate = refreshmentDate.AddDays(daysAmount);
                firstElement = 0;

                SmartTileFile.Root.RemoveAll();

                //take all events in range
                for(int i = 0; i < daysAmount; i++)
                {
                    var currentDate = refreshmentDate.AddDays(i);
                    var list = this.LoadXml(currentDate.Year, currentDate.Month, currentDate.Day);
                    events.AddRange(list);
                }

                //take personal
                var persList = this.PersonalAndServices(refreshmentDate.Month, refreshmentDate.Year);

                if (endDate.Month == refreshmentDate.Month)
                    persList = persList.Where(x => x.Day >= refreshmentDate.Day && x.Day < endDate.Day).ToList();
                else
                {
                    //from first month
                    persList = persList.Where(x => x.Day >= refreshmentDate.Day).ToList();

                    //from second month
                    var nextList = this.PersonalAndServices(endDate.Month, endDate.Year);
                    persList.AddRange(nextList.Where(x => x.Day < endDate.Day).ToList());
                }

                //Sort
                this.SortCollection(ref events);

                //complite file
                using (XmlWriter writer = SmartTileFile.Root.CreateWriter())
                {
                    foreach (var ev in events)
                    {
                        writer.WriteStartElement("event");

                        writer.WriteStartAttribute("value");
                        writer.WriteString(ev.Value);
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("day");
                        writer.WriteString(ev.Day.ToString());
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("month");
                        writer.WriteString(ev.Month.ToString());
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("year");
                        writer.WriteString(ev.Year.ToString());
                        writer.WriteEndAttribute();
                    }
                }
            }
            //or read collection from file
            else
            {
                //set variables
                firstElement = int.Parse(SmartTileFile.Root.Attribute("firstElement").Value);
                endDate = refreshmentDate.AddDays(daysAmount);

                //read events from file
                foreach (var element in SmartTileFile.Root.Elements())
                {
                    var listItem = new Event()
                    {
                        Value = element.Attribute("value").Value,
                        Day = int.Parse(element.Attribute("day").Value),
                        Month = int.Parse(element.Attribute("month").Value),
                        Year = int.Parse(element.Attribute("year").Value)
                    };

                    events.Add(listItem);
                }

            }

            lastElement = firstElement + 4;


            if(events.Count > 5 && lastElement >= events.Count)
            {
                int difference = lastElement - firstElement;
                int toSkip = firstElement - difference;
                var subcoll = events.Skip(firstElement - 1).ToList();
                events = events.Take(difference).ToList();
                events.AddRange(subcoll);

                firstElement = difference;
            }
            else if(lastElement <= events.Count)
            {
                events = events.Skip(firstElement).Take(5).ToList();

                firstElement = lastElement;
            }


            //set attributes
            SmartTileFile.Root.Attribute("firstElement").SetValue(firstElement.ToString());
            SmartTileFile.Root.Attribute("refreshmentDate").SetValue(refreshmentDate.Date.ToString(DateFormat));
            SaveSmartTileFile(SmartTileFile);

            return events;
        }


        /// <summary>
        /// Saves changed personal data
        /// </summary>
        public async void SaveSmartTileFile(XDocument document)
        {
            try
            {
                StorageFile sampleFile = await ApplicationData.Current.LocalFolder.
                     CreateFileAsync("SmartTileFile.xml", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(sampleFile, document.ToString());
            }
            catch { }
        }

        public void LoadResourceHolidays()
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
        public void LoadPersonalData()
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
        private void SetServices()
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

        public List<Event> LoadXml(int _year, int _month, int _day)
        {
            List<Event> collection = new List<Event>();

            //collections of holidays
            var persCollection = PersonalData.Root.Descendants("theme").Descendants("holiday");
            var computational = doc.Root.Descendants("month").ElementAt(_month - 1).Descendants("computational");
            var movable = doc.Root.Descendants("month").ElementAt(_month - 1).Descendants("movable");
            var stated = doc.Root.Descendants("month").ElementAt(_month - 1).Descendants("day");

            if (computational.Count() != 0)
                foreach (XElement pers in persCollection)
                    foreach (XElement x in computational)
                    {
                        if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                            collection.Add(new Event
                            {
                                Day = ComputeHoliday(Convert.ToInt32(x.Attribute("date").Value),
                                                    Convert.ToInt32(x.Attributes().ElementAt(2).Value), GetStart()),
                                Value = x.Attributes().ElementAt(0).Value,
                                Month = _month,
                                Year = _year
                            });
                    }

            //looking for holidays from selected categories
            foreach (XElement x in stated)
            {
                foreach (XElement pers in persCollection)
                    if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                        collection.Add(new Event
                        {
                            Day = Convert.ToInt32(x.Attribute("date").Value),
                            Value = x.Attributes().ElementAt(0).Value,
                            Month = _month,
                            Year = _year
                        });
            }

            if (movable.Count() != 0)
                foreach (XElement x in movable)
                    foreach (XElement pers in persCollection)
                    {
                        if (x.Attribute("year").Value == _year.ToString() && x.Parent.FirstAttribute.Value == pers.FirstAttribute.Value)
                        {
                            collection.Add(new Event
                            {
                                Day = Convert.ToInt32(x.Attribute("date").Value),
                                Value = x.Attribute("name").Value,
                                Month = _month,
                                Year = _year
                            });
                        }
                    }


            return collection.Where(el => el.Day >= _day).OrderBy(el => el.Day).ToList();
        }


        /// <summary>
        /// computes holidays from tag COMPUTATIONAL
        /// </summary>
        /// <param name="dow">day of week (from 0 to 6)</param>
        /// <param name="now">number of week (from 0 to 4)</param>
        /// <param name="start">start position of this month in calenar (row*10+col)</param>
        /// <returns></returns>
        private int ComputeHoliday(int dow, int now, int start)
        {
            return ((8 - (int)(start % 10)) + now * 7 - 6 + dow);
        }

        private int GetStart()
        {
            //start from the firsth of {selected month}
            int a, y, m, R;
            a = (14 - DateTime.Now.Month) / 12;
            y = DateTime.Now.Year - a;
            m = DateTime.Now.Month + 12 * a - 2;
            R = 7000 + (1 + y + y / 4 - y / 100 + y / 400 + (31 * m) / 12);
            R %= 7;

            R = (R > 0) ? R : 7;

            return (R == 1) ? (10 + R) : R;
        }

        /// <summary>
        /// Get collecton of personal events and services
        /// </summary>
        /// <param name="_month">month</param>
        /// <param name="_year">year</param>
        /// <returns>collection of events</returns>
        public List<Event> PersonalAndServices(int _month, int _year)
        {
            var collection = new List<Event>();
            try
            {
                //personal
                var persDates = PersonalData.Root.Descendants("holidays").Descendants("persDate").ToList();

                if (persDates.Count > 1)
                    foreach (XElement pers in persDates)
                        if (pers.Attribute("month").Value == _month.ToString() && (pers.Attribute("year").Value == _year.ToString() || pers.Attribute("year").Value == "0"))
                            collection.Add(new Event
                            {
                                Day = Convert.ToInt32(pers.Attribute("date").Value),
                                Value = pers.Attribute("name").Value,
                                Month = _month,
                                Year = _year
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
                                    Value = "google calendar",
                                    Year = _year
                                });
                        }
                    foreach (var service in Services)
                    {
                        var servCollection = PersonalData.Root.Element(service).Descendants();
                        foreach (var holiday in servCollection)
                        {
                            int year = int.Parse(holiday.Attribute("year").Value);
                            int month = int.Parse(holiday.Attribute("month").Value);
                            int day = int.Parse(holiday.Attribute("date").Value);

                            if (year == _year && month == _month)
                                collection.Add(new Event
                                {
                                    Day = day,
                                    Month = month,
                                    Value = holiday.Attribute("name").Value,
                                    Year = _year
                                });
                        }
                    }
                }
            }
            catch
            {
                collection = new List<Event>();
            }

            return collection;
        }

        public int GetToastSnooze()
        {
            return Convert.ToInt32(PersonalData.Root.Attribute("toast").Value);
        }

        public void SortCollection(ref List<Event> collection)
        {

        }
    }

    public sealed class Event
    {
        public string Value { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
