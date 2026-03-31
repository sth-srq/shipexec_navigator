using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;
using System.IO.Ports;

namespace ShipExecNavigator.BusinessLogic
{
    internal static class JsonHelper
    {
        private static readonly DataContractJsonSerializerSettings _settings =
            new DataContractJsonSerializerSettings
            {
                DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
            };

        public static string Serialize<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T), _settings);
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), _settings);
                return (T)serializer.ReadObject(ms);
            }
        }
    }
}
