using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IExplanationService
{
    Task<List<ExplanationResponse>> GetByQuestionId(int questionId);
    Task<ExplanationResponse?> GetById(int id);
    Task<ExplanationResponse?> Create(CreateExplanationRequest request);
    Task<bool> Update(UpdateExplanationRequest request);
    Task<bool> Delete(int id);
}

public class ExplanationService : IExplanationService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<ExplanationService> _logger;

    public ExplanationService(TobisoDbContext context, ILogger<ExplanationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ExplanationResponse>> GetByQuestionId(int questionId)
    {
        try
        {
            return await _context.Explanations
                .AsNoTracking()
                .Where(e => e.QuestionId == questionId)
                .Select(e => new ExplanationResponse
                {
                    Id = e.Id,
                    Text = e.Text,
                    QuestionId = e.QuestionId
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání vysvětlení pro otázku {QuestionId}", questionId);
            throw;
        }
    }

    public async Task<ExplanationResponse?> GetById(int id)
    {
        var explanation = await _context.Explanations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (explanation == null) return null;

        return new ExplanationResponse
        {
            Id = explanation.Id,
            Text = explanation.Text,
            QuestionId = explanation.QuestionId
        };
    }

    public async Task<ExplanationResponse?> Create(CreateExplanationRequest request)
    {
        try
        {
            var explanation = new Explanation
            {
                Text = request.Text,
                QuestionId = request.QuestionId
            };

            _context.Explanations.Add(explanation);
            await _context.SaveChangesAsync();

            return await GetById(explanation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření vysvětlení");
            throw;
        }
    }

    public async Task<bool> Update(UpdateExplanationRequest request)
    {
        try
        {
            var explanation = await _context.Explanations
                .FirstOrDefaultAsync(e => e.Id == request.Id);

            if (explanation == null) return false;

            explanation.Text = request.Text;
            explanation.QuestionId = request.QuestionId;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci vysvětlení {Id}", request.Id);
            throw;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var explanation = await _context.Explanations
                .FirstOrDefaultAsync(e => e.Id == id);

            if (explanation == null) return false;

            _context.Explanations.Remove(explanation);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání vysvětlení {Id}", id);
            throw;
        }
    }
}
