using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class DetailPage : UserControl, INavPage
    {
        private readonly int playlistId;
        private PlaylistSummary playlist = new();
        private List<PlaylistTrack> tracks = new();
        private bool likedByMe;
        public int PlaylistId => playlistId;
        public DetailPage(int id)
        {
            InitializeComponent();
            playlistId = id;
            // 화면에 있는 동안만 재생 상태를 반영하고, 썸네일 칸을 영상 자리로 알려준다.
            Loaded += (_, _) =>
            {
                Player.Changed += RenderTracks;
                Player.SetSlot(HeroFrame, playlistId);
                RenderTracks();
            };
            Unloaded += (_, _) =>
            {
                Player.Changed -= RenderTracks;
                Player.ClearSlot(HeroFrame);
            };
        }
        public async Task OnShown(bool firstTime)
        {
            if (firstTime)
                await LoadDetail();
            else
                await Refresh();
        }
        // 상세 API는 호출할 때마다 클릭 수가 오르므로 화면을 처음 열 때만 호출한다.
        private async Task LoadDetail()
        {
            try
            {
                var detail = await Nav.Api.GetPlaylistDetail(playlistId);
                playlist = detail.Playlist;
                tracks = detail.Tracks;
                likedByMe = detail.LikedByMe;
                comments = await Nav.Api.GetComments(playlistId);
                Render();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                Nav.Replace(new PrivatePage());
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                await Nav.Alert(ex.Message);
                Nav.Back();
            }
        }
        // 곡 추가, 수정, 로그인 화면에서 돌아왔을 때 다시 불러온다.
        private async Task Refresh()
        {
            tracks = await Nav.Api.GetTracks(playlistId);
            playlist.TrackCount = tracks.Count;
            playlist.LikeCount = await Nav.Api.GetLikeCount(playlistId);
            likedByMe = Nav.CurrentUser != null
            && await Nav.Api.IsLiked(playlistId);
            comments = await Nav.Api.GetComments(playlistId);
            Render();
        }
        private void Render()
        {
            bool loggedIn = Nav.CurrentUser != null;
            bool isOwner = Nav.CurrentUser?.UserId == playlist.UserId;
            TitleLabel.Text = playlist.PlaylistTitle;
            DescriptionLabel.Text = playlist.Description;
            DescriptionLabel.Visibility = string.IsNullOrEmpty(playlist.Description)
            ? Visibility.Collapsed : Visibility.Visible;
            OwnerLabel.Text = $"{playlist.UserName} · {playlist.PublicText}";
            OwnerAvatar.UserName = playlist.UserName;
            DetailTags.ItemsSource = playlist.Tags;
            DetailTags.Visibility = playlist.TagsVisibility;
            InfoLabel.Text = $"{playlist.TrackCount}곡 · 클릭 {playlist.ViewCount}";
            LikeLabel.Text = $"좋아요 {playlist.LikeCount}";
            // 채운 하트 / 빈 하트
            HeartIcon.Text = likedByMe ? "" : "";
            // SetResourceReference: 테마를 바꾸면 색도 따라 바뀐다.
            string likeBrush = likedByMe ? "GoldBrush" : "TextBrush";
            HeartIcon.SetResourceReference(TextBlock.ForegroundProperty, likeBrush);
            LikeLabel.SetResourceReference(TextBlock.ForegroundProperty, likeBrush);
            LikeButton.IsHitTestVisible = loggedIn;
            LoginHint.Visibility = loggedIn ? Visibility.Collapsed : Visibility.Visible;
            RenderTracks();
            RenderComments();
            RenderTab();
            Thumb.SetLarge(HeroImage, tracks.FirstOrDefault()?.ThumbnailUrl);
        }
        // ===== 곡 목록 / 댓글 탭 =====
        private bool showComments;
        private List<PlaylistComment> comments = new();
        private void RenderTab()
        {
            bool isOwner = Nav.CurrentUser?.UserId == playlist.UserId;
            TrackScroll.Visibility = showComments ? Visibility.Collapsed : Visibility.Visible;
            CommentPanel.Visibility = showComments ? Visibility.Visible : Visibility.Collapsed;
            // 곡 추가, 수정, 삭제는 곡 목록 탭에서만 보여 준다.
            OwnerActions.Visibility = isOwner && !showComments
            ? Visibility.Visible : Visibility.Collapsed;
            TracksTabButton.SetResourceReference(Button.ForegroundProperty,
            showComments ? "SubTextBrush" : "TextBrush");
            CommentsTabButton.SetResourceReference(Button.ForegroundProperty,
            showComments ? "TextBrush" : "SubTextBrush");
        }
        private void TracksTab_Click(object sender, RoutedEventArgs e)
        {
            showComments = false;
            RenderTab();
        }
        private void CommentsTab_Click(object sender, RoutedEventArgs e)
        {
            showComments = true;
            RenderTab();
            CommentScroll.ScrollToEnd();
        }
        private void RenderComments()
        {
            int? me = Nav.CurrentUser?.UserId;
            bool isOwner = me != null && me == playlist.UserId;
            foreach (var comment in comments)
            {
                comment.DeleteVisibility = me != null && (comment.UserId == me || isOwner)
                ? Visibility.Visible : Visibility.Collapsed;
            }
            CommentList.ItemsSource = null;
            CommentList.ItemsSource = comments;
            CommentsTabLabel.Text = comments.Count == 0 ? "댓글" : $"댓글 {comments.Count}";
            CommentEmptyLabel.Visibility = comments.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
            bool loggedIn = me != null;
            CommentInput.Visibility = loggedIn ? Visibility.Visible : Visibility.Collapsed;
            CommentLoginHint.Visibility = loggedIn ? Visibility.Collapsed : Visibility.Visible;
        }
        private async void AddComment_Click(object sender, RoutedEventArgs e)
        {
            await SubmitComment();
        }
        // Enter: 등록, Shift+Enter: 줄바꿈
        private async void CommentBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                int caret = CommentBox.CaretIndex;
                CommentBox.Text = CommentBox.Text.Insert(caret, Environment.NewLine);
                CommentBox.CaretIndex = caret + Environment.NewLine.Length;
                e.Handled = true;
                return;
            }
            e.Handled = true;
            await SubmitComment();
        }
        private void CommentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CommentHint.Visibility = CommentBox.Text.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        private async Task SubmitComment()
        {
            string content = CommentBox.Text.Trim();
            if (content.Length == 0)
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.AddComment(playlistId, content);
                CommentBox.Clear();
                comments = await Nav.Api.GetComments(playlistId);
                RenderComments();
                CommentScroll.ScrollToEnd();
            });
        }
        private async void DeleteComment_Click(object sender, RoutedEventArgs e)
        {
            var comment = (PlaylistComment)((FrameworkElement)sender).DataContext;
            bool confirmed = await Nav.Confirm("댓글을 삭제할까요?",
            "삭제한 댓글은 되돌릴 수 없습니다.", "삭제", true);
            if (!confirmed)
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.DeleteComment(playlistId, comment.CommentId);
                comments = await Nav.Api.GetComments(playlistId);
                RenderComments();
            });
        }
        private void CommentLogin_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new LoginPage());
        }
        // 곡 목록만 다시 그린다. 재생 곡이 바뀔 때도 호출된다.
        private void RenderTracks()
        {
            bool isOwner = Nav.CurrentUser?.UserId == playlist.UserId;
            int? playingId = Player.PlaylistId == playlistId ? Player.Current?.TrackId : null;
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].DisplayOrder = i + 1;
                tracks[i].OwnerActionsVisibility = isOwner
                ? Visibility.Visible : Visibility.Collapsed;
                tracks[i].NowPlaying = tracks[i].TrackId == playingId;
            }
            TrackList.ItemsSource = null;
            TrackList.ItemsSource = tracks;
            EmptyLabel.Visibility = tracks.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
            PlayAllButton.Visibility = tracks.Count == 0
            ? Visibility.Collapsed : Visibility.Visible;
        }
        // 썸네일 칸을 16:9로 유지한다. (영상이 이 칸에 겹쳐 표시된다)
        private void HeroFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
                HeroFrame.Height = HeroFrame.ActualWidth * 9 / 16;
        }
        private async void PlayAll_Click(object sender, RoutedEventArgs e)
        {
            await Player.Play(playlistId, playlist.PlaylistTitle, tracks, 0);
        }
        private async void PlayTrack_Click(object sender, RoutedEventArgs e)
        {
            // 행 안의 버튼(YouTube에서 보기, 수정, 삭제)을 누른 경우는 재생하지 않는다.
            if (e.OriginalSource != sender)
                return;
            var track = (PlaylistTrack)((FrameworkElement)sender).DataContext;
            // 지금 재생 중인 곡을 다시 누르면 일시정지 / 재생을 바꾼다.
            if (track.NowPlaying)
            {
                Player.Toggle();
                return;
            }
            await Player.Play(playlistId, playlist.PlaylistTitle, tracks, tracks.IndexOf(track));
        }
        private async void Like_Click(object sender, RoutedEventArgs e)
        {
            await Nav.Action(async () =>
            {
                try
                {
                    if (likedByMe)
                        await Nav.Api.RemoveLike(playlistId);
                    else
                        await Nav.Api.AddLike(playlistId);
                    likedByMe = !likedByMe;
                }
                // 이미 누른 상태이면 오류 대신 누른 상태로 표시한다.
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    likedByMe = true;
                }
                // 화면을 갱신할 때 상세 API를 호출하지 않고 개수만 다시 가져온다.
                playlist.LikeCount = await Nav.Api.GetLikeCount(playlistId);
                Render();
            });
        }
        private void AddTrack_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new TrackAddPage(playlistId, playlist.PlaylistTitle));
        }
        private void EditPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new PlaylistFormPage(playlist));
        }
        private async void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = await Nav.Confirm(
            "플레이리스트를 삭제할까요?",
            "곡, 좋아요, 클릭 기록이 함께 삭제되며 되돌릴 수 없습니다.",
            "삭제", true);
            if (!confirmed)
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.DeletePlaylist(playlistId);
                Nav.Back();
            });
        }
        // ===== 드래그로 곡 순서 바꾸기 (플레이리스트 주인만) =====
        private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 행 버튼(재생)이 눌리지 않도록 여기서 처리를 끝낸다.
            e.Handled = true;
            var track = (PlaylistTrack)((FrameworkElement)sender).DataContext;
            DragDrop.DoDragDrop((DependencyObject)sender,
            new DataObject(typeof(PlaylistTrack), track), DragDropEffects.Move);
            DropLine.Visibility = Visibility.Collapsed;
        }
        private void TrackList_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PlaylistTrack)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            // 목록 위아래 끝에 가까우면 스크롤한다.
            double y = e.GetPosition(TrackScroll).Y;
            if (y < 40)
                TrackScroll.LineUp();
            else if (y > TrackScroll.ActualHeight - 40)
                TrackScroll.LineDown();
            // 놓일 자리에 골드 선을 표시한다.
            int index = GetDropIndex(e.GetPosition(TrackArea).Y, out double lineY);
            DropLine.Margin = new Thickness(0, Math.Max(0, lineY - 1), 0, 0);
            DropLine.Visibility = index < 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        private void TrackList_DragLeave(object sender, DragEventArgs e)
        {
            DropLine.Visibility = Visibility.Collapsed;
        }
        private async void TrackList_Drop(object sender, DragEventArgs e)
        {
            DropLine.Visibility = Visibility.Collapsed;
            if (e.Data.GetData(typeof(PlaylistTrack)) is not PlaylistTrack dragged)
                return;
            int from = tracks.IndexOf(dragged);
            int to = GetDropIndex(e.GetPosition(TrackArea).Y, out _);
            if (from < 0 || to < 0)
                return;
            // 자기 아래로 옮기면 빠진 자리만큼 한 칸 당겨진다.
            if (to > from)
                to--;
            if (to == from)
                return;
            // 화면을 먼저 바꾸고 서버에 저장한다. 실패하면 서버 순서로 되돌린다.
            tracks.RemoveAt(from);
            tracks.Insert(to, dragged);
            RenderTracks();
            await Nav.Action(async () =>
            {
                try
                {
                    await Nav.Api.ChangeTrackOrder(playlistId,
                    tracks.Select(t => t.TrackId).ToList());
                }
                catch (ApiException)
                {
                    await Refresh();
                    throw;
                }
            });
        }
        // 마우스 위치(y)에 곡을 놓았을 때 들어갈 순서. lineY는 표시할 선의 위치.
        private int GetDropIndex(double y, out double lineY)
        {
            lineY = 0;
            for (int i = 0; i < tracks.Count; i++)
            {
                if (TrackList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement row)
                    continue;
                double top = row.TranslatePoint(new Point(0, 0), TrackArea).Y;
                lineY = top + row.ActualHeight;
                if (y < top + row.ActualHeight / 2)
                {
                    lineY = top;
                    return i;
                }
            }
            return tracks.Count == 0 ? -1 : tracks.Count;
        }
        private void OpenYouTube_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlaylistTrack)((FrameworkElement)sender).DataContext;
            Nav.OpenYouTube(track.YoutubeUrl);
        }
        private async void EditTrack_Click(object sender, RoutedEventArgs e)
        {
            var track = (PlaylistTrack)((FrameworkElement)sender).DataContext;
            var dialog = new TrackEditDialog("곡 정보 수정", "저장",
            track.MusicTitle, track.Artist, track.YoutubeVideoId);
            if (!await Nav.ShowDialog(dialog))
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.UpdateTrack(playlistId, track.TrackId,
                track.YoutubeVideoId, dialog.MusicTitle, dialog.Artist);
                await Refresh();
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
                await Refresh();
            });
        }
    }
}
