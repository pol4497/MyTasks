using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTasks.Contexts;
using MyTasks.Dtos;
using MyTasks.Mappings;
using MyTasks.Repositories;
using MyTasks.Services;

namespace MyTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        IAuthService _auth,
        IUserRepository _users,
        ITaskOwnerContext _ownerContext) : ControllerBase
    {
        /// <summary>
        /// Registers a new user account.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserReadDto>> Register([FromBody] RegisterDto dto)
        {
            var user = await _auth.RegisterAsync(dto);
            return StatusCode(StatusCodes.Status201Created, user);
        }

        /// <summary>
        /// Logs a user in and issues an access token + refresh token pair.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access token + refresh token pair.
        /// The old refresh token is revoked (rotation).
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
        {
            var result = await _auth.RefreshAsync(dto.RefreshToken);
            return Ok(result);
        }

        /// <summary>
        /// Revokes a refresh token, ending that login session.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto)
        {
            await _auth.LogoutAsync(dto.RefreshToken);
            return NoContent();
        }

        /// <summary>
        /// Returns the profile of the currently authenticated user.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserReadDto>> GetCurrentUser()
        {
            if (!_ownerContext.UserId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _users.GetByIdAsync(_ownerContext.UserId.Value);
            if (user == null) return NotFound();

            return Ok(user.ToReadDto());
        }

        /// <summary>
        /// Lists all registered users. Admin-only.
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<UserReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetAllUsers()
        {
            var users = await _users.GetAllUsersAsync();
            return Ok(users.Select(u => u.ToReadDto()));
        }
    }
}