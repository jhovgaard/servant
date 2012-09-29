using System.Collections.Generic;
using Nancy.Security;

namespace Servant.Manager.Infrastructure
{
    public class UserIdentity : IUserIdentity   
    {
        public string UserName { get; set; }
        public IEnumerable<string> Claims { get; set; }
        public UserIdentity(string userName, IEnumerable<string> claims) {
            UserName = userName;
            Claims = claims;
        }
    }
}