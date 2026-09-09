using System.ComponentModel.DataAnnotations;

namespace MyTasks.Dtos
{
    public record RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; init; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; init; } = string.Empty;
    }

    public record LoginDto
    {
        [Required]
        public string Username { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }

    public record RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; init; } = string.Empty;
    }

    public record AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; init; }
        public string RefreshToken { get; init; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; init; }
        public UserReadDto User { get; init; } = default!;
    }

    public record UserReadDto
    {
        public int Id { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
