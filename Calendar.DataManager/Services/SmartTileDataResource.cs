using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Calendar.Data.Models;
using Windows.Storage;
using System.Xml;

namespace Calendar.Data.Services
{
    public class SmartTileDataResource : BasicDataResource
    {
        protected override string Path
        {
            get { return "SmartTileFile.xml"; }
        }
        
        public SmartTileDataResource()
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            Document = base.LoadFile(Path, storageFolder);
        }

        public override IEnumerable<XElement> GetItemList(string parentName, int? extraKey)
        {
            throw new NotImplementedException();
        }

        public bool ProcessSmartTileFile(string snooze)
        {
            //try load
            try
            {
                Document.Root.Attribute("refreshmentDate").SetValue(DateTime.Now.Date.AddDays(-1).ToString(DataManager.DateFormat));
                Document.Root.Attribute("daysAmount").SetValue(snooze);
                
                //save changes
                SaveDocument();

                return true;
            }
            //elseway - try create
            catch
            {
                try
                {
                    //create file
                    Document = new XDocument();
                    Document = new XDocument(new XElement("root", Document.Root));
                    Document.Root.Value = "";

                    using (XmlWriter writer = Document.Root.CreateWriter())
                    {
                        writer.WriteStartAttribute("firstElement");
                        writer.WriteString("0");
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("refreshmentDate");
                        writer.WriteString(DateTime.Now.Date.AddDays(-1).ToString(DataManager.DateFormat));
                        writer.WriteEndAttribute();
                        writer.WriteStartAttribute("daysAmount");
                        writer.WriteString(snooze);
                        writer.WriteEndAttribute();
                    }

                    //save changes
                    SaveDocument();

                    return true;
                }
                catch
                {
                    Document = null;
                    return false;
                }
            }
        }

        protected async override void SaveDocument()
        {
            if (Document != null)
                try
                {
                    StorageFile sampleFile = await ApplicationData.Current.LocalFolder.
                         CreateFileAsync("SmartTileFile.xml", CreationCollisionOption.OpenIfExists);
                    await FileIO.WriteTextAsync(sampleFile, Document.ToString());
                }
                catch { }
        }

        #region LiveTileActivity
        public List<HolidayItem> LoadSmartTileFile()
        {
            DateTime refreshmentDate, endDate;
            int firstElement, lastElement, daysAmount;
            List<HolidayItem> events = new List<HolidayItem>();

            //--------------
            //set variables
            var array = Document.Root.Attribute("refreshmentDate").Value.Split(DataManager.DateSeparator);
            refreshmentDate = new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
            daysAmount = int.Parse(Document.Root.Attribute("daysAmount").Value);


            //load files
            events = DataManager.GetComposedData(refreshmentDate, 1);

            bool toLoad = (refreshmentDate.Day == DateTime.Now.Day && refreshmentDate.Month == DateTime.Now.Month && refreshmentDate.Year == DateTime.Now.Year)
                            ? false : true;

            //-------------------------
            //set collection, if it isn't in file
            if (toLoad)
            {
                //set variables
                refreshmentDate = DateTime.Now;
                endDate = refreshmentDate.AddDays(daysAmount);
                firstElement = 0;

                Document.Root.Descendants().Remove();

                if (endDate.Month == refreshmentDate.Month)
                {
                    events = events.Where(x => x.Day >= refreshmentDate.Day && x.Day < endDate.Day).ToList();
                }
                else
                {
                    //from first month
                    events = events.Where(x => x.Day >= refreshmentDate.Day).ToList();

                    //from second month
                    var temp = DataManager.GetComposedData(endDate, 1);
                    events.AddRange(temp.Where(x => x.Day < endDate.Day).ToList());                    
                }

                //Sort
                events = events.OrderBy(x => x.Year).OrderBy(x => x.Month).OrderBy(x => x.Day).ToList();

                foreach (var ev in events)
                {
                    //complite file
                    using (XmlWriter writer = Document.Root.CreateWriter())
                    {
                        writer.WriteStartElement("event");

                        writer.WriteStartAttribute("value");
                        writer.WriteString(ev.HolidayName);
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
                firstElement = int.Parse(Document.Root.Attribute("firstElement").Value);
                endDate = refreshmentDate.AddDays(daysAmount);

                //read events from file
                foreach (var element in Document.Root.Elements())
                {
                    var listItem = new HolidayItem()
                    {
                        HolidayName = element.Attribute("value").Value,
                        Day = int.Parse(element.Attribute("day").Value),
                        Month = int.Parse(element.Attribute("month").Value),
                        Year = int.Parse(element.Attribute("year").Value)
                    };

                    events.Add(listItem);
                }

            }

            lastElement = firstElement + 4;


            if (events.Count > 5 && lastElement >= events.Count)
            {
                int difference = lastElement - firstElement;
                int toSkip = firstElement - difference;
                var subcoll = events.Skip(firstElement - 1).ToList();
                events = events.Take(difference).ToList();
                events.AddRange(subcoll);

                firstElement = difference;
            }
            else if (lastElement < events.Count)
            {
                events = events.Skip(firstElement).Take(5).ToList();

                firstElement = lastElement;
            }
            else firstElement = 0;


            //set attributes
            Document.Root.Attribute("firstElement").SetValue(firstElement.ToString());
            Document.Root.Attribute("refreshmentDate").SetValue(refreshmentDate.Date.ToString(DataManager.DateFormat));
            SaveDocument();

            return events;
        }

        #endregion
    }
}
