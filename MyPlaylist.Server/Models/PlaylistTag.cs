using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    // 플레이리스트의 분위기·장르 태그 (최대 3개, 사용자가 새로 만들 수도 있다)
    [Table("PLAYLIST_TAG")]
    public class PlaylistTag
    {
        [Key]
        [Column("tag_id")]
        public int TagId { get; set; }
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("tag_name", TypeName = "varchar(20)")]
        public string TagName { get; set; } = "";
        public Playlist? Playlist { get; set; }
    }
    public static class PlaylistTags
    {
        // 기본 추천 태그. 아직 아무도 쓰지 않았어도 고르기 목록에 보여 준다.
        public static readonly string[] Defaults =
        {
            "새벽 감성", "출근길", "집중할 때", "운동", "드라이브",
            "재즈", "비 오는 날", "힐링", "신나는", "잔잔한"
        };
        public const int MaxPerPlaylist = 3;
        public const int MaxLength = 20;
        // 한글·영문·숫자·공백만 허용한다. (앞의 #은 서버에서 떼어 낸다)
        public static readonly System.Text.RegularExpressions.Regex Allowed =
        new(@"^[\p{L}\p{N} ]+$");
        // 입력값 정리: 앞뒤 공백과 #을 떼고, 연속 공백은 하나로 줄인다.
        public static string Normalize(string tag)
        {
            string name = tag.Trim().TrimStart('#').Trim();
            return System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ");
        }
    }
}
