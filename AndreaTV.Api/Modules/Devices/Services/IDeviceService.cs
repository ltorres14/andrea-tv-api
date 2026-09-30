using AndreaTV.Api.Modules.Devices.Models;

namespace AndreaTV.Api.Modules.Devices.Services;

public interface IDeviceService
{
    DeviceStatus GetStatus();
}
