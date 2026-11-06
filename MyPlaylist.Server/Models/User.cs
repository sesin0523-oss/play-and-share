using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    // 회원 API 담당 조원과 함께 사용하는 모델
    [Table("USER")]
    public class User
    {
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("user_name", TypeName = "varchar(50)")]
        public string UserName { get; set; } = "";
        [Column("user_pw", TypeName = "varchar(255)")]
        public string UserPw { get; set; } = "";
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        // 회원과 플레이리스트, 좋아요, 조회 기록, 댓글의 관계
        public List<Playlist> Playlists { get; set; } = new();
        public List<PlaylistLike> Likes { get; set; } = new();
        public List<PlaylistView> Views { get; set; } = new();
        public List<PlaylistComment> Comments { get; set; } = new();
    }
}
