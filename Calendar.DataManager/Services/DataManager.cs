using Calendar.Data.Models;
using Calendar.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;

namespace Calendar.Data.Services
{
    public partial class DataManager
    {
        /// <summary>
        /// unified date format for all descentants
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// unified separator for date split
        /// </summary>
        public const char DateSeparator = '-';
        
        /// <summary>
        /// a position of a first day of a month on the calendar grid
        /// </summary>
        public static int Start { get; private set; }
        /// <summary>
        /// a position of a last day of a month on the calendar grid
        /// </summary>
        public static int End { get; private set; }

        /// <summary>
        /// 5 - weeek starts from Mon, 0 - week starts from Sun
        /// </summary>
        public static int Weekend { get; private set; } = -1;

        /// <summary>
        /// Application Resources (localized strings)
        /// </summary>
        protected static ResourceLoader Resource { get; set; } = null;

        /// <summary>
        /// Categories collection
        /// </summary>
        protected static Dictionary<string, string> SelectedCategories { get; set; }

        protected static GeneralDataResource genDataResource { get; set; }
        protected static PersonalDataResource persDataResource { get; set; }

        /// <summary>
        /// Set application resources
        /// </summary>
        /// <param name="value"></param>
        public static void SetResource(ResourceLoader value)
        {
            Resource = value;
        }

        /// <summary>
        /// Reset month data
        /// </summary>
        /// <param name="SelectedDate">current month and year</param>
        /// <param name="callerID">0 - application, 1 - tile/smartTile, 2 - toast</param>
        public static void ResetMonth(DateTime SelectedDate, int callerID)
        {
            if (Weekend == -1)
            {
                try
                {
                    string theDay = Resource.GetString("Weekend");
                    Weekend = Convert.ToInt32(theDay);
                }
                catch
                {
                    Weekend = 5;
                }
            }

            int weekDay = FirstDay(SelectedDate, callerID);
            int days = DateTime.DaysInMonth(SelectedDate.Year, SelectedDate.Month);

            //start position for this month
            int ii = (weekDay == 0) ? 1 : 0;
            Start = (ii * 7) + weekDay;
            End = days + Start;
        }

        /// <summary>
        /// Get string from resource file
        /// </summary>
        /// <param name="resourceStringName">key (uid)</param>
        /// <returns>localized string</returns>
        public static string GetStringFromResourceLoader(string resourceStringName)
        {
            return Resource.GetString(resourceStringName);
        }

        /// <summary>
        /// Load application XML files (personal and localized)
        /// </summary>
        /// <param name="loadDoc">load files</param>
        protected static void LoadApplicationDataFiles(bool loadDoc)
        {
            //personal
            persDataResource = new PersonalDataResource();

            //basic collection
            if (loadDoc)
            {
                //set collection of selected categories of holidays
                var collection = GetCollectionFromSourceFile("theme");
                genDataResource = new GeneralDataResource();

                SelectedCategories = new Dictionary<string, string>();

                //set dictionary
                foreach (var item in collection)
                    SelectedCategories.Add(item.Attribute("name").Value, item.Attribute("desc").Value);
            }
        }

        #region Collection generator
        /// <summary>
        /// Returns collection of holidays
        /// </summary>
        /// <param name="SelectedDate">current date</param>
        /// <param name="callerID">0 - application, 1 - tile/smartTile, 2 - toast</param>
        /// <returns></returns>
        public static List<HolidayItem> GetComposedData(DateTime SelectedDate, int callerID)
        {
            //collections of holidays   
            var fullCollection = new List<HolidayItem>();
            List<DocElement> tempCollection = new List<DocElement>();

            if (callerID != 2)
            {
                //looking for holidays from selected categories
                tempCollection = GetCollectionFromSourceFile(SelectedDate, "day");
                foreach (var x in tempCollection)
                {
                    fullCollection.Add(new HolidayItem
                    {
                        Day = Convert.ToInt32(x.Value.Attribute("date").Value),
                        Month = SelectedDate.Month,
                        Year = SelectedDate.Year,
                        HolidayName = x.Value.Attribute("name").Value,
                        HolidayTag = x.Key
                    });
                }

                //computationals
                tempCollection = GetCollectionFromSourceFile(SelectedDate, "computational");
                foreach (var x in tempCollection)
                {
                    fullCollection.Add(new HolidayItem
                    {
                        Day = ComputeHoliday(Convert.ToInt32(x.Value.Attributes().ElementAt(1).Value), Convert.ToInt32(x.Value.Attributes().ElementAt(2).Value), callerID),
                        Month = SelectedDate.Month,
                        Year = SelectedDate.Year,
                        HolidayName = x.Value.Attributes().ElementAt(0).Value,
                        HolidayTag = x.Key
                    });
                }

                //movables
                tempCollection = GetCollectionFromSourceFile(SelectedDate, "movable");
                if (tempCollection != null)
                    foreach (var x in tempCollection)
                    {
                        //try parse year
                        int mYear = 0;
                        bool sth = Int32.TryParse(x.Value.Attribute("year").Value, out mYear);

                        if (mYear != 0 && mYear == SelectedDate.Year)
                            fullCollection.Add(new HolidayItem
                            {
                                Day = Convert.ToInt32(x.Value.Attribute("date").Value),
                                Month = SelectedDate.Month,
                                Year = SelectedDate.Year,
                                HolidayName = x.Value.Attribute("name").Value,
                                HolidayTag = x.Key
                            });
                    }
            }

            //personal
            string mine = "";
            if (callerID == 0)
                mine = Resource.GetString("MineAsTag");

            var list = GetCollectionFromSourceFile("persDate");
            foreach (var pers in list)
            {
                //get year
                int year = -1;
                if (!int.TryParse(pers.Attribute("year").Value, out year))
                    year = -1;

                //check month
                bool isCurrenMonth = (pers.Attribute("month").Value == SelectedDate.Month.ToString() ||
                                      pers.Attribute("month").Value == "0") ?
                                      true : false;

                if (isCurrenMonth && year != -1)
                    fullCollection.Add(new HolidayItem
                    {
                        Day = Convert.ToInt32(pers.Attribute("date").Value),
                        Month = SelectedDate.Month,
                        Year = SelectedDate.Year,
                        HolidayName = pers.Attribute("name").Value,
                        HolidayTag = mine
                    });
            }

            //services
            var collection = persDataResource.GetItemList(null, -1);

            if (collection.Count() != 0)
                foreach (var holiday in collection.Where(x => x.Name.LocalName == "persDate"))
                {
                    int year = int.Parse(holiday.Attribute("year").Value);
                    int month = int.Parse(holiday.Attribute("month").Value);

                    if (year == SelectedDate.Year && month == SelectedDate.Month)
                        fullCollection.Add(new HolidayItem
                        {
                            Day = int.Parse(holiday.Attribute("date").Value),
                            Month = SelectedDate.Month,
                            Year = SelectedDate.Year,
                            HolidayName = holiday.Attribute("name").Value,
                            HolidayTag = mine
                        });
                }

            //empty item
            if (callerID == 0)
                fullCollection.Add(new HolidayItem
                {
                    Day = 0,
                    Month = SelectedDate.Month,
                    Year = SelectedDate.Year,
                    HolidayName = Resource.GetString("PersonalNote"),
                    HolidayTag = Resource.GetString("MineAsTag")
                });

            return fullCollection;
        }

        /// <summary>
        /// computes holidays from tag COMPUTATIONAL
        /// </summary>
        /// <param name="dow">day of week (from 0 to 6)</param>
        /// <param name="now">number of week (from 0 to 4 (or 6 if last week))</param>
        /// <param name="callerID">0 - application, 1 - tile/smartTile, 2 - toast</param>
        /// <returns></returns>
        private static int ComputeHoliday(int dow, int now, int callerID)
        {
            if (callerID != 0)
            {
                int a;
                if (now != 6)
                {
                    int startDay = ((Start % 10) == 7) ? 0 : (int)(Start % 10);
                    a = (int)(Start / 10) * 7 + now * 7 + dow + 1 - startDay;

                    //if week starts from Sun, add 7 days
                    if (Weekend == 0) return a + 7;
                    return a;
                }
                //last week
                else
                {
                    a = (int)(End / 7) * 7 - Start - 6;
                    if (End % 7 > dow)
                        a += 7 + dow;
                    else a += dow;
                    return a;
                }
            }
            else
            {
                return ((8 - (int)(Start % 10)) + now * 7 - 6 + dow);
            }
        }

        /// <summary>
        /// Find a day, that starts selected month
        /// </summary>
        /// <param name="SelectedDate">selected month</param>
        /// <param name="callerID">0 - application, 1 - tile/smartTile, 2 - toast</param>
        /// <returns>1th of {selected month} weekday</returns>
        private static int FirstDay(DateTime SelectedDate, int callerID)
        {
            //start from the first of {selected month}
            int day = 1;
            int a, y, m, R;
            a = (14 - SelectedDate.Month) / 12;
            y = SelectedDate.Year - a;
            m = SelectedDate.Month + 12 * a - 2;
            R = 7000 + (day + y + y / 4 - y / 100 + y / 400 + (31 * m) / 12);
            R %= 7;

            if (callerID != 0)
            {
                if (Weekend == 0) return R;
                else return R = (R > 0) ? (R - 1) : 6;
            }
            else
            {
                R = (R > 0) ? R : 7;
                return (R == 1) ? (10 + R) : R;
            }
        }

        #endregion

        #region personal data
        /// <summary>
        /// Set snooze of toast notification
        /// </summary>
        /// <param name="value">snooze</param>
        public static void SetToastSnoozeValue(int value)
        {
            persDataResource.SetToastSnoozeValue(value);
        }

        /// <summary>
        /// Get snooze value
        /// </summary>
        /// <returns>snooze</returns>
        public static int GetToastSnoozeValue()
        {
            return persDataResource.GetToastSnoozeValue();
        }

        /// <summary>
        /// Write theme node
        /// </summary>
        /// <param name="writer">XmlWriter instance</param>
        /// <param name="name">text</param>
        /// <param name="tag">tag (key)</param>
        public static void AddHolidayCategory(XmlWriter writer, string name, string tag)
        {
            persDataResource.WriteNode(writer, name, tag);
        }

        /// <summary>
        /// Save new note
        /// </summary>
        /// <param name="text">note's text</param>
        /// <param name="day">day value</param>
        /// <param name="month">month value</param>
        /// <param name="year">year value</param>
        public static void CreateRecord(string text, int day, int month, int year)
        {
            persDataResource.CreateRecord(text, day, month, year, true, "holidays");
        }

        /// <summary>
        /// Remove note from file 
        /// </summary>
        /// <param name="name">Note text</param>
        /// <param name="day">day</param>
        /// <param name="month">month</param>
        /// <param name="year">year</param>
        public static void RemoveRecord(string name, string day, string month, string year)
        {
            persDataResource.RemoveRecord(name, day, month, year);
        }
        /// <summary>
        /// Change note
        /// </summary>
        /// <param name="oldValue">old text</param>
        /// <param name="newValue">changed text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        public static void ChangeRecord(string oldValue, string newValue, string day, string month, string year)
        {
            persDataResource.ChangeRecord(oldValue, newValue, day, month, year);
        }

        #endregion

        #region general data
        /// <summary>
        /// Get collection of elements from general (localized) data file
        /// </summary>
        /// <param name="SelectedDate">selected date</param>
        /// <param name="collectionType">type of collection (moveable, compytational, day)</param>
        /// <returns>collection of pairs category/XElement</returns>
        public static List<DocElement> GetCollectionFromSourceFile(DateTime SelectedDate, string collectionType)
        {
            return genDataResource.GetCollectionFromSourceFile(SelectedDate, collectionType, SelectedCategories);
        }

        /// <summary>
        /// Get categories collection
        /// </summary>
        /// <returns>pairs name(key)/description</returns>
        public static Dictionary<string, string> GetSelectedCategoriesList()
        {
            return SelectedCategories;
        }
        #endregion

        #region smart tile data
        /// <summary>
        /// Create/change data in SmartTileFile.xml
        /// </summary>
        /// <param name="snooze">snooze</param>
        /// <returns>done or not</returns>
        public static bool ProcessSmartTileFile(string snooze)
        {
            var smartTile = new SmartTileDataResource();
            return smartTile.ProcessSmartTileFile(snooze);
        }

        /// <summary>
        /// Get collection of notes for SmartTile
        /// </summary>
        /// <returns>collection of parsed notes from file</returns>
        public static List<HolidayItem> GetSmartTileCollection()
        {
            var smartTile = new SmartTileDataResource();
            return smartTile.LoadSmartTileFile();
        }

        #endregion

        /// <summary>
        /// Get collection of XElements from resource files
        /// </summary>
        /// <param name="collectionType"></param>
        /// <returns></returns>
        protected static List<XElement> GetCollectionFromSourceFile(string collectionType)
        {
            List<XElement> holidayCollection = new List<XElement>();
            
            holidayCollection = persDataResource.GetCollectionFromSourceFile(collectionType);

            return holidayCollection;
        }

        /// <summary>
        /// Get collection of holiday's categories (Key - tag, Value - description)
        /// </summary>
        /// <returns>Collection of categories</returns>
        public static Dictionary<string, string> GetCollectionOfCategories()
        {
            List<XElement> listOfElements = new List<XElement>();
            listOfElements = genDataResource.GetCollectionFromSourceFile();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            if (listOfElements.Count > 0)
                foreach (var element in listOfElements)
                    dict.Add(element.LastAttribute.Value.ToLower(), element.FirstAttribute.Value.ToLower());

            return dict;
        }
    }
}
