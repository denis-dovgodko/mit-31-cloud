using Azure;
using lab5.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure.AI.TextAnalytics;
using Azure.AI.Translation.Text;
using Microsoft.IdentityModel.Tokens;
using Sprache;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace lab5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly string languageKey = Environment.GetEnvironmentVariable("LANGUAGE_KEY");
        private static readonly string languageEndpoint = Environment.GetEnvironmentVariable("LANGUAGE_ENDPOINT");
        private static readonly string translatorKey = Environment.GetEnvironmentVariable("TRANSLATOR_KEY");
        private static readonly string region = Environment.GetEnvironmentVariable("TRANSLATOR_REGION");
        private static readonly string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
        private static readonly AzureKeyCredential translatorCredentials = new AzureKeyCredential(translatorKey);
        private static readonly Uri endpoint = new Uri(languageEndpoint);
        private static readonly TextAnalyticsClient client = new TextAnalyticsClient(endpoint, credentials);
        private static readonly TextTranslationClient translationClient = new TextTranslationClient(translatorCredentials, region);

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var languages = await GetLanguagesAsync();
            ViewBag.Languages = languages;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult AnalyzeText(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return View();
            }

            var response = client.RecognizeLinkedEntities(userInput);
            return View(response.Value);
        }
        [HttpPost]
        public IActionResult RecognizeEntities(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return View();
            }

            var response = client.RecognizeEntities(userInput);
            return View(response.Value);
        }
        [HttpPost]
        public IActionResult PersonalDetect(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return View();
            }

            var response = client.RecognizePiiEntities(userInput);
            return View(response.Value);
        }
        [HttpPost]
        async public Task<IActionResult> HealthcareEntities(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return View();
            }
            List<string> batchInput = new List<string>()
            {
                userInput
            };

            var response = await client.StartAnalyzeHealthcareEntitiesAsync(batchInput);
            await response.WaitForCompletionAsync();
            List<AnalyzeHealthcareEntitiesResultCollection> data = new List<AnalyzeHealthcareEntitiesResultCollection>();
            await foreach (AnalyzeHealthcareEntitiesResultCollection documentsInPage in response.Value)
            {
                data.Add(documentsInPage);
            }
            return View(data);
        }
        private async Task<List<SelectListItem>> GetLanguagesAsync()
        {
            var client = new HttpClient();
            var requestUri = $"{translatorEndpoint}/languages?api-version=3.0&scope=translation";

            var response = await client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            var json = await JsonDocument.ParseAsync(responseStream);
            var languages = json.RootElement.GetProperty("translation");

            var result = new List<SelectListItem>();

            foreach (var lang in languages.EnumerateObject())
            {
                var name = lang.Value.GetProperty("name").GetString();
                result.Add(new SelectListItem { Text = name, Value = lang.Name });
            }

            return result.OrderBy(l => l.Text).ToList();
        }
        [HttpPost]
        async public Task<IActionResult> Translate(string targetLanguage, string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
            {
                return View();
            }
            Response<IReadOnlyList<TranslatedTextItem>> response = await translationClient.TranslateAsync(targetLanguage, userInput).ConfigureAwait(false);
            IReadOnlyList<TranslatedTextItem> translations = response.Value;
            TranslatedTextItem translation = translations.FirstOrDefault();
            return View(translation);
        }
    }
}
