using CinemaBot.CognitiveModels;
using Microsoft.Bot.Builder;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaBot.Services
{
    public interface IWarningsService
    {
        Task<bool> CheckForUnsupportedMovie(ITurnContext context, string movie, string language, 
            CancellationToken cancellationToken);
        Task<bool> CheckForUnsupportedCinema(ITurnContext context, TicketBooking luisResult, CancellationToken cancellationToken);
    }
}
