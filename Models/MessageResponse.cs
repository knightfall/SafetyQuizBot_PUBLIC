using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SafetyQuizBot.Models
{
    public partial class MessageResponse
    {
        [JsonProperty("recipient", NullValueHandling = NullValueHandling.Ignore)]
        public Recipient Recipient { get; set; }

        [JsonProperty("messaging_type", NullValueHandling = NullValueHandling.Ignore)]
        public string MessagingType { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public ResponseMessage ResponseMessage { get; set; }
    }

    public partial class ResponseMessage
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string RText { get; set; }

        [JsonProperty("quick_replies", NullValueHandling = NullValueHandling.Ignore)]
        public List<QuickReply> QuickReplies { get; set; }
    }

    public partial class QuickReply
    {
        [JsonProperty("content_type", NullValueHandling = NullValueHandling.Ignore)]
        public string ContentType { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public string Payload { get; set; }
    }

    public partial class MessageResponse
    {
        public static MessageResponse FromJson(string json) => JsonConvert.DeserializeObject<MessageResponse>(json, SafetyQuizBot.Models.MessageResponseConverter.Settings);
    }

    public static class MessageResponseSerialize
    {
        public static string ToJson(this MessageResponse self) => JsonConvert.SerializeObject(self, SafetyQuizBot.Models.MessageResponseConverter.Settings);
    }

    internal static class MessageResponseConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
