using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace ShefaaHealthCare.Controllers
{
    public class AiController(HttpClient httpClient) : Controller
    {
        private readonly HttpClient _httpClient = httpClient;

        [HttpGet]
        public IActionResult Triage()
        {
            return View();
        }

        public class SymptomRequest
        {
            public string Symptoms { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeSymptoms([FromBody] SymptomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Symptoms))
                return BadRequest("يجب إدخال الأعراض أولاً.");

            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { symptoms = request.Symptoms }), 
                    System.Text.Encoding.UTF8, 
                    "application/json"
                );
                
                // Call the fast Python microservice
                var response = await _httpClient.PostAsync("http://127.0.0.1:8000/api/triage", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return Content(responseString, "application/json");
                }

                return StatusCode((int)response.StatusCode, "حدث خطأ أثناء الاتصال بالممرض الذكي.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ داخلي في الخادم: {ex.Message}");
            }
        }
    }
}
