using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SafetyQuizBot.Models
{
    public partial class AnswerParams
    {
        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public Parameters Parameters { get; set; }
    }

    public partial class Parameters
    {
        [JsonProperty("alpha", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Alpha { get; set; }
    }

    public partial class AnswerParams
    {
        public static AnswerParams FromJson(string json) => JsonConvert.DeserializeObject<AnswerParams>(json, SafetyQuizBot.Models.AnswerParamsConverter.Settings);
    }

    public static class AnswerParamsSerialize
    {
        public static string ToJson(this AnswerParams self) => JsonConvert.SerializeObject(self, SafetyQuizBot.Models.AnswerParamsConverter.Settings);
    }

    internal static class AnswerParamsConverter
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
