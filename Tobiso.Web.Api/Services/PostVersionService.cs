using Microsoft.EntityFrameworkCore;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Api.Services;

public interface IPostVersionService
{
    Task<List<PostVersionResponse>> GetByPost(int postId);
    Task<PostVersionResponse?> Create(CreateVersionRequest req);
    Task<bool> Update(int id, UpdateVersionRequest req);
    Task<bool> UpdateGrade(int id, int gradeId);
    Task<bool> Delete(int id);
}

public class PostVersionService : IPostVersionService
{
    private readonly TobisoDbContext _context;

    public PostVersionService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<PostVersionResponse>> GetByPost(int postId)
    {
        return await _context.PostVersions
            .Where(v => v.PostId == postId)
            .Include(v => v.Grade)
            .OrderBy(v => v.Grade!.Level)
            .Select(v => new PostVersionResponse
            {
                Id = v.Id,
                PostId = v.PostId,
                GradeId = v.GradeId,
                GradeName = v.Grade != null ? v.Grade.Name : null,
                GradeLevel = v.Grade != null ? v.Grade.Level : (int?)null,
                Content = v.Content,
                LastFix = v.LastFix,
                LastEdit = v.LastEdit
            })
            .ToListAsync();
    }

    public async Task<PostVersionResponse?> Create(CreateVersionRequest req)
    {
        // Ensure the grade exists
        var grade = await _context.Grades.FindAsync(req.GradeId);
        if (grade == null) return null;

        // Enforce unique (PostId, GradeId)
        var exists = await _context.PostVersions
            .AnyAsync(v => v.PostId == req.PostId && v.GradeId == req.GradeId);
        if (exists) return null;

        var entity = new PostVersion
        {
            PostId = req.PostId,
            GradeId = req.GradeId,
            Content = req.Content,
            LastFix = req.LastFix,
            LastEdit = req.LastEdit
        };
        _context.PostVersions.Add(entity);
        await _context.SaveChangesAsync();

        return new PostVersionResponse
        {
            Id = entity.Id,
            PostId = entity.PostId,
            GradeId = entity.GradeId,
            GradeName = grade.Name,
            GradeLevel = grade.Level,
            Content = entity.Content,
            LastFix = entity.LastFix,
            LastEdit = entity.LastEdit
        };
    }

    public async Task<bool> Update(int id, UpdateVersionRequest req)
    {
        var entity = await _context.PostVersions.FindAsync(id);
        if (entity == null) return false;

        entity.Content = req.Content;
        entity.LastFix = req.LastFix;
        entity.LastEdit = req.LastEdit;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateGrade(int id, int gradeId)
    {
        var entity = await _context.PostVersions.FindAsync(id);
        if (entity == null) return false;

        var grade = await _context.Grades.FindAsync(gradeId);
        if (grade == null) return false;

        var conflict = await _context.PostVersions
            .AnyAsync(v => v.PostId == entity.PostId && v.GradeId == gradeId && v.Id != id);
        if (conflict) return false;

        entity.GradeId = gradeId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var entity = await _context.PostVersions.FindAsync(id);
        if (entity == null) return false;
        _context.PostVersions.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
