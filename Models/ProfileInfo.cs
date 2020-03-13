using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SafetyQuizBot.Models
{
    public partial class ProfileInfo
    {
        
        [JsonProperty("first_name", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; set; }

        [JsonProperty("last_name", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; set; }

        [JsonProperty("profile_pic", NullValueHandling = NullValueHandling.Ignore)]
        public string ProfilePic { get; set; }
        [Key]
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }
        [JsonProperty("verified", NullValueHandling = NullValueHandling.Ignore)]
        public string Verified { get; set; }
    }

    public partial class ProfileInfo
    {
        public static ProfileInfo FromJson(string json) => JsonConvert.DeserializeObject<ProfileInfo>(json, SafetyQuizBot.Models.ProfileInfoConverter.Settings);
    }

    public static class ProfileInfoSerialize
    {
        public static string ToJson(this ProfileInfo self) => JsonConvert.SerializeObject(self, SafetyQuizBot.Models.ProfileInfoConverter.Settings);
    }

    internal static class ProfileInfoConverter
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
