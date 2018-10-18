using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackMeUp.AzureML.Models
{
    public class DoubleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // name used in a previous answer
            if (objectType == typeof(double))
            {
                return true;
            }

            return false;
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            double.TryParse(token.ToString(), out var d);
            return d;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}