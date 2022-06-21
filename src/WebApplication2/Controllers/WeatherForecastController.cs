using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly HttpClient _client;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient client)
        {
            _logger = logger;
            _client = client;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Getting the weather forecast");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("GetLoad")]
        public string GetLoad()
        {

            // Get Traffic
            _logger.LogInformation("Making upstream requests");
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("https://www.google.com"));
            }
            Task.WaitAll(tasks.ToArray());

            // Get CPU
            double a = 10.1;
            for (int i = 0; i < 10000; i++)
            {
                Task.Run(() =>
                {
                    for (int j = 0; j < 100000; j++)
                    {
                        a = j / a;
                    }
                });
            }
            // Gc
            GC.Collect();

            return "done";
        }

        [HttpGet("Throw")]
        public string ThrowEx()
        {
            throw new Exception("Testing Exceptions");
        }
    }
}