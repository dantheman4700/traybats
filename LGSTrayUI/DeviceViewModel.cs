using CommunityToolkit.Mvvm.ComponentModel;
using LGSTrayPrimitives;

namespace LGSTrayUI
{
    public partial class DeviceViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _batteryLevel;

        [ObservableProperty]
        private PowerSupplyStatus _status;

        [ObservableProperty]
        private double _batteryVoltage;
    }
} 