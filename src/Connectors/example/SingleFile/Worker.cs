using Microsoft.EntityFrameworkCore;
using Steeltoe.Connector.SqlServer.EFCore;
using System.Security.Cryptography;

namespace Steeltoe.Connector.Example;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ICMSComm _cmsComm;

    public Worker(ILogger<Worker> logger, ICMSComm cmsComm)
    {
        _logger = logger;
        _cmsComm = cmsComm;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation($"Worker running at: {DateTimeOffset.Now} {await _cmsComm.DbOnline()}");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
