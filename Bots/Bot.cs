using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Bots
{
    public class Bot<T> : ActivityHandler where T : Dialog
    {
        protected readonly Dialog dialog;
        protected readonly BotState conversationState;
        protected readonly BotState userState;
        protected readonly ILogger logger;
        private readonly IStatePropertyAccessor<string> languageStateProperty;

        private const string englishLanguage = "en";
        private const string russianLanguage = "ru";
        public Bot(ConversationState conversationState, UserState userState, T dialog, ILogger<Bot<T>> logger)
        {
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialog = dialog;
            this.logger = logger;
            languageStateProperty = userState.CreateProperty<string>("LanguagePreference");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var message = turnContext.Activity.Text;
            if (message == russianLanguage || message == englishLanguage)
            {
                var language = message == englishLanguage ? englishLanguage : russianLanguage;
                await languageStateProperty.SetAsync(turnContext, language, cancellationToken);

                var reply = MessageFactory.Text($"Your current language code is: {language}");
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }
            await dialog.RunAsync(turnContext, conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId == "webchat")
                return;

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
            }
        }
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);

            if (turnContext.Activity.ChannelId == "webchat" && turnContext.Activity.MembersAdded?.FirstOrDefault(m => m?.Name == "CinemaBotBelarus") != null)
            {
                await SendWelcomeMessageAsync(turnContext, cancellationToken);
            }
        }

        private async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text(
                      $"Hello and welcome to Cinema services!"),
                      cancellationToken);
            var reply = MessageFactory.Text("Choose your language:");
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction() { Title = "Русский", Type = ActionTypes.PostBack, Value = russianLanguage },
                            new CardAction() { Title = "English", Type = ActionTypes.PostBack, Value = englishLanguage },
                        },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}