using System.Text.Json.Nodes;

namespace Tobiso.Web.Shared.DTOs;

public record GameStateDto(int Revision, DateTime UpdatedAt, JsonNode State);

public record PutGameStateRequest(int BaseRevision, JsonNode? State);

public record GameStateSavedDto(int Revision, DateTime UpdatedAt);

public record GameStateConflictDto(int Revision, JsonNode State);

public record PointsClaimRequest(string Reason, string Ref);

/// <summary>Credits after the exchange and the remaining exchange-point balance.</summary>
public record PointsClaimResultDto(int Credits, int Points);

/// <summary>Server-owned exchange points: balance plus today's earnings against the daily cap.</summary>
public record PointsWalletDto(int Balance, int EarnedToday, int DailyEarnCap);

/// <summary>One graded answer: pick ids for choice questions, free text for single-answer (text) questions.</summary>
public record QuizAnswerSubmission(int QuestionId, List<int>? AnswerIds, string? Text);

public record PointsEarnRequest(List<QuizAnswerSubmission>? Answers);

public record PointsEarnResultDto(int Correct, int Awarded, int Balance, int EarnedToday, int DailyEarnCap);
