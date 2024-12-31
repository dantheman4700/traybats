using LGSTrayPrimitives;
using Microsoft.Extensions.Hosting;

namespace LGSTrayHID;

public class HidppManagerService : IHostedService
{
    private readonly HidppManagerContext _context;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public HidppManagerService(
        HidppManagerContext context,
        IHostApplicationLifetime applicationLifetime)
    {
        _context = context;
        _applicationLifetime = applicationLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
