using System;
using System.Text;

namespace Servant.Business.Helpers
{
    public static class SecurityHelper
    {
         public static string HashPassword(string password)
         {
             var algorithm = new System.Security.Cryptography.SHA512Managed();
             var hashByte = algorithm.ComputeHash(Encoding.UTF8.GetBytes(password));
             return Convert.ToBase64String(hashByte);
         }

         public static bool IsPasswordValid(string password, string hash)
         {
             if (hash == null)
                 return false;

             return HashPassword(password) == hash;
         }
    }
}