using Newtonsoft.Json;

namespace CinemaBot.Models
{
    public class Cinema
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }
}