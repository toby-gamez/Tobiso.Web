using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IGradeService
{
    Task<List<GradeResponse>> GetAll();
    Task SeedDefaultsAsync();
    Task<GradeResponse?> GetById(int id);
    Task<GradeResponse?> Create(CreateGradeRequest req);
    Task<bool> Update(int id, UpdateGradeRequest req);
    Task<bool> Delete(int id);
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

    public async Task<GradeResponse?> GetById(int id)
    {
        var g = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().FindAsync(id);
        if (g == null) return null;
        return new GradeResponse { Id = g.Id, Name = g.Name, Level = g.Level };
    }

    public async Task<GradeResponse?> Create(CreateGradeRequest req)
    {
        // enforce unique Level
        var exists = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().AnyAsync(x => x.Level == req.Level);
        if (exists) return null;

        var entity = new Tobiso.Web.Domain.Entities.Grade { Name = req.Name, Level = req.Level };
        _context.Set<Tobiso.Web.Domain.Entities.Grade>().Add(entity);
        await _context.SaveChangesAsync();
        return new GradeResponse { Id = entity.Id, Name = entity.Name, Level = entity.Level };
    }

    public async Task<bool> Update(int id, UpdateGradeRequest req)
    {
        var g = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().FindAsync(id);
        if (g == null) return false;

        // if level is changing, ensure uniqueness
        if (g.Level != req.Level)
        {
            var exists = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().AnyAsync(x => x.Level == req.Level && x.Id != id);
            if (exists) return false;
        }

        g.Name = req.Name;
        g.Level = req.Level;
        _context.Set<Tobiso.Web.Domain.Entities.Grade>().Update(g);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var g = await _context.Set<Tobiso.Web.Domain.Entities.Grade>().FindAsync(id);
        if (g == null) return false;

        // Check for PostVersions referencing this grade
        var used = await _context.Set<Tobiso.Web.Domain.Entities.PostVersion>().AnyAsync(pv => pv.GradeId == id);
        if (used) throw new InvalidOperationException("Grade has post versions and cannot be deleted.");

        _context.Set<Tobiso.Web.Domain.Entities.Grade>().Remove(g);
        await _context.SaveChangesAsync();
        return true;
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
