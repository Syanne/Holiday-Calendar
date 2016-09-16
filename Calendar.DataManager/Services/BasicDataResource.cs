using Calendar.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Popups;

namespace Calendar.Data.Services
{
    public abstract class BasicDataResource
    {
        /// <summary>
        /// File basic path
        /// </summary>
        protected abstract string Path { get; }
        
        /// <summary>
        /// Instance of a document
        /// </summary>
        public XDocument Document { get; protected set; }

        /// <summary>
        /// Loads file from path
        /// </summary>
        /// <param name="filepath">path to a file</param>
        /// <param name="folder">folder or null (to load from application folder)</param>
        /// <returns></returns>
        protected XDocument LoadFile(string filepath, StorageFolder folder)
        {
            try
            {
                StorageFile file;

                //load file
                if (folder == null)
                {
                    var uri = new Uri(filepath);
                    file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                else
                {
                    file = folder.GetFileAsync(filepath).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                }

                //parse and return
                var result = FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                return XDocument.Parse(result);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Prepare ollection of elements.
        /// Key - parent.
        /// Value - list of descendants.
        /// </summary>
        /// <param name="parentName">parent node name</param>
        /// <param name="extraKey">extra parameter</param>
        /// <returns>Collection of elements</returns>
        public virtual IEnumerable<XElement> GetItemList(string parentName, int? extraKey)
        {
            return null;
        }

        public async void MyMessage(string text)
        {
            var dial = new MessageDialog(text);

            dial.Commands.Add(new UICommand("OK"));
            var command = await dial.ShowAsync();
        }

        protected virtual void SaveDocument()
        {

        }
    }
}
