using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace dz_4
{
    public class RekognitionService
    {
        private readonly AmazonRekognitionClient _client;

        public RekognitionService(string awsAccessKey, string awsSecretKey)
        {
            _client = new AmazonRekognitionClient(
                awsAccessKey,
                awsSecretKey,
                RegionEndpoint.EUCentral1);
        }

        // 1. Аналіз зображення
        public async Task<List<Label>> AnalyzeAsync(byte[] imageBytes)
        {
            var request = new DetectLabelsRequest
            {
                Image = new Image
                {
                    Bytes = new MemoryStream(imageBytes)
                },
                MaxLabels = 10,
                MinConfidence = 75F
            };

            var response = await _client.DetectLabelsAsync(request);

            return response.Labels;
        }

        // 2. Розпізнавання тексту
        public async Task<List<TextDetection>> DetectTextAsync(byte[] imageBytes)
        {
            var request = new DetectTextRequest
            {
                Image = new Image
                {
                    Bytes = new MemoryStream(imageBytes)
                }
            };

            var response = await _client.DetectTextAsync(request);

            return response.TextDetections;
        }

        // 3. Модерація
        public async Task<List<ModerationLabel>> ModerateAsync(byte[] imageBytes)
        {
            var request = new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    Bytes = new MemoryStream(imageBytes)
                },
                MinConfidence = 75F
            };

            var response = await _client.DetectModerationLabelsAsync(request);

            return response.ModerationLabels;
        }

        // 4. Порівняння облич
        public async Task<CompareFacesResponse> CompareFacesAsync(
            byte[] sourceImageBytes,
            byte[] targetImageBytes)
        {
            var request = new CompareFacesRequest
            {
                SourceImage = new Image
                {
                    Bytes = new MemoryStream(sourceImageBytes)
                },

                TargetImage = new Image
                {
                    Bytes = new MemoryStream(targetImageBytes)
                },

                SimilarityThreshold = 0F
            };

            return await _client.CompareFacesAsync(request);
        }
    }
}