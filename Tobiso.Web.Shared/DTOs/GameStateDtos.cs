using System.Text.Json.Nodes;

namespace Tobiso.Web.Shared.DTOs;

public record GameStateDto(int Revision, DateTime UpdatedAt, JsonNode State);

public record PutGameStateRequest(int BaseRevision, JsonNode? State);

public record GameStateSavedDto(int Revision, DateTime UpdatedAt);

public record GameStateConflictDto(int Revision, JsonNode State);

public record PointsClaimRequest(string Reason, string Ref);

public record PointsClaimResultDto(int Credits, double Points, int Revision);
