namespace CinemaBot.TranslationModels
{
    public class TranslatorRequest
    {
        public TranslatorRequest(string text)
        {
            Text = text;
        }
        public string Text { get; set; }
    }
}