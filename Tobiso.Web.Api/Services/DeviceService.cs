using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IDeviceService
{
    Task RegisterOrUpdateAsync(string fcmToken, string deviceName);
}

public class DeviceService : IDeviceService
{
    private readonly TobisoDbContext _context;

    public DeviceService(TobisoDbContext context) => _context = context;

    public async Task RegisterOrUpdateAsync(string fcmToken, string deviceName)
    {
        var existing = await _context.DeviceTokens.FirstOrDefaultAsync(d => d.FcmToken == fcmToken);
        if (existing != null)
        {
            existing.DeviceName = deviceName;
            existing.LastSeenAt = DateTime.UtcNow;
        }
        else
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                FcmToken     = fcmToken,
                DeviceName   = deviceName,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt   = DateTime.UtcNow,
            });
        }
        await _context.SaveChangesAsync();
    }
}
