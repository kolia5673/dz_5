using Amazon.Rekognition;
using dz_4;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var recognitionService = new RekognitionService("YOUR_AWS_ACCESS_KEY","YOUR_AWS_SECRET_KEY");

var bot = new TelegramBotClient("YOUR_TELEGRAM_BOT_TOKEN");

var userModes = new Dictionary<long, string>();

// Зберігаємо перше фото для порівняння облич
var firstFaceImages = new Dictionary<long, byte[]>();

var me = await bot.GetMe();

bot.OnMessage += NewMessageHandler;

async Task NewMessageHandler(Message message, UpdateType type)
{
    var menuKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "🔍 Аналіз зображення", "📝 Розпізнавання тексту" },
        new KeyboardButton[] { "🛡 Модерація", "👤 Порівняння облич" }
    })
    {
        ResizeKeyboard = true
    };

    var backKeyboard = new ReplyKeyboardMarkup(new[]
    {
        new KeyboardButton[] { "↩️ Повернутися до меню" }
    })
    {
        ResizeKeyboard = true
    };

    // /start
    if (message.Text == "/start")
    {
        userModes.Remove(message.Chat.Id);
        firstFaceImages.Remove(message.Chat.Id);

        await bot.SendMessage(
            message.Chat.Id,
            "Вітаю! Оберіть режим роботи бота:",
            replyMarkup: menuKeyboard);

        return;
    }

    // Повернення до меню
    if (message.Text == "↩️ Повернутися до меню")
    {
        userModes.Remove(message.Chat.Id);
        firstFaceImages.Remove(message.Chat.Id);

        await bot.SendMessage(
            message.Chat.Id,
            "Оберіть режим роботи бота:",
            replyMarkup: menuKeyboard);

        return;
    }

    // Аналіз зображення
    if (message.Text == "🔍 Аналіз зображення")
    {
        userModes[message.Chat.Id] = "analyze";

        await bot.SendMessage(
            message.Chat.Id,
            "🔍 Аналіз зображення\nНадішліть фото.",
            replyMarkup: backKeyboard);

        return;
    }

    // Розпізнавання тексту
    if (message.Text == "📝 Розпізнавання тексту")
    {
        userModes[message.Chat.Id] = "text";

        await bot.SendMessage(
            message.Chat.Id,
            "📝 Розпізнавання тексту\nНадішліть фото.",
            replyMarkup: backKeyboard);

        return;
    }

    // Модерація
    if (message.Text == "🛡 Модерація")
    {
        userModes[message.Chat.Id] = "moderation";

        await bot.SendMessage(
            message.Chat.Id,
            "🛡 Модерація\nНадішліть фото.",
            replyMarkup: backKeyboard);

        return;
    }

    // Порівняння облич
    if (message.Text == "👤 Порівняння облич")
    {
        userModes[message.Chat.Id] = "faces";
        firstFaceImages.Remove(message.Chat.Id);

        await bot.SendMessage(
            message.Chat.Id,
            "👤 Порівняння облич\nНадішліть перше фото.",
            replyMarkup: backKeyboard);

        return;
    }

    // Якщо надіслано фото
    if (message.Photo != null)
    {
        if (!userModes.ContainsKey(message.Chat.Id))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Спочатку оберіть режим роботи бота.",
                replyMarkup: menuKeyboard);

            return;
        }

        var mode = userModes[message.Chat.Id];

        // Отримуємо фото з Telegram
        var photo = message.Photo.Last();

        var file = await bot.GetFile(photo.FileId);

        if (file.FilePath == null)
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Не вдалося отримати файл.");

            return;
        }

        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{photo.FileId}.jpg");

        await using (var fileStream = new FileStream(
            filePath,
            FileMode.Create))
        {
            await bot.DownloadFile(
                file.FilePath,
                fileStream);
        }

        var imageBytes = await File.ReadAllBytesAsync(filePath);

        // ==========================================
        // 1. АНАЛІЗ ЗОБРАЖЕННЯ
        // ==========================================

        if (mode == "analyze")
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Фото отримано. Починаю аналіз...");

            var labels = await recognitionService.AnalyzeAsync(imageBytes);

            if (labels.Count == 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Не вдалося знайти об'єкти на зображенні.");

                File.Delete(filePath);
                return;
            }

            await bot.SendMessage(
                message.Chat.Id,
                "Результати аналізу:");

            foreach (var label in labels)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"Мітка: {label.Name}\n" +
                    $"Впевненість: {label.Confidence:F2}%");
            }

            File.Delete(filePath);
            return;
        }

        // ==========================================
        // 2. РОЗПІЗНАВАННЯ ТЕКСТУ
        // ==========================================

        if (mode == "text")
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Фото отримано. Починаю розпізнавання тексту...");

            var textDetections =
                await recognitionService.DetectTextAsync(imageBytes);

            var lines = textDetections
                .Where(x => x.Type == TextTypes.LINE)
                .ToList();

            if (lines.Count == 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Текст на зображенні не знайдено.");

                File.Delete(filePath);
                return;
            }

            await bot.SendMessage(
                message.Chat.Id,
                "Розпізнаний текст:");

            foreach (var text in lines)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"Текст: {text.DetectedText}\n" +
                    $"Впевненість: {text.Confidence:F2}%");
            }

            File.Delete(filePath);
            return;
        }

        // ==========================================
        // 3. МОДЕРАЦІЯ
        // ==========================================

        if (mode == "moderation")
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Фото отримано. Починаю перевірку...");

            var moderationLabels =
                await recognitionService.ModerateAsync(imageBytes);

            if (moderationLabels.Count == 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Потенційно небажаний контент не знайдено.");

                File.Delete(filePath);
                return;
            }

            await bot.SendMessage(
                message.Chat.Id,
                "Результати модерації:");

            foreach (var label in moderationLabels)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"Категорія: {label.Name}\n" +
                    $"Впевненість: {label.Confidence:F2}%");
            }

            File.Delete(filePath);
            return;
        }

        // ==========================================
        // 4. ПОРІВНЯННЯ ОБЛИЧ
        // ==========================================

        if (mode == "faces")
        {
            // Перше фото
            if (!firstFaceImages.ContainsKey(message.Chat.Id))
            {
                firstFaceImages[message.Chat.Id] = imageBytes;

                await bot.SendMessage(
                    message.Chat.Id,
                    "Перше фото отримано.\n" +
                    "Тепер надішліть друге фото.",
                    replyMarkup: backKeyboard);

                File.Delete(filePath);
                return;
            }

            // Друге фото
            await bot.SendMessage(
                message.Chat.Id,
                "Друге фото отримано. Порівнюю обличчя...");

            var firstImage = firstFaceImages[message.Chat.Id];

            var compareResult =
                await recognitionService.CompareFacesAsync(
                    firstImage,
                    imageBytes);

            if (compareResult.FaceMatches.Count == 0)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "Схожих облич не знайдено.");

                firstFaceImages.Remove(message.Chat.Id);
                File.Delete(filePath);
                return;
            }

            foreach (var match in compareResult.FaceMatches)
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    $"Схожість облич: {match.Similarity:F2}%");
            }

            firstFaceImages.Remove(message.Chat.Id);

            await bot.SendMessage(
                message.Chat.Id,
                "Порівняння завершено. Для нового порівняння надішліть перше фото.",
                replyMarkup: backKeyboard);

            File.Delete(filePath);
            return;
        }
    }
}

Console.ReadLine();