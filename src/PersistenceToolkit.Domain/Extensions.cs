using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PersistenceToolkit.Domain
{
    public static class Extensions
    {
        public static string GetJson(this object obj)
        {
            var actualType = obj.GetType();
            return JsonSerializer.Serialize(obj, actualType);
        }
        public static T GetObject<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        public static T DeepClone<T>(this T obj)
        {
            return JsonSerializer.Deserialize<T>(
                JsonSerializer.Serialize(obj));
        }
    }
}
