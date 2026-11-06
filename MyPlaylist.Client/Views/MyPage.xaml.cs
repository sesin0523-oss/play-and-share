using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class MyPage : UserControl, INavPage
    {
        private int loadVersion;
        // showLibrary: 상단의 "내 보관함"으로 열면 좋아요한 플레이리스트 탭부터 보여 준다.
        public MyPage(bool showLibrary = false)
        {
            InitializeComponent();
            (showLibrary ? LikedTab : MineTab).IsChecked = true;
        }
        public void ShowLibrary()
        {
            LikedTab.IsChecked = true;
        }
        public Task OnShown(bool firstTime)
        {
            return Load();
        }
        private async Task Load()
        {
            int version = ++loadVersion;
            bool showLiked = LikedTab.IsChecked == true;
            bool showRecent = RecentTab.IsChecked == true;
            var stats = await Nav.Api.GetMyStatistics();
            var list = showRecent ? await Nav.Api.GetRecentPlaylists()
            : showLiked ? await Nav.Api.GetLikedPlaylists()
            : await Nav.Api.GetMyPlaylists();
            await Nav.Api.FillThumbnails(list);
            if (version != loadVersion)
                return;
            UserNameLabel.Text = Nav.CurrentUser?.UserName;
            StatsLabel.Text = $"내 플레이리스트 {stats.PlaylistCount} · 등록한 곡 {stats.TrackCount} · "
            + $"누른 좋아요 {stats.GivenLikeCount} · 받은 좋아요 {stats.ReceivedLikeCount} · "
            + $"받은 클릭 {stats.TotalViewCount}";
            // 내 플레이리스트: 공개 여부 / 좋아요, 최근 본: 만든 사람 표시
            bool mine = !showLiked && !showRecent;
            foreach (var playlist in list)
            {
                playlist.ShowOwner = !mine;
                playlist.ShowPublic = mine;
                playlist.ShowHeart = showLiked;
            }
            MyList.SetItems(list, showRecent ? "아직 열어본 플레이리스트가 없습니다."
            : showLiked ? "좋아요한 플레이리스트가 없습니다."
            : "아직 만든 플레이리스트가 없습니다.");
        }
        private async void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                await Nav.Run(Load);
        }
        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new UserEditPage());
        }
        private async void Logout_Click(object sender, RoutedEventArgs e)
        {
            await Nav.Action(async () =>
            {
                await Nav.Api.Logout();
                Nav.SetUser(null);
                Nav.Home();
            });
        }
    }
}
