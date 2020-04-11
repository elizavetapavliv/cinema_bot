using CinemaBot.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CinemaBot.Services
{
    public interface ICinemaService
    {
        Task LoadCurrentMoviesAsync(string language);
        List<Movie> Movies { get; set; }
        Dictionary<string, Dictionary<string, List<Show>>> MoviesDictionary { get; set; }
        List<Show> GetMovieShowList(string movie, string date);
        bool IsFilmShowOnDate(string movie, string date);
        string GetOrderTicketUri(string movieCode, string showId);
        string GetRealCinemaName(string cinemaName);
    }
}