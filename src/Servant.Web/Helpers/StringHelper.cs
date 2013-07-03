namespace Servant.Web.Helpers
{
    public static class StringHelper
    {
         public static string Ellipticize(this string str, int maxLength)
         {
             if (str.Length > maxLength)
                 str = str.Substring(0, maxLength - 3) + "...";

             return str;
         }

         public static string EllipticizeForErrorDescription(this string str)
         {
             return Ellipticize(str, 80);
         }
    }
}