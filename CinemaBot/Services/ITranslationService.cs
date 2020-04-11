using CinemaBot.TranslationModels;
using System.Threading.Tasks;

namespace CinemaBot.Services
{
    public interface ITranslationService
    {
        Task<string[]> TranslateAsync(TranslatorRequest[] request, string targetLocale);
    }
}