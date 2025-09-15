using Microsoft.EntityFrameworkCore;
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

    public ExplanationService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<ExplanationResponse>> GetByQuestionId(int questionId)
    {
        try
        {
            var explanations = await _context.Explanations
                .Where(e => e.QuestionId == questionId)
                .ToListAsync();

            return explanations.Select(e => new ExplanationResponse
            {
                Id = e.Id,
                Text = e.Text,
                QuestionId = e.QuestionId
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání vysvětlení: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    public async Task<ExplanationResponse?> GetById(int id)
    {
        var explanation = await _context.Explanations
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
            Console.WriteLine($"Chyba při vytváření vysvětlení: {ex.Message}\n{ex.StackTrace}");
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
            Console.WriteLine($"Chyba při aktualizaci vysvětlení: {ex.Message}\n{ex.StackTrace}");
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
            Console.WriteLine($"Chyba při mazání vysvětlení: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
}