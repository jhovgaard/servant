namespace Servant.Web.Helpers
{
    public static class MailchimpHelper
    {
        public static string Subscribe(string email, string firstname, string lastname)
        {
            const string subscribeUrl = "http://us5.api.mailchimp.com/1.3/?method=listSubscribe";
            var parameters = new[]
                {
                    "apikey=4a42b6d4e93a3a34b2f0ce5faac9539b-us5",
                    "email_address=" + email,
                    "id=dea225a6ba",
                    "merge_vars[FNAME]=" + firstname,
                    "merge_vars[LNAME]=" + lastname
                };
            var client = new System.Net.WebClient();
            var response = client.DownloadString(subscribeUrl + "&" + string.Join("&", parameters));

            return response;
        }
    }
}