using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    // 검색 결과, 또는 메인의 "더 보기"로 여는 전체 목록 (keyword가 null)
    public partial class SearchPage : UserControl, INavPage
    {
        private const int PageSize = 10;
        private readonly string? keyword;
        // 메인에서 넘어온 집계 기간과 태그 (검색창에서 열면 0, null)
        private readonly int days;
        private readonly string? tag;
        private string sort;
        private int page = 1;
        private int loadVersion;
        public SearchPage(string? searchKeyword, string startSort = "likes", int periodDays = 0,
        string? filterTag = null)
        {
            InitializeComponent();
            keyword = searchKeyword;
            sort = startSort;
            days = periodDays;
            tag = filterTag;
            string tagText = tag == null ? "" : $"#{tag} ";
            string periodText = days == 0 ? "" : $" (최근 {days}일)";
            TitleLabel.Text = keyword != null ? $"'{keyword}' 검색 결과"
            : $"{tagText}플레이리스트 전체{periodText}";
            // 처음 정렬 탭 선택 (IsLoaded 전이라 Tab_Checked에서 불러오지 않는다)
            (sort == "latest" ? LatestTab : sort == "views" ? ViewsTab : LikesTab).IsChecked = true;
        }
        public Task OnShown(bool firstTime)
        {
            return Load();
        }
        private async Task Load()
        {
            int version = ++loadVersion;
            int period = sort == "latest" ? 0 : days;
            var result = await Nav.Api.SearchPlaylists(keyword, sort, page, PageSize, 0, period, tag);
            await Nav.Api.FillThumbnails(result.Items);
            if (version != loadVersion)
                return;
            CountLabel.Text = keyword != null
            ? $"{result.TotalCount}개의 플레이리스트 · 제목, 아이디, 곡 제목, 가수로 찾았어요"
            : $"{result.TotalCount}개의 플레이리스트";
            // 전체 목록(더 보기)에서는 인기 순위 번호를 이어서 표시한다.
            if (keyword == null && sort != "latest")
            {
                for (int i = 0; i < result.Items.Count; i++)
                    result.Items[i].Rank = (page - 1) * PageSize + i + 1;
            }
            ResultList.SetItems(result.Items,
            keyword != null ? "검색 결과가 없습니다." : "플레이리스트가 없습니다.");
            int totalPages = Math.Max((result.TotalCount + PageSize - 1) / PageSize, 1);
            ResultPager.Set(page, totalPages);
        }
        private async void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;
            sort = sender == LatestTab ? "latest"
            : sender == ViewsTab ? "views" : "likes";
            page = 1;
            await Nav.Run(Load);
        }
        private async void ResultPager_PageChanged(int newPage)
        {
            page = newPage;
            await Nav.Run(Load);
        }
    }
}
