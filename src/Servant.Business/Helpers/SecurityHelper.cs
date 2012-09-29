using Servant.Business.Exceptions;

namespace Servant.Business.Helpers
{
    public static class SecurityHelper
    {
         public static string HashPassword(string password)
         {
             return BCrypt.Net.BCrypt.HashPassword(password);
         }

         public static bool IsPasswordValid(string password, string hash)
         {
             if(hash == null)
                 throw new SettingsException("The database doesn't contain a password.");

             return BCrypt.Net.BCrypt.Verify(password, hash);
         }
    }
}