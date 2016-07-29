using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundUpdater
{
    public sealed class TileBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //load data
            DataManager.BgTaskHelper();

            //start updating
            try
            {
                UpdateTile();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static void UpdateTile()
        {
            var updateManager = TileUpdateManager.CreateTileUpdaterForApplication();
            updateManager.Clear();
            updateManager.EnableNotificationQueue(true);


            List<Event> dyn = LoadXml(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            if (dyn.Count < 5)
            {
                DateTime dt = DateTime.Now.AddMonths(1);
                List<Event> s = LoadXml(dt.Year, dt.Month, 1);
                for (int i = 0; i < s.Count; i++)
                    dyn.Add(s[i]);
            }

            int n = (dyn.Count > 5) ? 5 : dyn.Count();

            if (dyn.Count > 0)
                for (int i = 1; i <= n; i++)
                {
                    int year = (dyn.ElementAtOrDefault(i - 1).Month == DateTime.Now.Month)
                               ? DateTime.Now.Year : (DateTime.Now.Year + 1);
                    //date to show
                    DateTime date = new DateTime(year, dyn.ElementAtOrDefault(i - 1).Month, dyn.ElementAtOrDefault(i - 1).Day);


                    var xml = @"<tile>      
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

                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(String.Format(xml,
                        date.ToString("d"),
                        dyn.ElementAt(i - 1).Name));
                    var notification = new TileNotification(xmlDocument);
                    updateManager.Update(notification);
                }
        }

        private static List<Event> LoadXml(int y, int m, int d)
        {
            List<Event> collection = new List<Event>();

            //collections of holidays
            var persCollection = DataManager.PersonalData.Root.Descendants("theme").Descendants("holiday");
            var computational = DataManager.doc.Root.Descendants("month").ElementAt(m - 1).Descendants("computational");
            var movable = DataManager.doc.Root.Descendants("month").ElementAt(m - 1).Descendants("movable");
            var stated = DataManager.doc.Root.Descendants("month").ElementAt(m - 1).Descendants("day");

            if (computational.Count() != 0)
                foreach (XElement pers in persCollection)
                    foreach (XElement x in computational)
                    {
                        if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                            collection.Add(new Event
                            {
                                Day = ComputeHoliday(Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                                                    Convert.ToInt32(x.Attributes().ElementAt(2).Value), GetStart()),
                                Name = x.Attributes().ElementAt(0).Value,
                                Month = m
                            });
                    }

            //looking for holidays from selected categories
            foreach (XElement x in stated)
            {
                foreach (XElement pers in persCollection)
                    if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                        collection.Add(new Event
                        {
                            Day = Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                            Name = x.Attributes().ElementAt(0).Value,
                            Month = m
                        });
            }

            if (movable.Count() != 0)
                foreach (XElement x in movable)
                    foreach (XElement pers in persCollection)
                    {
                        if (x.Attribute("year").Value == y.ToString() && x.Parent.FirstAttribute.Value == pers.FirstAttribute.Value)
                        {
                            collection.Add(new Event
                            {
                                Day = Convert.ToInt32(x.Attribute("day").Value),
                                Name = x.Attribute("name").Value,
                                Month = m
                            });
                        }
                    }

            collection.AddRange(DataManager.PersonalAndServices(m, y));

            return collection.Where(el => el.Day >= d).OrderBy(el => el.Day).ToList();
        }
        

        /// <summary>
        /// computes holidays from tag COMPUTATIONAL
        /// </summary>
        /// <param name="dow">day of week (from 0 to 6)</param>
        /// <param name="now">number of week (from 0 to 4)</param>
        /// <param name="start">start position of this month in calenar (row*10+col)</param>
        /// <returns></returns>
        private static int ComputeHoliday(int dow, int now, int start)
        {
            return ((8 - (int)(start % 10)) + now * 7 - 6 + dow);
        }
        private static int GetStart()
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
    }

}
