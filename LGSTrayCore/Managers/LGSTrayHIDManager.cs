using LGSTrayPrimitives;
using Microsoft.Extensions.Hosting;

namespace LGSTrayCore.Managers;

public class LGSTrayHIDManager : IDeviceManager, IHostedService
{
    private readonly ILogiDeviceCollection _deviceCollection;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public LGSTrayHIDManager(
        ILogiDeviceCollection deviceCollection,
        IHostApplicationLifetime applicationLifetime)
    {
        _deviceCollection = deviceCollection;
        _applicationLifetime = applicationLifetime;
    }

    public void RediscoverDevices()
    {
        // TODO: Implement device rediscovery
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
