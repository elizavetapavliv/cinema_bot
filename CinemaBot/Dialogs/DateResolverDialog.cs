using CinemaBot.Models;
using CinemaBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Dialogs
{
    public class DateResolverDialog : CancelAndHelpDialog
    {
        private const string promptMessageText = "When would you like to go to the cinema?";
        private const string notFullDateMessageText = "For your booking please enter" +
            " a full date including Day, Month and Year.";
        private const string movieIsNotShowingMessageText = "The movie is not going this day. Try to choose another date.";
        private const string dateIsNotRecognizedMessageText = "I can't identify the date. Please enter a full date including Day, " +
            "Month and Year.";

        private ICinemaService cinemaService;
        private string movie;
        private bool isMovieDateIncorrect;
        private bool isMovieDateUnrecognized;
        private bool isFirstAttempt;

        public DateResolverDialog(ICinemaService cinemaService) : base(nameof(DateResolverDialog))
        {
            this.cinemaService = cinemaService;
            AddDialog(new DateTimePrompt(nameof(DateTimePrompt), DateTimePromptValidator));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));
            
            InitialDialogId = nameof(WaterfallDialog);    
        }

        private async Task<DialogTurnResult> InitialStepAsync(
            WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (BookingDetails)stepContext.Options;
            var timex = bookingDetails.MovieDate;
            movie = bookingDetails.Movie.Name.ToLower();

            var promptMessage = MessageFactory.Text(promptMessageText);
            var dateIsIncorrectMessage = MessageFactory.Text(notFullDateMessageText);
            var repromptMessage = MessageFactory.Text("Try to choose another date.");

            if (timex == null)
            {
                isFirstAttempt = true;
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = promptMessage,
                        RetryPrompt = repromptMessage
                    }, cancellationToken);
            }
            bool needToRetry = false;
            isMovieDateIncorrect = false;
            isMovieDateUnrecognized = false;
            var timexProperty = new TimexProperty(timex);

            if (!timexProperty.Types.Contains(Constants.TimexTypes.Definite))
            {
                needToRetry = true;
                isFirstAttempt = true;
            }
            else if (!cinemaService.IsFilmShowOnDate(movie, timex))
            {
                needToRetry = true;
                dateIsIncorrectMessage.Text = movieIsNotShowingMessageText;
                isMovieDateIncorrect = true;
            }

            if (needToRetry)
            {
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                   new PromptOptions
                   {
                       Prompt = dateIsIncorrectMessage,
                       RetryPrompt = repromptMessage
                    }, 
                cancellationToken);
        }

            return await stepContext.NextAsync(new List<DateTimeResolution> { new DateTimeResolution { Timex = timex } }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = ((List<DateTimeResolution>)stepContext.Result)[0].Timex;
            return await stepContext.EndDialogAsync(timex, cancellationToken);
        }

        private async Task<bool> DateTimePromptValidator(
            PromptValidatorContext<IList<DateTimeResolution>> promptContext, 
            CancellationToken cancellationToken)
        {
            Activity message;
            if (promptContext.Recognized.Succeeded)
            {
                var timex = promptContext.Recognized.Value[0].Timex.Split('T')[0];
                var isDefinite = new TimexProperty(timex).Types.Contains(Constants.TimexTypes.Definite);
                if (isDefinite)
                {
                    if(!cinemaService.IsFilmShowOnDate(movie, timex))
                    {
                        isDefinite = false;
                        if (!isMovieDateIncorrect)
                        {
                            var messageText = movieIsNotShowingMessageText;
                            message = MessageFactory.Text(messageText, InputHints.IgnoringInput);
                            await promptContext.Context.SendActivityAsync(message, cancellationToken);
                            isMovieDateIncorrect = true;
                        }                 
                    }
                }
                else
                {
                    if (isMovieDateIncorrect || isMovieDateUnrecognized || isFirstAttempt)
                    {
                        isMovieDateIncorrect = false;
                        isMovieDateUnrecognized = false;
                        message = MessageFactory.Text(notFullDateMessageText, InputHints.IgnoringInput);
                        await promptContext.Context.SendActivityAsync(message, cancellationToken);
                    }               
                }
                isFirstAttempt = false;
                return isDefinite;
            }
            if (!isMovieDateUnrecognized)
            {
                message = MessageFactory.Text(dateIsNotRecognizedMessageText, InputHints.IgnoringInput);
                await promptContext.Context.SendActivityAsync(message, cancellationToken);
                isMovieDateUnrecognized = true;
            }
            isFirstAttempt = false;
            return false;
        }
    }
}