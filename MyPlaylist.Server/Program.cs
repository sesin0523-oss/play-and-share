using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Auth;
using MyPlaylist.Server.Data;
using MyPlaylist.Server.Models;
using MyPlaylist.Server.Services;
var builder = WebApplication.CreateBuilder(args);
// Controller 등록
builder.Services.AddControllers();
// MySQL 연결 설정
var connectionString =
builder.Configuration.GetConnectionString(
"DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
    connectionString,
    ServerVersion.AutoDetect(connectionString)
    );
});
// YouTube 검색 서비스 등록
builder.Services.AddHttpClient<YouTubeSearchService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
// 쿠키 로그인 세션 등록
// API이므로 로그인 페이지로 이동시키지 않고 상태 코드만 돌려준다.
builder.Services
.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.Cookie.Name = "MyPlaylist.Session";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
// Swagger 등록
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
// 개발 환경 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<DeletedUserMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
