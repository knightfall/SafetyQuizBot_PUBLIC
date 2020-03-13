using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SafetyQuizBot.Models
{
    public partial class MessengerEvent
    {
        [JsonProperty("object", NullValueHandling = NullValueHandling.Ignore)]
        public string Object { get; set; }

        [JsonProperty("entry", NullValueHandling = NullValueHandling.Ignore)]
        public List<Entry> Entry { get; set; }
    }

    public partial class Entry
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
        public long? Time { get; set; }

        [JsonProperty("messaging", NullValueHandling = NullValueHandling.Ignore)]
        public List<Messaging> Messaging { get; set; }
    }

    public partial class Messaging
    {
        [JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
        public Recipient Sender { get; set; }

        [JsonProperty("recipient", NullValueHandling = NullValueHandling.Ignore)]
        public Recipient Recipient { get; set; }

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public long? Timestamp { get; set; }

        [JsonProperty("read", NullValueHandling = NullValueHandling.Ignore)]
        public Read Read { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public Message Message { get; set; }

        [JsonProperty("postback", NullValueHandling = NullValueHandling.Ignore)]
        public Postback Postback { get; set; }
    }

    public partial class Message
    {
        [JsonProperty("mid", NullValueHandling = NullValueHandling.Ignore)]
        public string Mid { get; set; }

        [JsonProperty("seq", NullValueHandling = NullValueHandling.Ignore)]
        public long? Seq { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("quick_reply", NullValueHandling = NullValueHandling.Ignore)]
        public QuickReply QuickReply { get; set; }
    }


    public partial class Postback
    {
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public string Payload { get; set; }
    }

    public partial class Read
    {
        [JsonProperty("seq", NullValueHandling = NullValueHandling.Ignore)]
        public long? Seq { get; set; }

        [JsonProperty("watermark", NullValueHandling = NullValueHandling.Ignore)]
        public long? Watermark { get; set; }
    }

    public partial class Recipient
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
    }

    public partial class MessengerEvent
    {
        public static MessengerEvent FromJson(string json) => JsonConvert.DeserializeObject<MessengerEvent>(json, SafetyQuizBot.Models.MessengerEventConverter.Settings);
    }

    public static class MessengerEventSerialize
    {
        public static string ToJson(this MessengerEvent self) => JsonConvert.SerializeObject(self, SafetyQuizBot.Models.MessengerEventConverter.Settings);
    }

    internal static class MessengerEventConverter
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
}
