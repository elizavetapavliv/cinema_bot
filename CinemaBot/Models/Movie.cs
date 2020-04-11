using Newtonsoft.Json;
using System.Collections.Generic;

namespace CinemaBot.Models
{
    public class Movie
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("posterLink")]
        public string Poster { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("showList")]
        public Dictionary<string, List<Show>> ShowList { get; set; }
    }
}