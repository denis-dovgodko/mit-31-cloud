using Azure;
using lab5.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure.AI.TextAnalytics;
using Microsoft.IdentityModel.Tokens;

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

            var entities = new List<dynamic>();

            foreach (var entity in response.Value)
            {
                var matches = new List<dynamic>();
                foreach (var match in entity.Matches)
                {
                    matches.Add(new { Text = match.Text, Score = match.ConfidenceScore });
                }

                entities.Add(new
                {
                    Name = entity.Name,
                    Url = entity.Url,
                    DataSource = entity.DataSource,
                    Matches = matches
                });
            }

            ViewData["Entities"] = entities;
            return View();
        }
    }
}
