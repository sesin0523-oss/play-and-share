using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    // 메인(둘러보기): 태그 필터, 오늘의 추천, 순위 상위 4개
    public partial class MainPage : UserControl, INavPage
    {
        private const int ListSize = 4;
        private string sort = "likes";
        // 집계 기간: 0 전체, 7 / 30 최근 N일 (최신순에는 쓰지 않는다)
        private int days;
        // 고른 태그 (null이면 전체)
        private string? tag;
        private int pickPlaylistId;
        // 탭을 빠르게 바꿨을 때 늦게 도착한 이전 응답을 무시한다.
        private int loadVersion;
        public MainPage()
        {
            InitializeComponent();
            LikesTab.IsChecked = true;
            PeriodBox.SelectedIndex = 0;
            DiscoverLatest.IsChecked = true;
        }
        public async Task OnShown(bool firstTime)
        {
            // 다른 화면에서 새 태그가 생겼을 수 있으므로 돌아올 때마다 목록을 새로 받는다.
            await LoadTagOptions();
            await Task.WhenAll(Load(), LoadDiscover());
            // 목록을 채우는 동안 화면이 아래로 밀려 내려갈 수 있으므로 처음 열 때는 맨 위에서 시작한다.
            if (firstTime)
                PageScroll.ScrollToTop();
        }
        // ===== 취향을 더 넓혀보세요 =====
        private const int DiscoverSize = 3;
        private string discoverSort = "latest";
        private int discoverVersion;
        // 상세 화면을 열면 "열어본 플레이리스트"가 되므로, 돌아올 때마다 다시 받아 다른 추천으로 바뀐다.
        private async Task LoadDiscover()
        {
            int version = ++discoverVersion;
            var items = await Nav.Api.GetDiscover(discoverSort, DiscoverSize);
            await Nav.Api.FillThumbnails(items.Select(i => i.Playlist));
            if (version != discoverVersion)
                return;
            bool loggedIn = Nav.CurrentUser != null;
            DiscoverHint.Text = loggedIn
            ? "아직 열어보지 않은 플레이리스트예요. 골드 태그는 처음 만나는 분위기예요."
            : "로그인하면 내가 열어본 기록을 바탕으로 새로운 취향을 추천해 드려요.";
            DiscoverList.ItemsSource = items;
            DiscoverEmpty.Text = loggedIn
            ? "공개된 플레이리스트를 모두 둘러보셨어요. 새 플레이리스트가 올라오면 여기에 보여 드릴게요."
            : "아직 공개된 플레이리스트가 없습니다.";
            DiscoverEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        private async void DiscoverSort_Checked(object sender, RoutedEventArgs e)
        {
            discoverSort = sender == DiscoverLikes ? "likes" : "latest";
            if (IsLoaded)
                await Nav.Run(LoadDiscover);
        }
        private void Discover_Click(object sender, RoutedEventArgs e)
        {
            var item = (DiscoverItem)((FrameworkElement)sender).DataContext;
            Nav.Go(new DetailPage(item.Playlist.PlaylistId));
        }
        private bool fillingTags;
        // "전체" + 태그 목록(많이 쓰인 순)으로 콤보박스를 채우고, 고른 태그는 그대로 둔다.
        private async Task LoadTagOptions()
        {
            var tags = await Nav.Api.GetTags();
            fillingTags = true;
            TagFilterBox.Items.Clear();
            TagFilterBox.Items.Add(new ComboBoxItem { Content = "전체", Tag = null });
            foreach (string name in tags)
                TagFilterBox.Items.Add(new ComboBoxItem { Content = $"#{name}", Tag = name });
            TagFilterBox.SelectedItem = TagFilterBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => (string?)i.Tag == tag) ?? TagFilterBox.Items[0];
            fillingTags = false;
        }
        private async Task Load()
        {
            int version = ++loadVersion;
            int period = sort == "latest" ? 0 : days;
            PeriodBox.Visibility = sort == "latest" ? Visibility.Hidden : Visibility.Visible;
            var statsTask = Nav.Api.GetTotalStatistics();
            var pickTask = Nav.Api.GetTodayPick();
            var listTask = Nav.Api.SearchPlaylists(null, sort, 1, ListSize, 0, period, tag);
            var stats = await statsTask;
            var pick = await pickTask;
            var list = await listTask;
            var withThumbs = list.Items.ToList();
            if (pick != null)
                withThumbs.Add(pick);
            await Nav.Api.FillThumbnails(withThumbs);
            if (version != loadVersion)
                return;
            StatsLabel.Text = $"서로의 플레이리스트에서 새로운 취향을 발견해요 · 회원 {stats.UserCount} · "
            + $"플레이리스트 {stats.PlaylistCount} · 총 클릭 {stats.TotalViewCount} · 총 좋아요 {stats.TotalLikeCount}";
            ShowPick(pick);
            // 제목과 더 보기 문구는 정렬에 맞춘다.
            string tagText = tag == null ? "" : $"#{tag} ";
            RankHeading.Text = sort == "latest" ? $"{tagText}새로 올라온 플레이리스트"
            : $"{tagText}지금 인기 있는 플레이리스트";
            MoreLabel.Text = sort == "latest" ? "새 플레이리스트 더 보기" : "인기 플레이리스트 더 보기";
            for (int i = 0; i < list.Items.Count; i++)
            {
                // 최신순은 인기 순위가 아니므로 번호를 표시하지 않는다.
                if (sort != "latest")
                    list.Items[i].Rank = i + 1;
            }
            string emptyText = tag != null && period == 0 ? $"'{tag}' 태그가 붙은 플레이리스트가 아직 없습니다."
            : period == 0 ? "아직 공개된 플레이리스트가 없습니다."
            : sort == "likes" ? $"최근 {period}일 동안 좋아요를 받은 플레이리스트가 없습니다."
            : $"최근 {period}일 동안 클릭된 플레이리스트가 없습니다.";
            RankList.SetItems(list.Items, list.TotalCount == 0 ? emptyText : "");
            MoreButton.Visibility = list.TotalCount > ListSize ? Visibility.Visible : Visibility.Collapsed;
        }
        private void ShowPick(PlaylistSummary? playlist)
        {
            PickButton.Visibility = playlist == null ? Visibility.Hidden : Visibility.Visible;
            PickEmptyLabel.Visibility = playlist == null ? Visibility.Visible : Visibility.Collapsed;
            if (playlist == null)
                return;
            pickPlaylistId = playlist.PlaylistId;
            PickTitle.Text = playlist.PlaylistTitle;
            PickDescription.Text = playlist.Description;
            PickDescription.Visibility = string.IsNullOrWhiteSpace(playlist.Description)
            ? Visibility.Collapsed : Visibility.Visible;
            PickTags.ItemsSource = playlist.Tags;
            PickTags.Visibility = playlist.TagsVisibility;
            PickAvatar.UserName = playlist.UserName;
            PickOwner.Text = playlist.UserName;
            PickInfo.Text = $"{playlist.TrackCount}곡 · 좋아요 {playlist.LikeCount}";
            Thumb.SetLarge(PickImage, playlist.ThumbnailUrl);
        }
        private async void TagFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fillingTags || TagFilterBox.SelectedItem is not ComboBoxItem item)
                return;
            tag = (string?)item.Tag;
            if (IsLoaded)
                await Nav.Run(Load);
        }
        private async void Tab_Checked(object sender, RoutedEventArgs e)
        {
            // 처음 화면을 만들 때는 OnShown에서 불러온다.
            if (!IsLoaded)
                return;
            sort = sender == LatestTab ? "latest"
            : sender == ViewsTab ? "views" : "likes";
            await Nav.Run(Load);
        }
        private async void PeriodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodBox.SelectedItem is not ComboBoxItem item)
                return;
            days = int.Parse((string)item.Tag);
            if (IsLoaded)
                await Nav.Run(Load);
        }
        // 지금 보고 있는 정렬, 기간, 태그 그대로 전체 목록 화면을 연다.
        private void More_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new SearchPage(null, sort, sort == "latest" ? 0 : days, tag));
        }
        private void Pick_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new DetailPage(pickPlaylistId));
        }
    }
}
