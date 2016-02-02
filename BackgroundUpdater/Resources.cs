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

        public static async void BgTaskHelper()
        {
            string path = String.Format(@"ms-appx:///Holidays/{0}/Holidays.xml", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            string fileContent;
            StorageFile f = StorageFile.GetFileFromApplicationUriAsync(new Uri(path)).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
            using (StreamReader sRead = new StreamReader(await f.OpenStreamForReadAsync()))
                fileContent = await sRead.ReadToEndAsync();

            doc = XDocument.Parse(fileContent);
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
                PersonalData = XDocument.Load(@"Holidays/" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName + "/PersData.xml");
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
