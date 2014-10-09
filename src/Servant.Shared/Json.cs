using System;
using ServiceStack.Text;

namespace Servant.Shared
{
    public static class Json
    {
        static Json()
        {
            JsConfig<Guid>.SerializeFn = guid => guid.ToString("D");
            JsConfig<Guid?>.SerializeFn = guid => guid.HasValue ? guid.Value.ToString("D") : null;
        }

        public static string SerializeToString<T>(T value)
        {
            return JsonSerializer.SerializeToString(value);
        }

        public static T DeserializeFromString<T>(string value)
        {
            return JsonSerializer.DeserializeFromString<T>(value);
        }
    }
}
