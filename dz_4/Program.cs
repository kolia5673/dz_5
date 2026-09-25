using Dz_4;
using System.IO;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var bot = new TelegramBotClient("tg_token");
var me = await bot.GetMe();

bot.OnMessage += NewMessageHandler;
Console.ReadLine();

async Task NewMessageHandler(Message message, UpdateType type)
{
    if (!string.IsNullOrWhiteSpace(message.Text) &&
        Uri.TryCreate(message.Text, UriKind.Absolute, out Uri? imageUrl) &&
        (imageUrl.Scheme == Uri.UriSchemeHttp ||
         imageUrl.Scheme == Uri.UriSchemeHttps))
    {
        await bot.SendMessage(
            message.Chat.Id,
            "Завантажую зображення за посиланням..."
        );

        using var httpClient = new HttpClient();

        var urlImageBytes = await httpClient.GetByteArrayAsync(imageUrl);

        Animal.ModelInput urlSampleData = new Animal.ModelInput()
        {
            ImageSource = urlImageBytes,
        };

        var urlScores =
            Animal.PredictAllLabels(urlSampleData);

        var urlBestResult = urlScores.First();

        var urlConfidence = urlBestResult.Value * 100;

        await bot.SendMessage(
            message.Chat.Id,
            $"Клас: {urlBestResult.Key}\n" +
            $"Впевненість: {urlConfidence:F2}%"
        );

        return;
    }
    if (message.Photo == null)
    {
        await bot.SendMessage(message.Chat.Id, "Надішліть фото для класифікації");
        return;
    }
    await bot.SendMessage(message.Chat.Id, "Надіслано фото");
    var photo = message.Photo.Last();
    var file = await bot.GetFile(photo.FileId);
    var filePath = Path.Combine(Path.GetTempPath(), $"{photo.FileId}.jpg");
    await using (var fileStream = new FileStream(filePath, FileMode.Create))
    {
        await bot.DownloadFile(file.FilePath!, fileStream);
    }

    ////////////////////////////// AutoML ///////////////////////////

    var imageBytes = File.ReadAllBytes(filePath);
    Animal.ModelInput sampleData = new Animal.ModelInput()
    {
        ImageSource = imageBytes,
    };
    var sortedScoresWithLabel = Animal.PredictAllLabels(sampleData);
    var bestResult = sortedScoresWithLabel.First();
    var confidence = bestResult.Value * 100;
    await bot.SendMessage(
        message.Chat.Id,
        $"Клас: {bestResult.Key}, Впевненість: {confidence:F2}%"
    );
}