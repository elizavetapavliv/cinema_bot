using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Dialogs
{
    public class CancelAndHelpDialog : ComponentDialog
    {
        private const string helpMessageText = "Try asking me to 'book a ticket'.";
        private const string cancelMessageText = "Bye bye!";

        public CancelAndHelpDialog(string id)
            : base(id)
        {}

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(
            DialogContext context, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(context, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(context, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext context, 
            CancellationToken cancellationToken)
        {
            if (context.Context.Activity.Type == ActivityTypes.Message)
            {
                var text = context.Context.Activity.Text.ToLowerInvariant();

                switch (text)
                {
                    case "help":
                    case "?":
                        var helpMessage = MessageFactory.Text(helpMessageText, helpMessageText,
                            InputHints.ExpectingInput);
                        await context.Context.SendActivityAsync(helpMessage,
                            cancellationToken);
                        return new DialogTurnResult(DialogTurnStatus.Waiting);

                    case "cancel":
                    case "exit":
                        var cancelMessage = MessageFactory.Text(cancelMessageText,
                            cancelMessageText, InputHints.IgnoringInput);
                        await context.Context.SendActivityAsync(cancelMessage,
                            cancellationToken);
                        return await context.CancelAllDialogsAsync(cancellationToken);
                }
            }
            return null;
        }
    }
}