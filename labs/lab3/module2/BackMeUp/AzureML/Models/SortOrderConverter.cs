using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BackMeUp.AzureML.Models
{
    public class SortOrderConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(int));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.ToString())
            {
                case "Day1_Success":
                    return 0;
                case "Week1_Success":
                    return 1;
                case "Month3_Success":
                    return 2;
                case "Day1_Unsuccessful":
                    return 3;
                case "Week1_Unsuccessful":
                    return 4;
                case "Month3_Unsuccessful":
                    return 5;
                case "Day1_Repeat_Surgery":
                    return 6;
                case "Week1_Repeat_Surgery":
                    return 7;
                case "Month3_Repeat_Surgery":
                    return 8;
                case "Long_Term_Pain_Mngmnt":
                    return 9;
                default:
                    throw new InvalidCastException($"The value \"{token.ToString()}\" is not recognized");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}