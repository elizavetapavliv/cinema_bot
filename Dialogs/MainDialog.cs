using CinemaBot.CognitiveModels;
using CinemaBot.Models;
using CinemaBot.Services;
using CinemaBot.TranslationModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private TicketBookingRecognizer luisRecognizer;

        private ICinemaService cinemaService;
        private IWarningsService warningsService;
        private ITranslationService translationService;

        private IStatePropertyAccessor<string> languageStateProperty;
        private IStatePropertyAccessor<bool> needToTranslateProperty;

        private string language;
        public MainDialog(TicketBookingRecognizer recognizer, BookingDialog bookingDialog, ICinemaService cinemaService, 
            IWarningsService warningsService, ITranslationService translationService, UserState userState) : base(nameof(MainDialog))
        {
            luisRecognizer = recognizer;
            this.cinemaService = cinemaService;
            this.warningsService = warningsService;
            this.translationService = translationService;
            languageStateProperty = userState.CreateProperty<string>("LanguagePreference");
            needToTranslateProperty = userState.CreateProperty<bool>("NeedToTranslate");

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            await needToTranslateProperty.SetAsync(stepContext.Context, true, cancellationToken);
            if (!luisRecognizer.IsConfigured)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var messageText = stepContext.Options?.ToString() ??
                "What can I help?\nSay something like \"book a ticket for \"Give your soul\" on March 22, 2020 in Galileo\"";
            var promptMessage = MessageFactory.Text(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (!luisRecognizer.IsConfigured)
            {
                return await stepContext.BeginDialogAsync(nameof(BookingDialog),
                    new BookingDetails(), cancellationToken);
            }

            var luisResult = await luisRecognizer.RecognizeAsync<TicketBooking>(
                stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case TicketBooking.Intent.BookTicket:
                    language = await languageStateProperty.GetAsync(stepContext.Context, () => TranslationSettings.defaultLanguage);
                    await cinemaService.LoadCurrentMoviesAsync(language);
                    Movie movie = null;
                    var movieName = luisResult.MovieEntity;
                    Show show = null;
                    var cinemaName = luisResult.CinemaEntities.Cinema;

                    if (await warningsService.CheckForUnsupportedMovie(stepContext.Context, movieName, language, cancellationToken))
                    {
                        movie = new Movie();
                        movie.Name = movieName;
                    }
                    if (await warningsService.CheckForUnsupportedCinema(stepContext.Context, luisResult, cancellationToken))
                    {
                        show = new Show();
                        show.Cinema = new Cinema();
                        show.Cinema.Name = cinemaName;
                    }

                    var bookingDetails = new BookingDetails()
                    {
                        Movie = movie,
                        Show = show,
                        MovieDate = luisResult.MovieDate,
                    };

                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case TicketBooking.Intent.Help:
                    var helpMessageText = "Try asking me to 'book a ticket'.";
                    var getWeatherMessage = MessageFactory.Text(helpMessageText, InputHints.IgnoringInput);
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, helpMessageText, cancellationToken);

                case TicketBooking.Intent.Cancel:
                    var cancelMessageText = "Bye bye!";
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(cancelMessageText,
                        InputHints.IgnoringInput), cancellationToken);
                    return await stepContext.CancelAllDialogsAsync(cancellationToken);

                default:
                    var notUnderstandMessageText = "Sorry, I didn't get that. Please try asking in a different way";
                    var didntUnderstandMessage = MessageFactory.Text(notUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            if (stepContext.Result is BookingDetails result)
            {
                var messageText = "Your link for booking ticket";
                messageText = language == TranslationSettings.defaultLanguage ? messageText :
                (await translationService.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(messageText) }, language))[0]; 
                var reply = MessageFactory.Attachment(new List<Attachment>());
                var heroCard = new HeroCard()
                {
                    Title = messageText,
                    Tap = new CardAction()
                    {
                        Type = ActionTypes.OpenUrl,
                        Value = cinemaService.GetOrderTicketUri(result.Movie.Code, result.Show.Id)
                    }
                };
                reply.Attachments.Add(heroCard.ToAttachment());
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }
            await needToTranslateProperty.SetAsync(stepContext.Context, true, cancellationToken);
            var promptMessage = "What else can I do?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}