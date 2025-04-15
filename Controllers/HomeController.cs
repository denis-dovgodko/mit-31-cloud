using Azure;
using lab5.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure.AI.TextAnalytics;
using Azure.AI.Translation.Text;
using Azure.AI.Vision.ImageAnalysis;
using Azure.AI.Vision.Face;
using Microsoft.IdentityModel.Tokens;
using Sprache;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Text;
using Microsoft.Identity.Client;

namespace lab5.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly string languageKey = Environment.GetEnvironmentVariable("LANGUAGE_KEY");
        private static readonly string languageEndpoint = Environment.GetEnvironmentVariable("LANGUAGE_ENDPOINT");
        private static readonly string translatorKey = Environment.GetEnvironmentVariable("TRANSLATOR_KEY");
        private static readonly string region = Environment.GetEnvironmentVariable("TRANSLATOR_REGION");
        private static readonly string visionKey = Environment.GetEnvironmentVariable("VISION_KEY");
        private static readonly string faceKey = Environment.GetEnvironmentVariable("FACE_KEY");
        private readonly string TenantId = Environment.GetEnvironmentVariable("TENANT_ID");
        private readonly string ClientId = Environment.GetEnvironmentVariable("CLIENT_ID");
        private readonly string ClientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
        private readonly string Subdomain = Environment.GetEnvironmentVariable("SUBDOMAIN");
        private static readonly string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly string url = "https://raw.githubusercontent.com/Azure-Samples/cognitive-services-sample-data-files/master/Face/images/";
        private static readonly string LargePersonGroupId = Guid.NewGuid().ToString();
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
        private static readonly AzureKeyCredential translatorCredentials = new AzureKeyCredential(translatorKey);
        private static readonly AzureKeyCredential visionCredentials = new AzureKeyCredential(visionKey);
        private static readonly AzureKeyCredential faceCredentials = new AzureKeyCredential(faceKey);
        private static readonly Uri endpoint = new Uri(languageEndpoint);
        private static readonly Uri visionEndpoint = new Uri(Environment.GetEnvironmentVariable("VISION_ENDPOINT"));
        private static readonly Uri faceEndpoint = new Uri(Environment.GetEnvironmentVariable("FACE_ENDPOINT"));
        private static readonly TextAnalyticsClient client = new TextAnalyticsClient(endpoint, credentials);
        private static readonly TextTranslationClient translationClient = new TextTranslationClient(translatorCredentials, region);
        private static readonly ImageAnalysisClient visionClient = new ImageAnalysisClient(visionEndpoint, visionCredentials);
        private static readonly FaceClient faceClient = new FaceClient(faceEndpoint, faceCredentials);

        private IConfidentialClientApplication _confidentialClientApplication;
        private IConfidentialClientApplication ConfidentialClientApplication
        {
            get
            {
                if (_confidentialClientApplication == null)
                {
                    _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority($"https://login.windows.net/{TenantId}")
                    .Build();
                }

                return _confidentialClientApplication;
            }
        }


        public HomeController(ILogger<HomeController> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = logger;

            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new ArgumentNullException("TenantId is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new ArgumentNullException("ClientId is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                throw new ArgumentNullException("ClientSecret is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(Subdomain))
            {
                throw new ArgumentNullException("Subdomain is null! Did you add that info to secrets.json?");
            }
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
        [HttpPost]
        async public Task<IActionResult> OpticalCharactersRecognition(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return View();
            }
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            BinaryData data = BinaryData.FromBytes(memoryStream.ToArray());

            ImageAnalysisResult result = visionClient.Analyze(data, VisualFeatures.Read);
            ViewBag.OriginImage = memoryStream.ToArray();
            return View(result);
        }
        [HttpPost]
        async public Task<IActionResult> ImageAnalysis(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return View();
            }
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            BinaryData data = BinaryData.FromBytes(memoryStream.ToArray());

            ImageAnalysisResult result = visionClient.Analyze(data, VisualFeatures.Caption |
                    VisualFeatures.DenseCaptions |
                    VisualFeatures.Tags |
                    VisualFeatures.Objects |
                    VisualFeatures.SmartCrops |
                    VisualFeatures.People |
                    VisualFeatures.Read);
            ViewBag.OriginImage = memoryStream.ToArray();
            return View(result);
        }
        [HttpPost]
        async public Task<IActionResult> FaceService(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return View();
            }
            var requiredFaceAttributes = new FaceAttributeType[] {
                FaceAttributeType.Detection01.Blur,
                FaceAttributeType.Detection01.HeadPose,
                FaceAttributeType.Detection01.Accessories,
                FaceAttributeType.Detection01.Glasses,
                FaceAttributeType.Detection01.Exposure
            };
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            BinaryData data = BinaryData.FromBytes(memoryStream.ToArray());
            var response = await faceClient.DetectAsync(data, FaceDetectionModel.Detection01, FaceRecognitionModel.Recognition04, returnFaceId: false,
                returnFaceAttributes: requiredFaceAttributes, returnFaceLandmarks: true);
            IReadOnlyList<FaceDetectionResult> faces = response.Value;
            ViewBag.OriginImage = memoryStream.ToArray();
            return View(faces);
        }
        [HttpPost]
        async public Task<IActionResult> Code(IFormFile image)
        {

            string fileName = $"code_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            if (image == null || image.Length == 0)
            {
                return File("no characters", "text/plain", fileName);
            }
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            BinaryData data = BinaryData.FromBytes(memoryStream.ToArray());
            ImageAnalysisResult result = visionClient.Analyze(data, VisualFeatures.Read);
            var sb = new StringBuilder();
            if (result.Read.Blocks != null)
            {
                foreach (var block in result.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        sb.AppendLine(line.Text);
                    }
                }
            }
            var textBytes = Encoding.UTF8.GetBytes(sb.ToString());

            return File(textBytes, "text/plain", fileName);
        }
        public async Task<string> GetTokenAsync()
        {
            const string resource = "https://cognitiveservices.azure.com/";

            var authResult = await ConfidentialClientApplication.AcquireTokenForClient(
                new[] { $"{resource}/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            return authResult.AccessToken;
        }

        [HttpGet]
        async public Task<JsonResult> GetTokenAndSubdomain()
        {
            try
            {
                string tokenResult = await GetTokenAsync();

                return new JsonResult(new { token = tokenResult, subdomain = Subdomain });
            }
            catch (Exception e)
            {
                string message = "Unable to acquire Microsoft Entra token. Check the console for more information.";
                Debug.WriteLine(message, e);
                return new JsonResult(new { error = message });
            }
        }
    }
}
