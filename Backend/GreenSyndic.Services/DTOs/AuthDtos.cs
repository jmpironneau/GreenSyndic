using System.ComponentModel.DataAnnotations;

namespace GreenSyndic.Services.DTOs;

public class RegisterRequest
{
    [Required] public string FirstName { get; set; } = default!;
    [Required] public string LastName { get; set; } = default!;
    [Required, EmailAddress] public string Email { get; set; } = default!;
    [Required, MinLength(8)] public string Password { get; set; } = default!;
    public string? Phone { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Role { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = default!;
    [Required] public string Password { get; set; } = default!;
}

public class AuthResponse
{
    public string Token { get; set; } = default!;
    public DateTime Expiration { get; set; }
    public UserInfo User { get; set; } = default!;
}

public class UserInfo
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public Guid? OrganizationId { get; set; }
    public string? Role { get; set; }
    public IList<string> Roles { get; set; } = [];
}
