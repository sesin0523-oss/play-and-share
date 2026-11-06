using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Auth;
using MyPlaylist.Server.Data;
using MyPlaylist.Server.Dtos;
using MyPlaylist.Server.Models;
using MySqlConnector;
namespace MyPlaylist.Server.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext db;
        private readonly IPasswordHasher<User> hasher;
        public UsersController(
        AppDbContext context,
        IPasswordHasher<User> passwordHasher)
        {
            db = context;
            hasher = passwordHasher;
        }
        // POST: api/users/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Signup(UserCredentialsRequest request)
        {
            string userName = request.UserName.Trim();
            // 중복 아이디 확인
            bool duplicated = await db.Users.AnyAsync(
            u => u.UserName == userName);
            if (duplicated)
                return Conflict("이미 사용 중인 아이디입니다.");
            var user = new User
            {
                UserName = userName,
                CreatedAt = DateTime.Now
            };
            user.UserPw = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync();
            }
            // 동시에 요청이 들어와 위의 확인을 통과한 경우 UNIQUE 제약조건이 막아 준다.
            catch (DbUpdateException ex) when (IsDuplicate(ex))
            {
                return Conflict("이미 사용 중인 아이디입니다.");
            }
            return Created($"/api/users/{user.UserId}", ToResponse(user));
        }
        // POST: api/users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserCredentialsRequest request)
        {
            string userName = request.UserName.Trim();
            var user = await db.Users.FirstOrDefaultAsync(
            u => u.UserName == userName);
            if (user == null || !VerifyPassword(user, request.Password))
                return Unauthorized("아이디 또는 비밀번호가 올바르지 않습니다.");
            // 다른 API는 NameIdentifier 클레임에서 회원 번호를 읽는다.
            var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()) },
            CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
            return Ok(ToResponse(user));
        }
        // POST: api/users/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return NoContent();
        }
        // GET: api/users/check-name?userName=alice
        [HttpGet("check-name")]
        public async Task<IActionResult> CheckName(
        [Required, StringLength(50, MinimumLength = 2)] string userName)
        {
            string name = userName.Trim();
            bool duplicated = await db.Users.AnyAsync(u => u.UserName == name);
            return Ok(new { available = !duplicated });
        }
        // GET: api/users/1
        [Authorize]
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetById(int userId)
        {
            // 본인 확인
            if (userId != User.GetUserId())
                return StatusCode(403, "본인의 정보만 조회할 수 있습니다.");
            var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound("회원을 찾을 수 없습니다.");
            return Ok(ToResponse(user));
        }
        // PUT: api/users/1
        [Authorize]
        [HttpPut("{userId:int}")]
        public async Task<IActionResult> Update(
        int userId, UserUpdateRequest request)
        {
            // 본인 확인
            if (userId != User.GetUserId())
                return StatusCode(403, "본인의 정보만 수정할 수 있습니다.");
            var user = await db.Users.FindAsync(userId);
            if (user == null)
                return NotFound("회원을 찾을 수 없습니다.");
            string userName = request.UserName.Trim();
            bool duplicated = await db.Users.AnyAsync(
            u => u.UserName == userName && u.UserId != userId);
            if (duplicated)
                return Conflict("이미 사용 중인 아이디입니다.");
            user.UserName = userName;
            if (request.NewPassword != null)
            {
                // 비밀번호 변경은 현재 비밀번호를 한 번 더 확인한다.
                if (request.CurrentPassword == null
                || !VerifyPassword(user, request.CurrentPassword))
                    return BadRequest("현재 비밀번호가 올바르지 않습니다.");
                user.UserPw = hasher.HashPassword(user, request.NewPassword);
            }
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicate(ex))
            {
                return Conflict("이미 사용 중인 아이디입니다.");
            }
            return NoContent();
        }
        // DELETE: api/users/1
        // 플레이리스트와 좋아요는 DB에서 함께 삭제되고, 조회 기록은 user_id만 NULL이 된다.
        [Authorize]
        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> Delete(int userId)
        {
            // 본인 확인
            if (userId != User.GetUserId())
                return StatusCode(403, "본인만 탈퇴할 수 있습니다.");
            var user = await db.Users.FindAsync(userId);
            if (user == null)
                return NotFound("회원을 찾을 수 없습니다.");
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            await HttpContext.SignOutAsync();
            return NoContent();
        }
        private bool VerifyPassword(User user, string password)
        {
            try
            {
                return hasher.VerifyHashedPassword(
                user, user.UserPw, password)
                != PasswordVerificationResult.Failed;
            }
            // 해시 형식이 아닌 값이 저장된 회원은 로그인할 수 없다.
            catch (FormatException)
            {
                return false;
            }
        }
        private static bool IsDuplicate(DbUpdateException ex)
        {
            return ex.InnerException is MySqlException
            {
                ErrorCode: MySqlErrorCode.DuplicateKeyEntry
            };
        }
        // 비밀번호는 응답에 넣지 않는다.
        private static object ToResponse(User user)
        {
            return new
            {
                user.UserId,
                user.UserName,
                user.CreatedAt
            };
        }
    }
}
