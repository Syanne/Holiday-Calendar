using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundUpdater
{
    public sealed class ToastBackgroundTask : IBackgroundTask
    {
        static DataBakgroundManager _manager;
        static DateTime SelectedDate { get; set; }
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely 
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            _manager = new DataBakgroundManager();
            _manager.LoadPersonalData();

            //get date
            int day = _manager.GetToastSnooze();
            SelectedDate = DateTime.Now.Date.AddDays(day);

            //start updating
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
            //prepare collection
            List<Event> collection =  _manager.PersonalAndServices(SelectedDate.Month, SelectedDate.Year);
            collection = collection.Where(p => p.Day == SelectedDate.Day || p.Value == "google calendar").ToList();

            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    // Using the ToastText02 toast template.
                    ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;

                    // Retrieve the content part of the toast so we can change the text.
                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                    //Find the text component of the content
                    XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");

                    // Set the text on the toast. 
                    // The first line of text in the ToastText02 template is treated as header text, and will be bold.
                    toastTextElements[0].AppendChild(toastXml.CreateTextNode(SelectedDate.ToString("d")));
                    toastTextElements[1].AppendChild(toastXml.CreateTextNode(item.Value));

                    // Set the duration on the toast
                    IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                    ((XmlElement)toastNode).SetAttribute("duration", "long");

                    // Create the actual toast object using this toast specification.
                    ToastNotification toast = new ToastNotification(toastXml);
                    ToastNotificationManager.CreateToastNotifier().Show(toast);
                }
            }
        }
    }
}
