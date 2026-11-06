using System.Windows;
using System.Windows.Controls;
namespace MyPlaylist.Client.Views
{
    // 곡 추가 전 확인과 곡 정보 수정에 함께 사용한다.
    public partial class TrackEditDialog : UserControl, IDialog
    {
        private readonly TaskCompletionSource<bool> result = new();
        public Task<bool> Result => result.Task;
        public string MusicTitle => MusicTitleBox.Text.Trim();
        public string? Artist =>
        string.IsNullOrWhiteSpace(ArtistBox.Text) ? null : ArtistBox.Text.Trim();
        public TrackEditDialog(string title, string saveText,
        string musicTitle, string? artist, string videoId)
        {
            InitializeComponent();
            TitleLabel.Text = title;
            SaveButton.Content = saveText;
            MusicTitleBox.Text = musicTitle;
            ArtistBox.Text = artist;
            VideoIdBox.Text = videoId;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (MusicTitle.Length == 0)
            {
                ErrorLabel.Text = "곡 제목을 입력해 주세요.";
                ErrorLabel.Visibility = Visibility.Visible;
                return;
            }
            result.TrySetResult(true);
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            result.TrySetResult(false);
        }
    }
}
