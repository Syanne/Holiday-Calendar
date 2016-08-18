﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundUpdater
{
    public sealed class TileBackgroundTask : IBackgroundTask
    {
        static DataBakgroundManager _manager;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //load data
            _manager = new DataBakgroundManager();
            _manager.LoadResourceHolidays();

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
            List<Event> eventCollection = _manager.LoadXml(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            eventCollection.AddRange(_manager.PersonalAndServices(DateTime.Now.Month, DateTime.Now.Year));

            if (eventCollection.Count < 5)
            {
                DateTime dt = DateTime.Now.AddMonths(1);
                List<Event> s = _manager.LoadXml(dt.Year, dt.Month, 1);
                eventCollection.AddRange(s);
                eventCollection.AddRange(_manager.PersonalAndServices(dt.Month, dt.Year));
            }

            //finalize collection
            eventCollection = (eventCollection.Count <= 5) ? eventCollection : eventCollection.Take(5).ToList();
            
            if (eventCollection.Count > 0)
                foreach (var currEvent in eventCollection)
                {
                    //date to show
                    DateTime date = new DateTime(currEvent.Year, currEvent.Month, currEvent.Day);

                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(String.Format(DataBakgroundManager.XML_TEMPLATE, date.ToString("d"), currEvent.Value));

                    var notification = new TileNotification(xmlDocument);
                    updateManager.Update(notification);
                }
        }
    }
}
