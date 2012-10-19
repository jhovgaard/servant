using System.Text;
using Servant.Business.Exceptions;

namespace Servant.Business.Helpers
{
    public static class SecurityHelper
    {
         public static string HashPassword(string password)
         {
             var algorithm = new System.Security.Cryptography.SHA512Managed();
             var hashByte = algorithm.ComputeHash(Encoding.UTF8.GetBytes(password));
             return hashByte.ToString();
         }

         public static bool IsPasswordValid(string password, string hash)
         {
             if(hash == null)
                 throw new SettingsException("The database doesn't contain a password.");

             return HashPassword(password) == hash;
         }
    }
}