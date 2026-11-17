using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Controllers;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Tests;

public class PracticeControllerTests
{
    private sealed class FakeAttempts : IQuestionAttemptService
    {
        public List<(int User, int Question, bool Correct)> Recorded { get; } = [];
        public (int? User, List<int>? Categories, int Count, bool Unmastered, bool Flashcards)? LastSession { get; private set; }
        public List<int> Weak { get; set; } = [4, 5];

        public Task RecordAttemptAsync(int userId, int questionId, bool isCorrect)
        {
            Recorded.Add((userId, questionId, isCorrect));
            return Task.CompletedTask;
        }

        public Task<PracticeStatsDto> GetStatsAsync(int userId) =>
            Task.FromResult(new PracticeStatsDto { TotalQuestions = 10, AttemptedCount = 4 });

        public Task<List<int>> GetWeakCategoryIds(int userId, int take = 3) => Task.FromResult(Weak);

        public Task<List<QuestionResponse>> GetSessionAsync(int? userId, List<int>? categoryIds, int count, bool preferUnmastered, bool requireFlashcardEligible = false)
        {
            LastSession = (userId, categoryIds, count, preferUnmastered, requireFlashcardEligible);
            return Task.FromResult(new List<QuestionResponse> { new() { Id = 1, QuestionText = "Q" } });
        }
    }

    private sealed class FakeCategories : ICategoryService
    {
        public Task<List<int>> GetDescendantIds(int categoryId) => Task.FromResult(new List<int> { categoryId, categoryId * 10 });
        public Task<List<CategoryResponse>> GetAll() => throw new NotSupportedException();
        public Task<List<CategoryTreeResponse>> GetTree() => throw new NotSupportedException();
        public Task<List<CategoryResponse>> GetAncestors(int categoryId) => throw new NotSupportedException();
        public Task<CategoryResponse> Create(CategoryResponse category) => throw new NotSupportedException();
        public Task<CategoryResponse> Update(int id, CategoryResponse category) => throw new NotSupportedException();
        public Task Delete(int id) => throw new NotSupportedException();
    }

    private static PracticeController Controller(FakeAttempts attempts, string? role, int userId = 7)
    {
        var c = new PracticeController(attempts, new FakeCategories())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (role != null)
        {
            c.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim("role", role)], "test"));
        }
        return c;
    }

    [Fact]
    public async Task Flashcards_AnonymousGetsAnEligibleSessionWithTheCategorySubtree()
    {
        var attempts = new FakeAttempts();

        var result = await Controller(attempts, role: null).GetFlashcards(categoryId: 3, count: null);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal((null, new List<int> { 3, 30 }, 20, true, true), attempts.LastSession);
    }

    [Fact]
    public async Task Flashcards_StudentSessionIsPersonalised()
    {
        var attempts = new FakeAttempts();

        await Controller(attempts, role: "student", userId: 9).GetFlashcards(categoryId: null, count: 12);

        Assert.Equal((9, null, 12, true, true), attempts.LastSession);
    }

    [Fact]
    public async Task Recommended_UsesTheStudentsWeakCategories()
    {
        var attempts = new FakeAttempts();

        var result = await Controller(attempts, role: "student", userId: 9).GetRecommended(count: null);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal((9, new List<int> { 4, 5 }, 25, true, false), attempts.LastSession);
    }

    [Fact]
    public async Task Recommended_RejectsAnonymousCallers()
    {
        Assert.IsType<UnauthorizedResult>(await Controller(new FakeAttempts(), role: null).GetRecommended(null));
    }

    [Fact]
    public async Task Stats_ReturnsTheStudentsStats()
    {
        var result = await Controller(new FakeAttempts(), role: "student").GetStats();

        var stats = Assert.IsType<PracticeStatsDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(4, stats.AttemptedCount);
    }

    [Fact]
    public async Task Attempt_IsRecordedForTheCallingStudent()
    {
        var attempts = new FakeAttempts();

        var result = await Controller(attempts, role: "student", userId: 5).RecordAttempt(new PracticeAttemptRequest(42, true));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((5, 42, true), Assert.Single(attempts.Recorded));
    }

    [Fact]
    public async Task Attempt_RejectsAdminTokensAndBadQuestionIds()
    {
        var attempts = new FakeAttempts();

        Assert.IsType<UnauthorizedResult>(await Controller(attempts, role: "admin").RecordAttempt(new PracticeAttemptRequest(1, true)));
        Assert.IsType<BadRequestObjectResult>(await Controller(attempts, role: "student").RecordAttempt(new PracticeAttemptRequest(0, true)));
        Assert.Empty(attempts.Recorded);
    }
}
