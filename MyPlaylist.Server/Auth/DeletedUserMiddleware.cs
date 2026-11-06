using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Data;
namespace MyPlaylist.Server.Auth
{
    // 탈퇴한 회원의 토큰이나 세션이 남아 있으면 비로그인 상태로 처리한다.
    public class DeletedUserMiddleware
    {
        private readonly RequestDelegate next;
        public DeletedUserMiddleware(RequestDelegate requestDelegate)
        {
            next = requestDelegate;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            int? userId = context.User.GetUserId();
            if (userId != null)
            {
                var db = context.RequestServices
                .GetRequiredService<AppDbContext>();
                bool exists = await db.Users
                .AnyAsync(u => u.UserId == userId);
                if (!exists)
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
            }
            await next(context);
        }
    }
}
