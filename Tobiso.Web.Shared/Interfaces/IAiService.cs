using Tobiso.Web.Shared.DTOs;
using System.Threading.Tasks;

namespace Tobiso.Web.Shared.Interfaces
{
    public interface IAiService
    {
        Task<AiChatResponse> AskAsync(AiChatRequest request, string clientKey);
        IAsyncEnumerable<string> AskStreamAsync(AiChatRequest request);
        Task<List<string>> DetectPeopleInTextAsync(string content);
        Task<string> AskRawJsonAsync(string systemPrompt, string userPrompt);
        Task<GrammarCheckResponse> CheckGrammarAsync(string content);
        Task<string> GenerateCheatSheetAsync(string title, string content, string ratio = "1x1");
        Task<List<CreateQuestionRequest>> GenerateQuestionsAsync(string content, int count, List<string> existingQuestions);
        Task<string> ExplainSentenceAsync(string sentence, string articleContext);
        Task<EvaluateAnswerResponse> EvaluateAnswerAsync(EvaluateAnswerRequest request);
        Task<FlashcardResponse> GenerateFlashcardsAsync(int postId);
        Task<PracticeProblemResponse> GeneratePracticeProblemsAsync(int postId, int count);
        Task<RewriteGradeResponse> RewriteForGradeAsync(int postId, int targetGrade);
        Task<RealWorldResponse> GetRealWorldApplicationsAsync(int postId);
        Task<SuggestRelatedResponse> SuggestRelatedPostsAsync(int postId);
        Task<WhatIfResponse> GetWhatIfScenarioAsync(int postId);
        Task<EvaluateComprehensionResponse> EvaluateComprehensionAsync(EvaluateComprehensionRequest request);
    }
}
