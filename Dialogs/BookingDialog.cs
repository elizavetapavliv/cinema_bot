using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CinemaBot.Models;
using CinemaBot.Services;
using CinemaBot.TranslationModels;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CinemaBot.Dialogs
{
    public class BookingDialog : CancelAndHelpDialog
    {
        private ICinemaService cinemaService;
        private ITranslationService translationService;

        private IStatePropertyAccessor<string> languageStateProperty;
        private IStatePropertyAccessor<bool> needToTranslateProperty;

        private string language;

        public BookingDialog(ICinemaService cinemaService, UserState userState, ITranslationService translationService)
            : base(nameof(BookingDialog))
        {
            this.cinemaService = cinemaService;
            this.translationService = translationService;
            languageStateProperty = userState.CreateProperty<string>("LanguagePreference");
            needToTranslateProperty = userState.CreateProperty<bool>("NeedToTranslate");

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog(cinemaService));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                MovieStepAsync,
                MovieDateStepAsync,
                CinemaStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));
            
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> MovieStepAsync(
            WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            language = await languageStateProperty.GetAsync(stepContext.Context, () => TranslationSettings.defaultLanguage);
            var bookingDetails = (BookingDetails)stepContext.Options;

            if (bookingDetails.Movie == null)
            {
                var movies = cinemaService.Movies;
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("What movie do you want to see ?"), cancellationToken);

                var reply = stepContext.Context.Activity.CreateReply();
                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                foreach (var movie in movies)
                {
                    var heroCard = new HeroCard()
                    {
                        Title = movie.Name,
                        Images = new List<CardImage>()
                        {
                            new CardImage()
                            {
                                Url = movie.Poster
                            }
                        },
                        Tap = new CardAction()
                        {
                            Type = ActionTypes.ImBack,
                            Value = movie.Name
                        }
                    };
                    reply.Attachments.Add(heroCard.ToAttachment());
                }
                await needToTranslateProperty.SetAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = reply });
            }
            return await stepContext.NextAsync(bookingDetails.Movie, cancellationToken);
        }

        private async Task<DialogTurnResult> MovieDateStepAsync(
          WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {       
            var bookingDetails = (BookingDetails)stepContext.Options;
            if (stepContext.Result is Movie movie)
            {
                var movieName = movie.Name;
                movieName = TranslateIfNeedAsync(movieName).Result;
                bookingDetails.Movie = cinemaService.Movies.FirstOrDefault(m => m.Name.ToLower() == movieName.ToLower());
            }
            else
            {
                bookingDetails.Movie = cinemaService.Movies.FirstOrDefault(m => m.Name == (string)stepContext.Result);
            }
            await needToTranslateProperty.SetAsync(stepContext.Context, true, cancellationToken);
            return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> CinemaStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;
            bookingDetails.MovieDate = (string)stepContext.Result;
            var movieName = bookingDetails.Movie.Name;

            var localizedMovieDate = getLocalizedDate(bookingDetails.MovieDate);
            var showList = cinemaService.GetMovieShowList(movieName.ToLower(), bookingDetails.MovieDate);

            await needToTranslateProperty.SetAsync(stepContext.Context, false, cancellationToken);

            if (bookingDetails.Show != null)
            {
                var cinemaName = bookingDetails.Show.Cinema.Name;
                cinemaName = cinemaService.GetRealCinemaName(cinemaName);
                bookingDetails.Show = showList.FirstOrDefault(show => show.Cinema.Name == cinemaName);
                if (bookingDetails.Show == null)
                {
                    var notGoing = await TranslateIfNeedAsync("does not go");
                    var inCinema = await TranslateIfNeedAsync("in");

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"\"{movieName}\" {notGoing} " +
                        $"{localizedMovieDate} {inCinema} {cinemaName}"), cancellationToken);
                }
            }

            if (bookingDetails.Show == null)
            {
                var searching = await TranslateIfNeedAsync("Searching for cinemas where");
                var isGoing = await TranslateIfNeedAsync("is going");
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{searching} " +
                    $"\"{movieName}\" {isGoing} {localizedMovieDate}..."), cancellationToken);

                var reply = stepContext.Context.Activity.CreateReply();
                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                await needToTranslateProperty.SetAsync(stepContext.Context, true, cancellationToken);
                await stepContext.Context.SendActivityAsync( MessageFactory.Text(
                    $"I found in total {showList.Count()} movie screenings on {localizedMovieDate}:"), cancellationToken);

                foreach (var show in showList)
                {
                    var cinema = show.Cinema;
                    var startTime = show.StartTime.ToString("HH:mm");
                    var endTime = show.EndTime.ToString("HH:mm");
                    var heroCard = new HeroCard()
                    {
                        Title = cinema.Name,
                        Subtitle = cinema.Address,
                        Text = await TranslateIfNeedAsync($"Show time: {startTime} - {endTime}"),
                        Buttons = new List<CardAction>()
                        { 
                            new CardAction()
                            {
                                Title =  await TranslateIfNeedAsync("More details"),
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q={cinema.Name}"
                            }
                        },
                        Tap = new CardAction()
                        {
                            Title = "",
                            Type = ActionTypes.ImBack,
                            Value = $"{cinema.Name}, {startTime} - {endTime}"
                        }
                    };
                    reply.Attachments.Add(heroCard.ToAttachment());
                }
                await needToTranslateProperty.SetAsync(stepContext.Context, false, cancellationToken);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = reply});
            }
            return await stepContext.NextAsync(bookingDetails.Show, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, 
            CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;
            string startTime;
            string cinemaName;

            if (stepContext.Result is Show show)
            {
                cinemaName = show.Cinema.Name;
                startTime = show.StartTime.ToString("HH:mm");
            }
            else
            {
                var showInfo = (string)stepContext.Result;
                string[] separator = { ", ", " - " };
                var showInfoArray = showInfo.Split(separator, 3, StringSplitOptions.RemoveEmptyEntries);
                cinemaName = showInfoArray[0];
                startTime = showInfoArray[1];
                bookingDetails.Show = cinemaService.GetMovieShowList(bookingDetails.Movie.Name.ToLower(), bookingDetails.MovieDate).
                    Where(s => s.Cinema.Name == cinemaName && s.StartTime.ToString("HH:mm") == startTime).FirstOrDefault();
            }
            await needToTranslateProperty.SetAsync(stepContext.Context, false, cancellationToken);

            var confirm = await TranslateIfNeedAsync("Please confirm, you are going to watch");
            var inCinema = await TranslateIfNeedAsync("in");
            var correct = await TranslateIfNeedAsync("Is everything correct?");

            var messageText = $"{confirm} \"{bookingDetails.Movie.Name}\" " +
                $"{getLocalizedDate(bookingDetails.MovieDate)} {startTime} " +
                $"{inCinema.ToLower()} {cinemaName}. {correct}";
            var promptMessage = MessageFactory.Text(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), 
                new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            return (bool)stepContext.Result ? await stepContext.EndDialogAsync(
                (BookingDetails)stepContext.Options, cancellationToken) :
                await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private string getLocalizedDate(string date)
        {
            var parsedMovieDate = Convert.ToDateTime(date, CultureInfo.InvariantCulture);
            return parsedMovieDate.ToString("dd.MM.yyyy");
        }

        private async Task<string> TranslateIfNeedAsync(string text)
        {
            return language == TranslationSettings.defaultLanguage ? text :
                (await translationService.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(text) }, language))[0];
        }
    }
}