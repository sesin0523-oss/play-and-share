using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    // 메인, 검색, 마이페이지에서 함께 쓰는 플레이리스트 목록
    public partial class PlaylistList : UserControl
    {
        public PlaylistList()
        {
            InitializeComponent();
        }
        public void SetItems(List<PlaylistSummary> items, string emptyMessage)
        {
            Rows.ItemsSource = items;
            EmptyLabel.Text = emptyMessage;
            EmptyLabel.Visibility = items.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        private void Row_Click(object sender, RoutedEventArgs e)
        {
            var playlist = (PlaylistSummary)((FrameworkElement)sender).DataContext;
            Nav.Go(new DetailPage(playlist.PlaylistId));
        }
    }
}
