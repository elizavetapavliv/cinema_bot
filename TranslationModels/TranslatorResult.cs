using Newtonsoft.Json;

namespace CinemaBot.TranslationModels
{
    public class TranslatorResult
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
