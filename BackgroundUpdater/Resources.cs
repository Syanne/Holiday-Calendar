using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public static void BgTaskHelper()
        {
            Uri uri = new Uri("ms-appx:///Strings/Holidays.xml");
            // uri = new Uri(holFile.Path.Skip(8).ToString());
            var holFile = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            var read = FileIO.ReadTextAsync(holFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            doc = XDocument.Parse(read);

            try
            {
                var storageFolder = ApplicationData.Current.RoamingFolder;
                var file = storageFolder.GetFileAsync("PersData.xml").AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                string text = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                PersonalData = XDocument.Parse(text);
            }
            //if it's the fist launch - load basic file
            catch
            {
                var persUri = new Uri("ms-appx:///Strings/PersData.xml");
                var persFile = StorageFile.GetFileFromApplicationUriAsync(persUri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                var persRead = FileIO.ReadTextAsync(persFile).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                PersonalData = XDocument.Parse(persRead);
            }

        }


        /// <summary>
        /// Load Personal Data
        /// </summary>
        public static void LoadPersonalData()
        {
            
            if (PersonalData == null)
            {
                    //load file 
                    StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
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
