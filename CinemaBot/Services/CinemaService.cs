using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CinemaBot.Models;
using CinemaBot.TranslationModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CinemaBot.Services
{
    public class CinemaService : ICinemaService
    {
        private HttpClient client;
        private ITranslationService translationService;
        private string eventsUri;
        private string orderTicketUri;
        public Dictionary<string, Dictionary<string, List<Show>>> MoviesDictionary { get; set; }
        public List<Movie> Movies { get; set; }

        private Dictionary<string, string> Cinemas { get; set; }

        public CinemaService(IConfiguration configuration, IHttpClientFactory clientFactory, ITranslationService translationService)
        {
            eventsUri = configuration["SilverScreenEventsUri"];
            orderTicketUri = configuration["SilverScreenOrderTicketUri"];
            client = clientFactory.CreateClient();
            this.translationService = translationService;
            Cinemas = new Dictionary<string, string>()
            {
                { "galileo", "Silver Screen cinemas в ТРЦ \"Galileo\""},
                { "dana", "VOKA CINEMA by Silver Screen в ТРЦ \"Dana Mall\""},
                { "arena", "Silver Screen cinemas в ТРЦ \"ArenaCity\""}
            };
        }

        public async Task LoadCurrentMoviesAsync(string language)
        {
            string request = await client.GetStringAsync(new Uri(eventsUri));
            Movies = JsonConvert.DeserializeObject<List<Movie>>(request).Where(x => x.ShowList != null).ToList();

            if (language == TranslationSettings.defaultLanguage)
            {
                var moviesNames = Movies.Select(movie => new TranslatorRequest(movie.Name)).ToArray();
                var translatedMoviesNames = await translationService.TranslateAsync(moviesNames, language);
                for (int i = 0; i < Movies.Count; i++)
                {
                    Movies[i].Name = translatedMoviesNames[i];
                }
                await Task.WhenAll(Movies.Select(movie => TranslateMovieCinemasInfo(language, movie)));
            }
            MoviesDictionary = Movies.ToDictionary(x => x.Name.ToLower(), x => x.ShowList);
        }

        private async Task TranslateMovieCinemasInfo(string language, Movie movie)
        {
            var showList = movie.ShowList.SelectMany(s => s.Value).ToList();
            var cinemaNames = showList.Select(show => new TranslatorRequest(show.Cinema.Name)).ToArray();
            var cinemaAddreses = showList.Select(show => new TranslatorRequest(show.Cinema.Address)).ToArray();

            var translatedCinemaNames = await translationService.TranslateAsync(cinemaNames, language);
            var translatedCinemaAddreses = await translationService.TranslateAsync(cinemaAddreses, language);

            int i = 0;
            foreach (var cinema in movie.ShowList)
            {
                cinema.Value.ForEach(show =>
                {
                    show.Cinema.Name = translatedCinemaNames[i];
                    show.Cinema.Address = translatedCinemaAddreses[i];
                });
                i++;
            }
        }

        public bool IsFilmShowOnDate(string movie, string date)
        {
            if (MoviesDictionary[movie] != null)
            {
                return MoviesDictionary[movie].ContainsKey(date);
            }
            return false;
        }

        public List<Show> GetMovieShowList(string movie, string date)
        {
            return MoviesDictionary[movie][date];
        }

        public string GetOrderTicketUri(string movieCode, string showId)
        {
            return $"{orderTicketUri}{movieCode}&showID={showId}";
        }

        public string GetRealCinemaName(string cinemaName)
        {
            if (cinemaName.ToLower().Contains("galileo"))
            {
                return Cinemas["galileo"];
            }
            if (cinemaName.ToLower().Contains("dana") || cinemaName.ToLower().Contains("voka"))
            {
                return Cinemas["dana"];
            }
            return Cinemas["arena"];
        }
    }
}