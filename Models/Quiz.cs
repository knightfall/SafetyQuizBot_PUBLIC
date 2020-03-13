using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SafetyQuizBot.Models
{
    public partial class Quiz
    {
        [JsonProperty("Questions", NullValueHandling = NullValueHandling.Ignore)]
        public List<Question> Questions { get; set; }
    }

    public partial class Question
    {
        [JsonProperty("Question", NullValueHandling = NullValueHandling.Ignore)]
        public string QuestionQuestion { get; set; }

        [JsonProperty("Responses", NullValueHandling = NullValueHandling.Ignore)]
        public List<Response> Responses { get; set; }

        [JsonProperty("Correct", NullValueHandling = NullValueHandling.Ignore)]
        public string Correct { get; set; }


        [JsonProperty("TrueValues", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TrueValues { get; set; }

        [JsonProperty("True", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> True { get; set; }

        [JsonProperty("Limit", NullValueHandling = NullValueHandling.Ignore)]
        public long? Limit { get; set; }
    }

    public partial class Response
    {
        [JsonProperty("Title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("ID", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("Option", NullValueHandling = NullValueHandling.Ignore)]
        public string Option { get; set; }

        [JsonProperty("Feedback", NullValueHandling = NullValueHandling.Ignore)]
        public string Feedback { get; set; }

        [JsonProperty("emoji", NullValueHandling = NullValueHandling.Ignore)]
        public string Emoji { get; set; }

        [JsonProperty("corr", NullValueHandling = NullValueHandling.Ignore)]
        public Corr? Corr { get; set; }
    }

    public enum Corr { Alt, No, Rec };

    public partial class Quiz
    {
        public static Quiz FromJson(string json) => JsonConvert.DeserializeObject<Quiz>(json, SafetyQuizBot.Models.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Quiz self) => JsonConvert.SerializeObject(self, SafetyQuizBot.Models.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                CorrConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class CorrConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Corr) || t == typeof(Corr?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "alt":
                    return Corr.Alt;
                case "no":
                    return Corr.No;
                case "rec":
                    return Corr.Rec;
            }
            throw new Exception("Cannot unmarshal type Corr");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Corr)untypedValue;
            switch (value)
            {
                case Corr.Alt:
                    serializer.Serialize(writer, "alt");
                    return;
                case Corr.No:
                    serializer.Serialize(writer, "no");
                    return;
                case Corr.Rec:
                    serializer.Serialize(writer, "rec");
                    return;
            }
            throw new Exception("Cannot marshal type Corr");
        }

        public static readonly CorrConverter Singleton = new CorrConverter();
    }
}
