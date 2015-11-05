using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace BackgroundUpdater
{
     class ResourceManager
    {
        public static XDocument PersonalData;
        public static XDocument doc;
        public static ResourceLoader resource;

        public static void BgTaskHelper()
        {
            var po = Package.Current.InstalledLocation.Path + resource.GetString("LocalHolidaysPath1");

            var fileeee = StorageFile.GetFileFromPathAsync(po).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            String stamp = FileIO.ReadTextAsync(fileeee).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

            doc = XDocument.Parse(stamp);

            if (PersonalData == null)
            {
                //load file 
                var storageFolder = ApplicationData.Current.RoamingFolder;
                var file = storageFolder.GetFileAsync("PersData.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                PersonalData = XDocument.Parse(text);
            }
        }


        /// <summary>
        /// Load Personal Data
        /// </summary>
        public static void LoadPersonalData()
        {
            doc = XDocument.Load(resource.GetString("LocalHolidaysPath"));
            if (PersonalData == null)
            {
                    //load file 
                    StorageFolder localFolder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                    StorageFile sampleFile = localFolder.GetFileAsync("PersData.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                    //read file
                    var randomAccessStream = sampleFile.OpenReadAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    String timestamp = FileIO.ReadTextAsync(sampleFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

                    if (timestamp == "") throw new Exception();

                    PersonalData = XDocument.Parse(timestamp);
                    if (PersonalData == null) throw new Exception();
                
            }
        }
    }
}
