using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTasks.Dtos;
using MyTasks.Services;

namespace MyTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class GuestController(IGuestSessionService _guestSessions) : ControllerBase
    {
        /// <summary>
        /// Issues a new guest session token. Send it back as the X-Guest-Token header on
        /// task requests to access that session's tasks without an account. If the caller
        /// later registers or logs in while presenting this header, any tasks created under
        /// it are transferred to that account and the guest session is invalidated.
        /// </summary>
        [HttpPost("session")]
        [ProducesResponseType(typeof(GuestSessionResponseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<GuestSessionResponseDto>> CreateSession()
        {
            var result = await _guestSessions.CreateGuestSessionAsync();
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
