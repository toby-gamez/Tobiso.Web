using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IAddendumService
{
    Task<List<AddendumResponse>> GetAll();
    Task<AddendumResponse?> GetById(int id);
    Task<bool> Update(AddendumResponse addendum);
    Task<bool> Delete(int id);
    Task<AddendumResponse?> Create(AddendumResponse addendum);
}

public class AddendumService : IAddendumService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<AddendumService> _logger;

    public AddendumService(TobisoDbContext context, ILogger<AddendumService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AddendumResponse>> GetAll()
    {
        try
        {
            var addendums = await _context.Addendums.ToListAsync();
            return addendums.Select(a => new AddendumResponse
            {
                Id = a.Id,
                Name = a.Name,
                Content = a.Content,
                UpdatedAt = a.UpdatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání dodatků");
            throw;
        }
    }

    public async Task<AddendumResponse?> GetById(int id)
    {
        try
        {
            var addendum = await _context.Addendums.FindAsync(id);
            if (addendum == null) return null;

            return new AddendumResponse
            {
                Id = addendum.Id,
                Name = addendum.Name,
                Content = addendum.Content,
                UpdatedAt = addendum.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání dodatku {Id}", id);
            throw;
        }
    }

    public async Task<AddendumResponse?> Create(AddendumResponse addendum)
    {
        try
        {
            var newAddendum = new Addendum
            {
                Name = addendum.Name,
                Content = addendum.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Addendums.Add(newAddendum);
            await _context.SaveChangesAsync();

            return new AddendumResponse
            {
                Id = newAddendum.Id,
                Name = newAddendum.Name,
                Content = newAddendum.Content,
                UpdatedAt = newAddendum.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření dodatku");
            throw;
        }
    }

    public async Task<bool> Update(AddendumResponse addendum)
    {
        try
        {
            var existingAddendum = await _context.Addendums.FindAsync(addendum.Id);
            if (existingAddendum == null) return false;

            existingAddendum.Name = addendum.Name;
            existingAddendum.Content = addendum.Content;
            existingAddendum.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci dodatku {Id}", addendum.Id);
            throw;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var addendum = await _context.Addendums.FindAsync(id);
            if (addendum == null) return false;

            _context.Addendums.Remove(addendum);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání dodatku {Id}", id);
            throw;
        }
    }
}
