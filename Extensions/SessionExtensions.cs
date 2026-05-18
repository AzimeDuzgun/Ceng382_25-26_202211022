#nullable enable
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace DZGNCatering.Extensions
{
    public static class SessionExtensions
    {
        // Objeyi JSON'a çevirip Session'a gömer
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Session'daki JSON'u okuyup tekrar Objeye çevirir
        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}