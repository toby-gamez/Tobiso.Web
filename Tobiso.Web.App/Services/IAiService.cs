using System.Threading;
using System.Threading.Tasks;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Services
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
        IAsyncEnumerable<string> AskStreamAsync(AiChatRequest request, CancellationToken cancellationToken = default);
        Task<string> ExplainSentenceAsync(string sentence, string articleContext);
        Task<EvaluateAnswerResponse> EvaluateAnswerAsync(EvaluateAnswerRequest request);
        Task<FlashcardResponse> GenerateFlashcardsAsync(int postId);
        Task<PracticeProblemResponse> GeneratePracticeProblemsAsync(int postId, int count, int? gradeId = null);
        Task<RewriteGradeResponse> RewriteForGradeAsync(int postId, int targetGrade);
        Task<RewriteGradeResponse> RewriteForRegisterAsync(int postId, string register);
        Task<RealWorldResponse> GetRealWorldApplicationsAsync(int postId);
        Task<SuggestRelatedResponse> SuggestRelatedPostsAsync(int postId);
        Task<WhatIfResponse> GetWhatIfScenarioAsync(WhatIfRequest request);
        Task<EvaluateComprehensionResponse> EvaluateComprehensionAsync(EvaluateComprehensionRequest request);
        Task<List<string>> GenerateFunFactsAsync(int postId);
        Task<List<ExamQuestion>> GenerateExamQuestionsAsync(int postId);
        Task<string> GenerateExamSummaryAsync(int postId);
        Task<FlashcardEligibilityBatchResult> ClassifyFlashcardEligibilityBatchAsync(int batchSize = 30);

        // Cache access for callers that generate/consume fun facts in-process (Blazor pages),
        // bypassing the HTTP AiController surface entirely - see AiService.TryGetCachedFunFactsAsync.
        Task<List<string>?> TryGetCachedFunFactsAsync(int postId);
        Task SaveFunFactsCacheAsync(int postId, List<string> facts);
    }
}
