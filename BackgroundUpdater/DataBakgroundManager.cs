using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using Calendar.Data.Services;
using Calendar.Data.Models;

namespace BackgroundUpdater
{
    class DataBakgroundManager : DataManager
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

        int CallerID { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="callerID">1 - tile/smartTile, 2 - toast</param>
        public DataBakgroundManager(int callerID)
        {
            CallerID = callerID;
            if (callerID == 2)
                DataManager.LoadApplicationDataFiles(false);
            else DataManager.LoadApplicationDataFiles(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date">selected date</param>
        /// <returns></returns>
        public List<HolidayItem> GetHolidayList(DateTime date)
        {
            var collection = DataManager.GetComposedData(date, CallerID);

            return collection;
        }

        public List<HolidayItem> GetSmartTileDataColection()
        {
            return DataManager.GetSmartTileCollection();
        }
        
    }
}
