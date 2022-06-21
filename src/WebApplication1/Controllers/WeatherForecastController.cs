using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
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

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Making upstream requests");
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_client.GetAsync("https://www.google.com"));
            }
            Task.WaitAll(tasks.ToArray());

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpGet("cpu")]
        public double GetCPU()
        {
            var tasks = new List<Task>();
            double a = 10.1 ;
            for (int i = 0; i < 10000; i++)
            {
                Task.Run(() =>
                {
                    for (int j = 0; j < 100000; j++)
                    {
                        a = j/a;
                    }
                });
            }
            return double.IsNaN(a)?0:1;
        }
        [HttpGet("GC")]
        public void GC()
        {
            System.GC.Collect();
        }
        [HttpGet("Exceptions")]
        public void Exceptions()
        {
            throw new SystemException("Testing");
        }
    }
}
