using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    public class SpecificHikeRepo
    {
        public string TrailName { get; set; }
        public string ImageUrl { get; set; }
        public string Location { get; set; }
        public string Summary { get; set; }
        public string Length { get; set; }

        public SpecificHikeRepo()
        { }

        [JsonConstructor]
        public SpecificHikeRepo(string trailName, string imageUrl, string location, string summary, string length)
        {
            TrailName = trailName;
            ImageUrl = imageUrl;
            Location = location;
            Summary = summary;
            Length = length;
        }
    }
}