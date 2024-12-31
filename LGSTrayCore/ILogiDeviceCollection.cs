using LGSTrayPrimitives;

namespace LGSTrayCore;

public interface ILogiDeviceCollection
{
    void OnDeviceMessage(IDeviceMessage message);
    void Clear();
}
