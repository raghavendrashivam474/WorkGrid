using System.ComponentModel.DataAnnotations;

namespace WorkGrid.Api.Models;

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password);
