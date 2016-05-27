﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace BackgroundTasks
{
    public sealed class TileBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

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

            List<HelpStruct> dyn = LoadXml(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            
            if(dyn.Count < 5)
            {
                DateTime dt = DateTime.Now.AddMonths(1);
                List<HelpStruct> s = LoadXml(dt.Year, dt.Month, 1);
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
                    xmlDocument.LoadXml(String.Format(xml, date.ToString("d"), dyn.ElementAtOrDefault(i - 1).Name));
                    var notification = new TileNotification(xmlDocument);
                    updateManager.Update(notification);
                }
        }

        private static List<HelpStruct> LoadXml(int y, int m, int d)
        {
            XDocument PersonalData = new XDocument();
            List<HelpStruct> collection = new List<HelpStruct>();

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
                PersonalData = XDocument.Load(@"Holidays/" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName + "/PersData.xml");
            }          

            finally
            {
                //file for current selected month
                XDocument doc = XDocument.Load(String.Format(@"Holidays/{0}/Holidays.xml", CultureInfo.CurrentCulture.TwoLetterISOLanguageName));
                //collections of holidays
                var computational = doc.Root.Descendants("month").ElementAt(m - 1).Descendants("computational");
                var persCollection = PersonalData.Root.Descendants("theme").Descendants("holiday");
                var movable = doc.Root.Descendants("month").ElementAt(m - 1).Descendants("movable");

                //looking for holidays from selected categories
                foreach (XElement x in doc.Root.Descendants("month").ElementAt(m - 1).Descendants("day"))
                {
                    foreach (XElement pers in persCollection)
                        if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                            collection.Add(new HelpStruct
                            {
                                Day = Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                                Name = x.Attributes().ElementAt(0).Value,
                                Month = m
                            });
                }

                if (computational.Count() != 0)
                    foreach (XElement pers in persCollection)
                        foreach (XElement x in computational)
                        {
                            if (x.FirstAttribute.Value != "" && x.Parent.Attribute("name").Value == pers.Attribute("name").Value.ToLower())
                                collection.Add(new HelpStruct
                                {
                                    Day = ComputeHoliday(Convert.ToInt32(x.Attributes().ElementAt(1).Value),
                                                        Convert.ToInt32(x.Attributes().ElementAt(2).Value), GetStart()),
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
                                collection.Add(new HelpStruct
                                {
                                    Day = Convert.ToInt32(x.Attribute("day").Value),
                                    Name = x.Attribute("name").Value,
                                    Month = m
                                });
                            }
                        }

                foreach (XElement pers in PersonalData.Root.Descendants("holidays").Descendants("persDate"))
                    if (pers.Attribute("month").Value == m.ToString() && (pers.Attribute("year").Value == y.ToString() || pers.Attribute("year").Value == "0"))
                        collection.Add(new HelpStruct
                        {
                            Day = Convert.ToInt32(pers.Attribute("date").Value),
                            Name = pers.Attribute("name").Value,
                            Month = m
                        });
            }
            return collection.Where(el=>el.Day >= d).OrderBy(el => el.Day).ToList();
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
        // Although most HTTP servers do not require User-Agent header, others will reject the request or return 
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers. 

    }

     public sealed class ToastBackgroundTask : IBackgroundTask
     {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            try
            {
                SendToast();
            }
            finally
            {
                deferral.Complete();
            }
        }

        private static void SendToast()
        {           
            XDocument PersonalData = new XDocument();

            try
            {
                PersonalData = XDocument.Load(ApplicationData.Current.RoamingFolder.Path + @"\PersData.xml");
                
                List<XElement> collection = LoadXml(ref PersonalData);
                int n = (collection.Count() > 3) ? 3 : collection.Count();
                if (collection.Count() > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        // Using the ToastText02 toast template.
                        ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;

                        // Retrieve the content part of the toast so we can change the text.
                        XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                        //Find the text component of the content
                        XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");

                        // Set the text on the toast. 
                        // The first line of text in the ToastText02 template is treated as header text, and will be bold.
                        int year = (Convert.ToInt32(collection.ElementAt(i).Attribute("month").Value) == DateTime.Now.Month) 
                            ? DateTime.Now.Year : (DateTime.Now.Year + 1);

                        DateTime date = new DateTime(year, Convert.ToInt32(collection.ElementAt(i).Attribute("month").Value), Convert.ToInt32(collection.ElementAt(i).Attribute("date").Value));
                        toastTextElements[0].AppendChild(toastXml.CreateTextNode(date.ToString("d")));

                        toastTextElements[1].AppendChild(toastXml.CreateTextNode(collection.ElementAt(i).Attribute("name").Value));

                        // Set the duration on the toast
                        IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                        ((XmlElement)toastNode).SetAttribute("duration", "long");

                        // Create the actual toast object using this toast specification.
                        ToastNotification toast = new ToastNotification(toastXml);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                    }
                }
            }
            catch
            {
                PersonalData = null;
            }            
        }

        private static List<XElement> LoadXml(ref XDocument PersonalData)
        {
            int day = Convert.ToInt32(PersonalData.Root.Attribute("toast").Value);
            List<XElement> personal;
            if ((DateTime.Now.Day + day) <= DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
            {
                personal = PersonalData.Root.Descendants("holidays").Descendants("persDate").
                    Where(pers => (pers.Attribute("year").Value == DateTime.Now.Year.ToString() || pers.Attribute("year").Value == "0")
                        && pers.Attribute("month").Value == DateTime.Now.Month.ToString()
                        && (Convert.ToInt32(pers.Attribute("date").Value) >= DateTime.Now.Day &&
                        Convert.ToInt32(pers.Attribute("date").Value) == (DateTime.Now.Day + day))).
                        OrderBy(pers => pers.Attribute("date").Value).
                        ToList();
            }
            else
            {
                int y = (DateTime.Now.Month == 12) ? DateTime.Now.AddYears(1).Year : DateTime.Now.Year;
                personal = PersonalData.Root.Descendants("holidays").Descendants("persDate").
                Where(pers => (Convert.ToInt32(pers.Attribute("year").Value) == y || pers.Attribute("year").Value == "0")
                    && pers.Attribute("month").Value == DateTime.Now.AddMonths(1).Month.ToString()
                    && (Convert.ToInt32(pers.Attribute("date").Value) < DateTime.Now.AddDays(day).Day)).
                    OrderBy(pers => pers.Attribute("date").Value).ToList();
            }
            return personal;
        }

     }

    struct HelpStruct
    {
        public string Name;
        public int Day;
        public int Month;
    }
}
