using Newtonsoft.Json;
using System;

namespace CinemaBot.Models
{
    public class Show
    {
        [JsonProperty("showId")]
        public string Id { get; set; }

        [JsonProperty("start")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end")]
        public DateTime EndTime { get; set; }

        [JsonProperty("theater")]
        public Cinema Cinema { get; set; }
    }
}