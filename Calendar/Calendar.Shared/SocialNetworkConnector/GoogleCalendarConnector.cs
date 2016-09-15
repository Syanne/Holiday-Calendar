using Calendar.Data.Models;
using Calendar.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Calendar.SocialNetworkConnector
{
    class GoogleCalendarConnector: BaseConnector
    {        
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        UserCredential credential;

        /// <summary>
        /// name of a service to connect
        /// </summary>
        protected override string ServiceName
        {
            get { return "google"; }      
        }

        /// <summary>  
        /// Creates a new object of BaseConnector 
        /// </summary>
        /// <param name="dateStart">sync start date</param>
        /// <param name="period">snooze time</param>
        public GoogleCalendarConnector(DateTime dateStart, int period) : base(dateStart, period)
        { }

        /// <summary>
        /// Gets records from service
        /// </summary>
        /// <returns>requires async/await</returns>
        public async override Task GetHolidayList()
        {
#if WINDOWS_PHONE_APP
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                      new Uri("ms-appx:///SocialNetworkConnector/client_wp.json"),
                      Scopes,
                      "user",
                      CancellationToken.None);
#else
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                       new Uri("ms-appx:///SocialNetworkConnector/client_secret.json"),
                       Scopes,
                       "user",
                       CancellationToken.None);
#endif

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Calendar and Holidays",
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
                    string when = null;
                    try
                    {
                        when = eventItem.Start.DateTime.Value.Date.ToString(LocalDataManager.DateFormat);
                    }
                    catch
                    {
                        if (String.IsNullOrEmpty(when))
                            when = eventItem.Start.Date;                        
                    }

                    var array = when.Split(LocalDataManager.DateSeparator);
                    HolidayItem item = new HolidayItem
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
            Message();
        }
    }
}
