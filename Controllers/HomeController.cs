using Azure;
using lab5.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure.AI.TextAnalytics;
using Microsoft.IdentityModel.Tokens;
using Sprache;

namespace lab5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly string languageKey = Environment.GetEnvironmentVariable("LANGUAGE_KEY");
        private static readonly string languageEndpoint = Environment.GetEnvironmentVariable("LANGUAGE_ENDPOINT");
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
        private static readonly Uri endpoint = new Uri(languageEndpoint);
        private static readonly TextAnalyticsClient client = new TextAnalyticsClient(endpoint, credentials);

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
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
    }
}
