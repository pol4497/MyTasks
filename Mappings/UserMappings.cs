using MyTasks.Dtos;
using MyTasks.Models;

namespace MyTasks.Mappings
{
    public static class UserMappings
    {
        public static UserReadDto ToReadDto(this User user)
        {
            return new UserReadDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            };
        }
    }
}
