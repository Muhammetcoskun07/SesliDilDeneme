using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SesliDil.Service.Services
{
    public class TtsService
    {
        private readonly HttpClient _httpClient;
        public TtsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var apiKey = configuration["OpenAI:ApiKey"];
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        }
        public async Task<byte[]> ConvertTextToSpeechAsync(string text, string voice = "alloy")
        {
            var request = new
            {
                model = "tts-1",
                voice = voice,
                input = text
            };

            using var response = await _httpClient.PostAsJsonAsync("audio/speech", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI TTS API error: {response.StatusCode}, Details: {errorContent}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        // Byte[]'ı mp3 olarak kaydeder ve URL döner
        public async Task<string> SaveAudioToFileAsync(byte[] audioBytes)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}.mp3";
            var filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(filePath, audioBytes);

            return $"/audio/{fileName}"; // Frontend’e yollamak için uygun URL
        }
    }
}
