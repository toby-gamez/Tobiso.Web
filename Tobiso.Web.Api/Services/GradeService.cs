using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IGradeService
{
    Task<List<GradeResponse>> GetAll();
    Task SeedDefaultsAsync();
}

public class GradeService : IGradeService
{
    private readonly TobisoDbContext _context;

    public GradeService(TobisoDbContext context)
    {
        _context = context;
    }

    public async Task<List<GradeResponse>> GetAll()
    {
        return await _context.Set<Tobiso.Web.Domain.Entities.Grade>()
            .OrderBy(g => g.Level)
            .Select(g => new GradeResponse { Id = g.Id, Name = g.Name, Level = g.Level })
            .ToListAsync();
    }

    public async Task SeedDefaultsAsync()
    {
        var grades = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().ToListAsync();
        if (grades.Any()) return; // already seeded

        var defaults = new[] {
            new Tobiso.Web.Domain.Entities.Grade { Name = "6. ročník", Level = 6 },
            new Tobiso.Web.Domain.Entities.Grade { Name = "7. ročník", Level = 7 },
            new Tobiso.Web.Domain.Entities.Grade { Name = "8. ročník", Level = 8 },
            new Tobiso.Web.Domain.Entities.Grade { Name = "9. ročník", Level = 9 },
        };
        _context.Set<Tobiso.Web.Domain.Entities.Grade>().AddRange(defaults);
        await _context.SaveChangesAsync();
    }
}
