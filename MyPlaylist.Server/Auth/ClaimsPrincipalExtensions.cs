using System.Security.Claims;
namespace MyPlaylist.Server.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        // 로그인한 회원 번호 (비로그인이면 null)
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            string? value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out int userId) ? userId : null;
        }
    }
}
