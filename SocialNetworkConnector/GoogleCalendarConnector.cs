using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SocialNetworkConnector
{
    public class GoogleCalendarConnector : IConnector
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/calendar-dotnet-quickstart.json
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        List<ItemBase> Items;

        public GoogleCalendarConnector(ref List<ItemBase> items)
        {
            this.Items = items;
        }

        public async void SetHolidays()
        {
            //items = new List<ItemBase>();
            UserCredential credential;

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
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;


            // List events.
            Events events = request.Execute();
            // Console.WriteLine("Upcoming events:");
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

                    // Console.WriteLine("{0} ({1})", eventItem.Summary, when);
                }
            }
            else
            {
                // Console.WriteLine("No upcoming events found.");
            }
            //  Console.Read();

            //return items;
        }

        List<ItemBase> IConnector.GetHolidayList()
        {
            throw new NotImplementedException();
        }
    }
}
