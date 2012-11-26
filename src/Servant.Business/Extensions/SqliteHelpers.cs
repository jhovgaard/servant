using System;

namespace Servant.Business.Extensions
{
    public static class SqliteHelpers
    {
        public static string ToSqlLiteDateTime(this DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToSqlLiteDateTime(this DateTime? value)
        {
            if (value.HasValue)
                return ToSqlLiteDateTime(value.Value);

            return null;
        }
    }
}