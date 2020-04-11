using System.Linq;

namespace CinemaBot.CognitiveModels
{
    public partial class TicketBooking
    {
        public string MovieEntity
        {
            get
            {
                return Entities?._instance?.Movie?.FirstOrDefault()?.Text;
            }
        }

        public (string Cinema, string AvailableCinema) CinemaEntities
        {
            get
            {
                var cinema = Entities?._instance?.Cinema?.FirstOrDefault()?.Text;
                var availableCinema = Entities?.Cinema?.FirstOrDefault()?.
                    AvailableCinema?.FirstOrDefault()?.FirstOrDefault();
                return (cinema, availableCinema);
            }
        }

        public string MovieDate
            => Entities.datetime?.FirstOrDefault()?.
            Expressions.FirstOrDefault()?.Split('T')[0];
    }
}
