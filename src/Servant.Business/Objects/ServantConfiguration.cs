using System.ComponentModel.DataAnnotations;

namespace Servant.Business.Objects
{
    public class ServantConfiguration
    {
        [Required(ErrorMessage = "The URL cannot be empty."), RegularExpression(@"^([a-z0-9+.-]+):(?://(?:((?:[a-z0-9-._~!$&'()*+,;=:]|%[0-9A-F]{2})*)@)?((?:[a-z0-9-._~!$&'()*+,;=]|%[0-9A-F]{2})*)(?::(\d*))?(/(?:[a-z0-9-._~!$&'()*+,;=:@/]|%[0-9A-F]{2})*)?|(/?(?:[a-z0-9-._~!$&'()*+,;=:@]|%[0-9A-F]{2})+(?:[a-z0-9-._~!$&'()*+,;=:@/]|%[0-9A-F]{2})*)?)(?:\?((?:[a-z0-9-._~!$&'()*+,;=:/?@]|%[0-9A-F]{2})*))?(?:#((?:[a-z0-9-._~!$&'()*+,;=:/?@]|%[0-9A-F]{2})*))?$", ErrorMessage = "The URL is not valid.")]
        public string ServantUrl { get; set; }
        public bool Debug { get; set; }
        [Required]
        public string Username { get; set; }
        public string Password { get; set; }
        public bool SetupCompleted { get; set; }
        public bool EnableErrorMonitoring { get; set; }
        public bool AutoSendCrashReport { get; set; }

        public ServantConfiguration()
        {
            ServantUrl = "http://localhost:54444/";
            Username = "admin";
        }

        public bool IsHttps()
        {
            return ServantUrl.StartsWith("https://");

        }
    }
}