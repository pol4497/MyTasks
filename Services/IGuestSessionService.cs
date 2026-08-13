using MyTasks.Dtos;

namespace MyTasks.Services
{
    public interface IGuestSessionService
    {
        Task<GuestSessionResponseDto> CreateGuestSessionAsync();
    }
}
