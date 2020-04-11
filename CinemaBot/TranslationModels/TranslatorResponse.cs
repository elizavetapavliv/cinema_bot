using Newtonsoft.Json;
using System.Collections.Generic;

namespace CinemaBot.TranslationModels
{
    public class TranslatorResponse
    {
        [JsonProperty("translations")]
        public IEnumerable<TranslatorResult> Translations { get; set; }
    }
}
