using LGSTrayPrimitives;

namespace LGSTrayHID;

public class HidppManagerContext
{
    private readonly Action<IDeviceMessage> _messageHandler;

    public HidppManagerContext(Action<IDeviceMessage> messageHandler)
    {
        _messageHandler = messageHandler;
    }

    public void SignalDeviceEvent(IDeviceMessage message)
    {
        _messageHandler(message);
    }
}
