using Calendar.Data.Models;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundUpdater
{
    public sealed class SmartTileBackgroundTask : IBackgroundTask
    {
        static DataBakgroundManager _manager;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //load data
            _manager = new DataBakgroundManager(1);

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

            //prepare collection
            List<HolidayItem> eventCollection = _manager.GetSmartTileDataColection();           
            
            if (eventCollection.Count > 0)
                foreach (var currEvent in eventCollection)
                {
                    //date to show
                    DateTime date = new DateTime((int)currEvent.Year, (int)currEvent.Month, currEvent.Day);

                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(String.Format(DataBakgroundManager.XML_TEMPLATE, date.ToString("d"), currEvent.HolidayName));

                    var notification = new TileNotification(xmlDocument);
                    updateManager.Update(notification);
                }
        }
    }
}

