using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class TrackAddPage : UserControl, INavPage
    {
        private readonly int playlistId;
        private List<PlaylistTrack> tracks = new();
        public TrackAddPage(int id, string playlistTitle)
        {
            InitializeComponent();
            playlistId = id;
            TitleLabel.Text = $"{playlistTitle}에 곡 추가";
        }
        public Task OnShown(bool firstTime)
        {
            return LoadTracks();
        }
        private async Task LoadTracks()
        {
            tracks = await Nav.Api.GetTracks(playlistId);
            for (int i = 0; i < tracks.Count; i++)
                tracks[i].DisplayOrder = i + 1;
            TrackList.ItemsSource = null;
            TrackList.ItemsSource = tracks;
            EmptyLabel.Visibility = tracks.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        // 검색 1회에 일일 할당량 100을 쓰므로 버튼이나 Enter를 눌렀을 때만 검색한다.
        private async Task SearchYouTube()
        {
            string keyword = KeywordBox.Text.Trim();
            if (keyword.Length == 0)
                return;
            SearchButton.IsEnabled = false;
            try
            {
                await Nav.Run(async () =>
                {
                    ResultList.ItemsSource = await Nav.Api.SearchYouTube(keyword);
                });
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }
        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await SearchYouTube();
        }
        private async void KeywordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SearchYouTube();
        }
        private void KeywordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            KeywordHint.Visibility = KeywordBox.Text.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        private async void AddResult_Click(object sender, RoutedEventArgs e)
        {
            var result = (YouTubeResult)((FrameworkElement)sender).DataContext;
            var dialog = new TrackEditDialog("곡 추가", "추가",
            result.MusicTitle, result.Artist, result.YoutubeVideoId);
            if (!await Nav.ShowDialog(dialog))
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.AddTrack(playlistId, result.YoutubeVideoId,
                dialog.MusicTitle, dialog.Artist);
                await LoadTracks();
            });
        }
        private async void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlaylistTrack)((FrameworkElement)sender).DataContext;
            bool confirmed = await Nav.Confirm("곡을 삭제할까요?",
            $"'{track.MusicTitle}'을(를) 플레이리스트에서 삭제합니다.", "삭제", true);
            if (!confirmed)
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.DeleteTrack(playlistId, track.TrackId);
                await LoadTracks();
            });
        }
        // 손잡이를 누른 채 움직이면 끌기를 시작한다.
        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            var element = (FrameworkElement)sender;
            DragDrop.DoDragDrop(element, element.DataContext, DragDropEffects.Move);
        }
        private async void TrackRow_Drop(object sender, DragEventArgs e)
        {
            var dragged = e.Data.GetData(typeof(PlaylistTrack)) as PlaylistTrack;
            var target = ((FrameworkElement)sender).DataContext as PlaylistTrack;
            if (dragged == null || target == null || dragged == target)
                return;
            var newOrder = new List<PlaylistTrack>(tracks);
            newOrder.Remove(dragged);
            newOrder.Insert(tracks.IndexOf(target), dragged);
            await Nav.Action(async () =>
            {
                await Nav.Api.ChangeTrackOrder(
                playlistId, newOrder.Select(t => t.TrackId).ToList());
                await LoadTracks();
            });
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Nav.Back();
        }
    }
}
