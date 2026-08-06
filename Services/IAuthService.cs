using MyTasks.Dtos;

namespace MyTasks.Services
{
    public interface IAuthService
    {
        Task<UserReadDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshAsync(string rawRefreshToken);
        Task LogoutAsync(string rawRefreshToken);
    }
}