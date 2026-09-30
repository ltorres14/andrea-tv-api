using AndreaTV.Api.Modules.Devices.Models;

namespace AndreaTV.Api.Modules.Devices.Services;

public sealed class DeviceService : IDeviceService
{
    public DeviceStatus GetStatus() => new("Devices module is ready.");
}
