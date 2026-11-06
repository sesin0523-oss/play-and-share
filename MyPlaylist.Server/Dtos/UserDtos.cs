using System.ComponentModel.DataAnnotations;
namespace MyPlaylist.Server.Dtos
{
    // 회원가입 / 로그인 요청
    public class UserCredentialsRequest
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string UserName { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = "";
    }
    // 회원정보 수정 요청
    // 비밀번호를 바꿀 때만 currentPassword와 newPassword를 보낸다.
    public class UserUpdateRequest
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string UserName { get; set; } = "";
        public string? CurrentPassword { get; set; }
        [StringLength(100, MinimumLength = 8)]
        public string? NewPassword { get; set; }
    }
}
