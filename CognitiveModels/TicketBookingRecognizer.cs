using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.CognitiveModels
{
    public class TicketBookingRecognizer : IRecognizer
    {
        private readonly LuisRecognizer recognizer;
        public TicketBookingRecognizer(IConfiguration configuration)
        {
            var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) 
                && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) 
                && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            if (luisIsConfigured)
            {
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    "https://" + configuration["LuisAPIHostName"]);

                recognizer = new LuisRecognizer(luisApplication);
            }
        }
        public virtual bool IsConfigured => recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, 
            CancellationToken cancellationToken)
            => await recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, 
            CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}
