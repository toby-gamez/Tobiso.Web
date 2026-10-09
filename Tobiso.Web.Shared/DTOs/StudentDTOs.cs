namespace Tobiso.Web.Shared.DTOs;

public record RegisterRequest(string Email, string? DisplayName, string Password, bool AcceptedTerms = false);

public record StudentProfileDto(string DisplayName, string Email, int Credits, string Role);

public record StudentLoginResponse(string Token, string DisplayName, int Credits);
