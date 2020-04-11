using CinemaBot.TranslationModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CinemaBot.Services
{
    public class TranslationService : ITranslationService
    {
        private HttpClient client;
        private string translatorTextUri;
        private string translatorTextKey;

        public TranslationService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            translatorTextUri = configuration["TranslatorTextAPIUri"];
            translatorTextKey = configuration["TranslatorKey"];
            client = clientFactory.CreateClient();
        }

        public async Task<string[]> TranslateAsync(TranslatorRequest[] request, string targetLocale)
        {
            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            content.Headers.Add("Ocp-Apim-Subscription-Key", translatorTextKey);

            var response = await client.PostAsync(new Uri(translatorTextUri + targetLocale), content);
            var result = JsonConvert.DeserializeObject<TranslatorResponse[]>(await response.Content.ReadAsStringAsync());

            return result.Select(translatorResponse => translatorResponse.Translations.First().Text).ToArray();
        }
    }
}