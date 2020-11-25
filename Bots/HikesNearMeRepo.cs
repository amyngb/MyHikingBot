using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class HikesNearMeRepo
    {
        public List<Hike> Trails { get; set; }


        public HikesNearMeRepo()
        { }

        [JsonConstructor]
        public HikesNearMeRepo(List<Hike> trails)
        {
            Trails = trails;
        }
    }

    public class Hike
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Url { get; set; }
    }
}