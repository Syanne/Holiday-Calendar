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
        /// row*10+col of the first day in the month
        /// </summary>
        public static int Start { get; private set; }
        /// <summary>
        /// row*10+col of the last day in the month
        /// </summary>
        public static int End { get; private set; }

        public static int Weekend { get; private set; } = -1;

        protected static GeneralDataResource genDataResource { get; set; }
        protected static PersonalDataResource persDataResource { get; set; }

        protected static ResourceLoader Resource { get; set; } = null;

        public static void SetResource(ResourceLoader value)
        {
            Resource = value;
        }
        
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

        public static string GetStringFromResourceLoader(string resourceStringName)
        {
            return Resource.GetString(resourceStringName);
        }

        /// <summary>
        /// Load Holidays Data (general, then personal)
        /// </summary>
        /// <param name="loadDoc">load files</param>
        protected static void LoadPersonalData(bool loadDoc)
        {
            //personal
            persDataResource = new PersonalDataResource();

            //basic collection
            if (loadDoc)
            {
                genDataResource = new GeneralDataResource();

                //set collection of selected categories of holidays
                var collection = GetCollectionFromSourceFile("theme");
                genDataResource.SetSelectedCategoriesList(collection);
            }
        }

        #region Collection of Items
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
        public static void SetToastSnoozeValue(string value)
        {
            persDataResource.SetToastSnoozeValue(value);
        }

        public static int GetToastSnoozeValue()
        {
            return persDataResource.GetToastSnoozeValue();
        }

        public static void WriteNode(XmlWriter writer, string name, string tag)
        {
            persDataResource.WriteNode(writer, name, tag);
        }

        public static void SavePersonal(string name, string day, string month, string year, bool needSave, string parentTag)
        {
            persDataResource.SavePersonal(name, day, month, year, needSave, parentTag);
        }

        /// <summary>
        /// Remove note from file 
        /// </summary>
        /// <param name="name">Note text</param>
        /// <param name="day">day</param>
        /// <param name="month">month</param>
        /// <param name="year">year</param>
        public static void RemoveHoliday(string name, string day, string month, string year)
        {
            persDataResource.RemoveHoliday(name, day, month, year);
        }
        /// <summary>
        /// Change note
        /// </summary>
        /// <param name="oldName">old text</param>
        /// <param name="newName">changed text</param>
        /// <param name="day">selected day</param>
        /// <param name="month">selected month</param>
        /// <param name="year">selected year (or 0)</param> 
        public static void ChangePersonal(string oldName, string newName, string day, string month, string year)
        {
            persDataResource.ChangePersonal(oldName, newName, day, month, year);
        }

        #endregion

        #region general data
        public static List<DocElement> GetCollectionFromSourceFile(DateTime SelectedDate, string collectionType)
        {
            return genDataResource.GetCollectionFromSourceFile(SelectedDate, collectionType);
        }

        public static Dictionary<string, string> GetSelectedCategoriesList()
        {
            return genDataResource.SelectedCategories;
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

        public static List<HolidayItem> GetSmartTileCollection()
        {
            var smartTile = new SmartTileDataResource();
            return smartTile.LoadSmartTileFile();
        }

        #endregion

        /// <summary>
        /// get collection of XElements from resource files
        /// </summary>
        /// <param name="collectionType"></param>
        /// <returns></returns>
        public static List<XElement> GetCollectionFromSourceFile(string collectionType)
        {
            List<XElement> holidayCollection = new List<XElement>();

            if (collectionType == "categories")
                holidayCollection = genDataResource.GetCollectionFromSourceFile();
            else holidayCollection = persDataResource.GetCollectionFromSourceFile(collectionType);

            return holidayCollection;
        }
    }
}
