using System.Threading.Tasks;

namespace Calendar.SocialNetworkConnector
{
    class FacebookConnector : BaseConnector
    {
        const string AppId = "884313808380322";
        const string AppSecret = "ab78433d18e56d0916838ba23cc5680a";
        private const string AuthenticationUrlFormat =
         "https://graph.facebook.com/oauth/access_token?client_id={0}&client_secret={1}&grant_type=client_credentials&scope=manage_pages,offline_access,publish_stream";
        
        string accessToken = null;

        protected override string ServiceName
        {
            get
            {
                return "facebook";
            }
        }

        public FacebookConnector(int period) : base(period)
        {
        }

        public override Task GetHolidayList()
        {
            return new Task(() =>
            {

            });
        }
        
    }
}
