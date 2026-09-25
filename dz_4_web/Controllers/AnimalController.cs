using Dz_4;
using Microsoft.AspNetCore.Mvc;

namespace Dz_4_Web.Controllers
{
    public class AnimalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile? image, string? imageUrl)
        {
            byte[] imageBytes;

            if (image != null && image.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }
            else if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                using var httpClient = new HttpClient();
                imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            }
            else
            {
                ViewBag.Error = "Оберіть зображення або введіть посилання.";
                return View();
            }

            Animal.ModelInput sampleData = new Animal.ModelInput()
            {
                ImageSource = imageBytes
            };

            var sortedScoresWithLabel = Animal.PredictAllLabels(sampleData);
            var bestResult = sortedScoresWithLabel.First();
            var confidence = bestResult.Value * 100;

            ViewBag.Result = bestResult.Key;
            ViewBag.Confidence = confidence;

            return View();
        }
    }
}