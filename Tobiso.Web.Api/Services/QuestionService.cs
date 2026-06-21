using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Shared.DTOs;
using Tobiso.Web.Domain.Entities;

namespace Tobiso.Web.Api.Services;

public interface IQuestionService
{
    Task<List<QuestionResponse>> GetAll();
    Task<List<QuestionResponse>> GetByPostId(int postId);
    Task<QuestionResponse?> GetById(int id);
    Task<QuestionResponse?> Create(CreateQuestionRequest request);
    Task<bool> Update(UpdateQuestionRequest request);
    Task<bool> Delete(int id);
}

public class QuestionService : IQuestionService
{
    private readonly TobisoDbContext _context;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(TobisoDbContext context, ILogger<QuestionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<QuestionResponse>> GetAll()
    {
        try
        {
            var questions = await _context.Questions
                .AsNoTracking()
                .Include(q => q.Answers)
                .Include(q => q.Explanations)
                .Include(q => q.Post)
                .ToListAsync();

            return questions.Select(q => new QuestionResponse
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                PostId = q.PostId,
                Answers = q.Answers.Select(a => new AnswerResponse
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    Correct = a.Correct,
                    QuestionId = a.QuestionId
                }).ToList(),
                Explanations = q.Explanations.Select(e => new ExplanationResponse
                {
                    Id = e.Id,
                    Text = e.Text,
                    QuestionId = e.QuestionId
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání všech otázek");
            throw;
        }
    }

    public async Task<List<QuestionResponse>> GetByPostId(int postId)
    {
        try
        {
            var questions = await _context.Questions
                .AsNoTracking()
                .Include(q => q.Answers)
                .Include(q => q.Explanations)
                .Where(q => q.PostId == postId)
                .ToListAsync();

            return questions.Select(q => new QuestionResponse
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                PostId = q.PostId,
                Answers = q.Answers.Select(a => new AnswerResponse
                {
                    Id = a.Id,
                    AnswerText = a.AnswerText,
                    Correct = a.Correct,
                    QuestionId = a.QuestionId
                }).ToList(),
                Explanations = q.Explanations.Select(e => new ExplanationResponse
                {
                    Id = e.Id,
                    Text = e.Text,
                    QuestionId = e.QuestionId
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při načítání otázek pro post {PostId}", postId);
            throw;
        }
    }

    public async Task<QuestionResponse?> GetById(int id)
    {
        var question = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Answers)
            .Include(q => q.Explanations)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return null;

        return new QuestionResponse
        {
            Id = question.Id,
            QuestionText = question.QuestionText,
            PostId = question.PostId,
            Answers = question.Answers.Select(a => new AnswerResponse
            {
                Id = a.Id,
                AnswerText = a.AnswerText,
                Correct = a.Correct,
                QuestionId = a.QuestionId
            }).ToList(),
            Explanations = question.Explanations.Select(e => new ExplanationResponse
            {
                Id = e.Id,
                Text = e.Text,
                QuestionId = e.QuestionId
            }).ToList()
        };
    }

    public async Task<QuestionResponse?> Create(CreateQuestionRequest request)
    {
        try
        {
            var question = new Question
            {
                QuestionText = request.QuestionText,
                PostId = request.PostId,
                Answers = request.Answers.Select(a => new Answer
                {
                    AnswerText = a.AnswerText,
                    Correct = a.Correct
                }).ToList(),
                Explanations = request.Explanations.Select(e => new Explanation
                {
                    Text = e.Text
                }).ToList()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return await GetById(question.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při vytváření otázky");
            throw;
        }
    }

    public async Task<bool> Update(UpdateQuestionRequest request)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Explanations)
                .FirstOrDefaultAsync(q => q.Id == request.Id);

            if (question == null) return false;

            question.QuestionText = request.QuestionText;

            _context.Answers.RemoveRange(question.Answers);
            _context.Explanations.RemoveRange(question.Explanations);

            if (request.Answers.Any())
            {
                var answers = request.Answers.Select(a => new Answer
                {
                    AnswerText = a.AnswerText,
                    Correct = a.Correct,
                    QuestionId = question.Id
                }).ToList();

                _context.Answers.AddRange(answers);
            }

            if (request.Explanations.Any())
            {
                var explanations = request.Explanations.Select(e => new Explanation
                {
                    Text = e.Text,
                    QuestionId = question.Id
                }).ToList();

                _context.Explanations.AddRange(explanations);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při aktualizaci otázky {QuestionId}", request.Id);
            throw;
        }
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Explanations)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return false;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba při mazání otázky {QuestionId}", id);
            throw;
        }
    }
}
