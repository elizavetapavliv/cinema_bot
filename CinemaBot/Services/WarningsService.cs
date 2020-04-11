using CinemaBot.CognitiveModels;
using CinemaBot.TranslationModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Services
{
    public class WarningsService: IWarningsService
    {
        private ICinemaService cinemaService;

        private ITranslationService translationService;

        public WarningsService(ICinemaService cinemaService, ITranslationService translationService)
        {
            this.cinemaService = cinemaService;
            this.translationService = translationService;
        }

        public async Task<bool> CheckForUnsupportedMovie(ITurnContext context, string movie, string language,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(movie))
            {
               var translatedMovie = language == TranslationSettings.defaultLanguage ? movie :
               (await translationService.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(movie) }, language))[0];
                if (!cinemaService.MoviesDictionary.ContainsKey(translatedMovie.ToLower()))
                {
                    var messageText = $"Sorry, but the following movie is not supported: {movie}";
                    var message = MessageFactory.Text(messageText, InputHints.IgnoringInput);
                    await context.SendActivityAsync(message, cancellationToken);
                    return false;
                }
                return true;
            }
            return false;
        }

        public async Task<bool> CheckForUnsupportedCinema(ITurnContext context, TicketBooking luisResult, CancellationToken cancellationToken)
        {
            var cinemaEntities = luisResult.CinemaEntities;
            if (!string.IsNullOrEmpty(cinemaEntities.Cinema))
            {
                if (string.IsNullOrEmpty(cinemaEntities.AvailableCinema))
                {
                    var messageText = $"Sorry, but the following cinema is not supported: {cinemaEntities.Cinema}";
                    var message = MessageFactory.Text(messageText, messageText,
                        InputHints.IgnoringInput);
                    await context.SendActivityAsync(message, cancellationToken);
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
