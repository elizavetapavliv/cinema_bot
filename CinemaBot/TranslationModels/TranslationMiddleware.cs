using CinemaBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.TranslationModels
{
    public class TranslationMiddleware : IMiddleware
    {
        private ITranslationService translator;
        private IStatePropertyAccessor<string> languageStateProperty;
        private IStatePropertyAccessor<bool> needToTranslateProperty;

        public TranslationMiddleware(ITranslationService translator, UserState userState)
        {
            this.translator = translator;
            languageStateProperty = userState.CreateProperty<string>("LanguagePreference");
            needToTranslateProperty = userState.CreateProperty<bool>("NeedToTranslate");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken)
        {
            if (await ShouldTranslateAsync(turnContext, cancellationToken))
            {
                if (turnContext.Activity.Type == ActivityTypes.Message)
                {
                    turnContext.Activity.Text = (await translator.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(turnContext.Activity.Text) },
                         TranslationSettings.defaultLanguage))[0];
                }
            }

            turnContext.OnSendActivities(async (newContext, activities, nextSend) =>
            {
                string userLanguage = await languageStateProperty.GetAsync(turnContext, () => TranslationSettings.defaultLanguage);

                if (userLanguage != TranslationSettings.defaultLanguage && needToTranslateProperty.GetAsync(turnContext, () => true).Result)
                {
                    List<Task> tasks = new List<Task>();
                    foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                    {
                        tasks.Add(TranslateMessageActivityAsync(currentActivity.AsMessageActivity(), userLanguage));
                    }

                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }

                return await nextSend();
            });

            await next(cancellationToken).ConfigureAwait(false);
        }

        private async Task TranslateMessageActivityAsync(IMessageActivity activity, string targetLocale)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                activity.Text = (await translator.TranslateAsync(new TranslatorRequest[] { new TranslatorRequest(activity.Text) },
                    targetLocale))[0];
            }
        }

        private async Task<bool> ShouldTranslateAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return await languageStateProperty.GetAsync(turnContext, () => TranslationSettings.defaultLanguage, cancellationToken) 
                != TranslationSettings.defaultLanguage && needToTranslateProperty.GetAsync(turnContext, () => true).Result;
        }
    }
}