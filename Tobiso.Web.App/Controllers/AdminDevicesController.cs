using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[Route("api/admin/devices")]
[ApiController]
[Authorize]
public class AdminDevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public AdminDevicesController(IDeviceService deviceService) => _deviceService = deviceService;

    [HttpPost]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        await _deviceService.RegisterOrUpdateAsync(request.FcmToken, request.DeviceName);
        return NoContent();
    }
}
