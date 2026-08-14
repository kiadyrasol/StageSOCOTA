using System.Text;
using System.Text.Json;

namespace GestionProjetSocota.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Clé API Gemini manquante");
        }

        public async Task<string> GenererCompteRendu(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={_apiKey}";

            var corpsRequete = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(corpsRequete);
            var contenu = new StringContent(json, Encoding.UTF8, "application/json");

            var reponse = await _httpClient.PostAsync(url, contenu);
            reponse.EnsureSuccessStatusCode();

            var reponseJson = await reponse.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(reponseJson);

            var texte = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return texte ?? "Aucune réponse générée.";
        }
    }
}