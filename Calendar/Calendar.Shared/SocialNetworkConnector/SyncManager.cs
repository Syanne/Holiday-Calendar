﻿using Calendar.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Calendar.SocialNetworkConnector
{
    public partial class SyncManager
    {
        List<ItemBase> SyncWith(string serverName, DateTime start, int period)
        {
            BaseConnector connector = null;
            List<ItemBase> collection = new List<ItemBase>();

            switch (serverName)
            {
                case "google": connector = new GoogleCalendarConnector(start, period, ref collection); break;
                case "facebook": break;
                case "outlook": break;
                case "vk": break;
                default: break;
            }

            connector.GetHolidayList();
            return collection;
        }
    }
}
