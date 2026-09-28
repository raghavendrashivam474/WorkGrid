namespace WorkGrid.Api.Models;

public sealed record LoginResponse(
    string Token,
    string Username,
    string Role);
