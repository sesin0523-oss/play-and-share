using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPlaylist.Server.Services;
namespace MyPlaylist.Server.Controllers
{
    [ApiController]
    [Route("api/youtube")]
    public class YouTubeController : ControllerBase
    {
        private readonly YouTubeSearchService youtube;
        private readonly ILogger<YouTubeController> logger;
        public YouTubeController(
        YouTubeSearchService service,
        ILogger<YouTubeController> log)
        {
            youtube = service;
            logger = log;
        }
        // GET: api/youtube/search?keyword=밤편지&maxResults=10
        // 검색 1회에 일일 할당량 100을 사용하므로 로그인한 회원만 호출할 수 있다.
        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> Search(
        [Required, StringLength(100)] string keyword,
        [Range(1, 25)] int maxResults = 10)
        {
            if (!youtube.IsConfigured)
                return StatusCode(503, "YouTube API 키가 설정되지 않았습니다.");
            try
            {
                return Ok(await youtube.SearchAsync(keyword.Trim(), maxResults));
            }
            catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                logger.LogWarning("YouTube API 호출 거부: {Status}", ex.StatusCode);
                return StatusCode(503, "YouTube API 사용 한도를 초과했거나 키 사용이 제한되었습니다.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning("YouTube API 호출 실패: {Status}", ex.StatusCode);
                return StatusCode(502, "YouTube 검색에 실패했습니다.");
            }
            catch (TaskCanceledException)
            {
                logger.LogWarning("YouTube API 응답 시간 초과");
                return StatusCode(504, "YouTube 응답 시간이 초과되었습니다.");
            }
        }
    }
}
