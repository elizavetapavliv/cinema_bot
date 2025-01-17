// <auto-generated>
// Code generated by LUISGen TicketBooking.json -cs CognitiveModels.TicketBooking -o .
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace CinemaBot.CognitiveModels
{
    public partial class TicketBooking: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        public enum Intent {
            BookTicket, 
            Cancel, 
            Help, 
            None
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] Movie;

            // Built-in entities
            public DateTimeSpec[] datetime;

            // Lists
            public string[][] AvailableCinema;

            // Composites
            public class _InstanceCinema
            {
                public InstanceData[] AvailableCinema;
            }
            public class CinemaClass
            {
                public string[][] AvailableCinema;
                [JsonProperty("$instance")]
                public _InstanceCinema _instance;
            }
            public CinemaClass[] Cinema;

            // Instance
            public class _Instance
            {
                public InstanceData[] AvailableCinema;
                public InstanceData[] Cinema;
                public InstanceData[] Movie;
                public InstanceData[] datetime;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<TicketBooking>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
