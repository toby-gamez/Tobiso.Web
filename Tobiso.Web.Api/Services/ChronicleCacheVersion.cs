namespace Tobiso.Web.Api.Services;

/// <summary>
/// Global invalidation token for cached kronika axis layouts (see TobisoDbContext.SaveChangesAsync,
/// which bumps this whenever any Chronicle* entity changes, and ChronicleAxisService, which mixes
/// it into its IMemoryCache keys). Coarse (any kronika write invalidates every cached layout, not
/// just the affected axis) but simple and always correct — precise per-axis invalidation isn't
/// worth the bookkeeping for the write volume this feature sees (admin edits, not high-frequency writes).
/// </summary>
public static class ChronicleCacheVersion
{
    private static int _version;
    public static int Current => _version;
    public static void Bump() => Interlocked.Increment(ref _version);
}
