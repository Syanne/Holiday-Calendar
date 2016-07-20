using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Windows.Storage;
using Calendar.Models;
using Google.Apis.Util.Store;
using Windows.ApplicationModel;
using CalendarResources;
using System.Threading.Tasks;

namespace Calendar.SocialNetworkConnector
{
    class GoogleCalendarConnector: BaseConnector
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        UserCredential credential;
        
        public GoogleCalendarConnector(DateTime dateStart, DateTime dateEnd, ref List<ItemBase> items) : base(dateStart, dateEnd, ref items)
        { 
        }     

        private async void SetCredentials()
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                       new Uri("ms-appx:///SocialNetworkConnector/client_secret.json"),
                       new[] { CalendarService.Scope.CalendarReadonly },
                       "user",
                       CancellationToken.None);
        }

        public async override Task GetHolidayList()
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                       new Uri("ms-appx:///SocialNetworkConnector/client_secret.json"),
                       new[] { CalendarService.Scope.CalendarReadonly },
                       "user",
                       CancellationToken.None);

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "My Project",
            });

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List("primary");
            //set user max and min dates
            request.TimeMin = DateStart.Date;
            request.TimeMax = DateEnd.Date;
            //set details
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 100;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    var array = when.Split('-');
                    ItemBase item = new ItemBase
                    {
                        HolidayName = eventItem.Summary,
                        Day = Convert.ToInt32(array[2]),
                        Month = Convert.ToInt32(array[1]),
                        Year = Convert.ToInt32(array[0]),
                        HolidayTag = "M"
                    };

                    Items.Add(item);
                }
            }
            else Items = null;

            base.Message();          
        }
    }
}
