using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MyPlaylist.Client.Services;
using MyPlaylist.Client.Views;
namespace MyPlaylist.Client
{
    // 확인 창과 입력 창의 공통 형식
    public interface IDialog
    {
        // true: 확인, false: 취소
        Task<bool> Result { get; }
    }
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int size);
        public UserControl? CurrentPage => PageHost.Content as UserControl;
        public MainWindow()
        {
            // 저장된 테마를 화면을 만들기 전에 적용해서 깜빡이지 않게 한다.
            Theme.Load();
            InitializeComponent();
            Theme.Changed += ApplyTheme;
            UpdateThemeButton();
            SetVideoHidden(false);
            Player.Attach(VideoView);
            Player.Changed += UpdatePlayerBar;
            Player.TimeChanged += UpdateProgress;
            Player.FullScreenChanged += ApplyFullScreen;
            Player.Started += () => SetVideoHidden(false);
            // 마지막으로 듣던 곡을 하단 바에 띄우고, 앱을 닫을 때 저장한다.
            Player.RestoreSession();
            Closing += (_, _) => Player.SaveSession();
            // 전체화면에서 앱 쪽에 포커스가 있어도 Esc로 빠져나올 수 있게 한다.
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape && Player.IsFullScreen)
                {
                    Player.ExitFullScreen();
                    e.Handled = true;
                }
            };
            LayoutUpdated += (_, _) => UpdateVideoPosition();
            Nav.Start(this);
        }
        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            ApplyTitleBar();
        }
        // 제목 표시줄도 테마에 맞춘다. (20: DWMWA_USE_IMMERSIVE_DARK_MODE)
        private void ApplyTitleBar()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
                return;
            int dark = Theme.IsDark ? 1 : 0;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        }
        private void ApplyTheme()
        {
            ApplyTitleBar();
            UpdateThemeButton();
        }
        // 지금과 반대 테마의 아이콘을 보여준다. (어두울 때 해, 밝을 때 달)
        private void UpdateThemeButton()
        {
            ThemeButton.Content = Theme.IsDark ? "" : "";
            ThemeButton.ToolTip = Theme.IsDark ? "밝은 테마로 바꾸기" : "어두운 테마로 바꾸기";
            AutomationProperties.SetName(ThemeButton, (string)ThemeButton.ToolTip);
        }
        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            Theme.Toggle();
        }
        public void ShowPage(UserControl page)
        {
            PageHost.Content = page;
            UpdateTopBar();
        }
        public void UpdateTopBar()
        {
            bool logoOnly = CurrentPage is INavPage { LogoOnly: true };
            var user = Nav.CurrentUser;
            SearchArea.Visibility = logoOnly ? Visibility.Hidden : Visibility.Visible;
            NavArea.Visibility = SearchArea.Visibility;
            // 지금 화면에 맞춰 둘러보기 / 내 보관함 밑줄을 표시한다.
            BrowseNav.IsChecked = CurrentPage is MainPage;
            LibraryNav.IsChecked = CurrentPage is MyPage;
            GuestMenu.Visibility = !logoOnly && user == null
            ? Visibility.Visible : Visibility.Collapsed;
            UserMenu.Visibility = !logoOnly && user != null
            ? Visibility.Visible : Visibility.Collapsed;
            UserNameButton.Content = user?.UserName;
        }
        public async Task<bool> ShowDialog(IDialog dialog)
        {
            DialogBox.Child = (UIElement)dialog;
            DialogLayer.Visibility = Visibility.Visible;
            bool result = await dialog.Result;
            DialogLayer.Visibility = Visibility.Collapsed;
            DialogBox.Child = null;
            return result;
        }
        private void Logo_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            Nav.Home();
        }
        private void BrowseNav_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            Nav.Home();
        }
        // 내 보관함: 좋아요한 플레이리스트 탭으로 마이페이지를 연다. (비로그인은 로그인 화면)
        private void LibraryNav_Click(object sender, RoutedEventArgs e)
        {
            if (Nav.CurrentUser == null)
            {
                UpdateTopBar();
                Nav.Go(new LoginPage());
                return;
            }
            if (CurrentPage is MyPage page)
                page.ShowLibrary();
            else
                Nav.Go(new MyPage(showLibrary: true));
        }
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new LoginPage());
        }
        private void Signup_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new SignupPage());
        }
        private void MyPage_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new MyPage());
        }
        private void NewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            Nav.Go(new PlaylistFormPage(null));
        }
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            string keyword = SearchBox.Text.Trim();
            if (e.Key == Key.Enter && keyword.Length > 0)
                Nav.Go(new SearchPage(keyword));
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchHint.Visibility = SearchBox.Text.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        // ===== 미디어 플레이어 =====
        private void UpdatePlayerBar()
        {
            var track = Player.Current;
            PlayerBar.Visibility = track == null ? Visibility.Collapsed : Visibility.Visible;
            if (track == null)
                return;
            Thumb.SetUrl(BarThumb, track.ThumbnailUrl);
            BarTitle.Text = track.MusicTitle;
            BarSub.Text = Player.Notice ?? (string.IsNullOrEmpty(track.Artist)
            ? Player.PlaylistTitle : $"{track.Artist} · {Player.PlaylistTitle}");
            // SetResourceReference: 테마를 바꾸면 색도 따라 바뀐다.
            BarSub.SetResourceReference(TextBlock.ForegroundProperty,
            Player.Notice != null ? "DangerBrush" : "SubTextBrush");
            // 재생 / 일시정지
            PlayButton.Content = Player.IsPlaying ? "" : "";
            PlayButton.ToolTip = Player.IsPlaying ? "일시정지" : "재생";
            NextButton.IsEnabled = Player.HasNext;
            // 음소거 / 소리
            MuteButton.Content = Player.IsMuted ? "" : "";
            MuteButton.ToolTip = Player.IsMuted ? "소리 켜기" : "음소거";
            UpdateProgress();
        }
        private void UpdateProgress()
        {
            double ratio = Player.Duration > 0 ? Player.Position / Player.Duration : 0;
            ProgressFill.Width = ProgressArea.ActualWidth * Math.Clamp(ratio, 0, 1);
            TimeLabel.Text = Player.Duration > 0
            ? $"{FormatTime(Player.Position)} / {FormatTime(Player.Duration)}" : "";
        }
        private static string FormatTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }
        // 작은 영상 창을 사용자가 옮긴 위치 (null이면 오른쪽 아래 기본 자리)
        private Point? floatPosition;
        private bool draggingVideo;
        private Point dragStartMouse;
        private Point dragStartPosition;
        // 재생 중인 플레이리스트의 상세 화면이면 썸네일 칸에 겹치고, 아니면 작은 창으로 띄운다.
        private void UpdateVideoPosition()
        {
            // 앱을 다시 켜서 불러온 곡은 재생 버튼을 누르기 전까지 빈 영상 창을 띄우지 않는다.
            if (Player.Current == null || videoHidden || Player.WaitingToResume)
            {
                if (VideoHost.Visibility != Visibility.Collapsed)
                    VideoHost.Visibility = Visibility.Collapsed;
                return;
            }
            VideoHost.Visibility = Visibility.Visible;
            double left, top, width, height;
            var slot = Player.VideoSlot;
            bool inSlot = slot != null && slot.IsVisible
            && Player.SlotPlaylistId == Player.PlaylistId && slot.ActualWidth > 0;
            var handleVisibility = inSlot || Player.IsFullScreen
            ? Visibility.Collapsed : Visibility.Visible;
            if (VideoHandle.Visibility != handleVisibility)
                VideoHandle.Visibility = handleVisibility;
            if (Player.IsFullScreen)
            {
                // 창 전체를 영상으로 덮는다.
                (left, top, width, height) = (0, 0, RootGrid.ActualWidth, RootGrid.ActualHeight);
            }
            else if (inSlot)
            {
                var point = slot!.TranslatePoint(new Point(0, 0), RootGrid);
                (left, top, width, height) = (point.X, point.Y, slot.ActualWidth, slot.ActualHeight);
            }
            else
            {
                // 영상 200 + 손잡이 24
                (width, height) = (356, 224);
                double barTop = PlayerBar.TranslatePoint(new Point(0, 0), RootGrid).Y;
                var position = floatPosition
                ?? new Point(RootGrid.ActualWidth - 56 - width, barTop - 20 - height);
                // 창 밖이나 하단 바 위로 나가지 않게 한다. (창 크기를 줄여도 따라 들어온다)
                left = Math.Clamp(position.X, 0, Math.Max(0, RootGrid.ActualWidth - width));
                top = Math.Clamp(position.Y, 0, Math.Max(0, barTop - height));
            }
            // 값이 같을 때 다시 넣으면 레이아웃이 반복되므로 바뀔 때만 넣는다.
            var margin = new Thickness(Math.Round(left), Math.Round(top), 0, 0);
            if (VideoHost.Margin != margin)
                VideoHost.Margin = margin;
            if (VideoHost.Width != Math.Round(width))
                VideoHost.Width = Math.Round(width);
            if (VideoHost.Height != Math.Round(height))
                VideoHost.Height = Math.Round(height);
        }
        // 영상 창을 닫아 둔 상태 (음악은 계속 나온다)
        private bool videoHidden;
        private void SetVideoHidden(bool hidden)
        {
            videoHidden = hidden;
            VideoToggleButton.ToolTip = hidden ? "영상 보기" : "영상 숨기기";
            AutomationProperties.SetName(VideoToggleButton, (string)VideoToggleButton.ToolTip);
            // 숨긴 상태면 아이콘을 흐리게 표시한다.
            VideoToggleButton.SetResourceReference(Button.ForegroundProperty,
            hidden ? "SubTextBrush" : "TextBrush");
            UpdateVideoPosition();
        }
        private void HideVideo_Click(object sender, RoutedEventArgs e)
        {
            SetVideoHidden(true);
        }
        private void VideoToggle_Click(object sender, RoutedEventArgs e)
        {
            SetVideoHidden(!videoHidden);
        }
        // 전체화면 전의 창 상태 (해제할 때 되돌린다)
        private WindowState stateBeforeFullScreen;
        private void ApplyFullScreen()
        {
            if (Player.IsFullScreen)
            {
                stateBeforeFullScreen = WindowState;
                // 이미 최대화 상태면 작업 표시줄까지 덮이지 않으므로 한 번 풀었다가 다시 최대화한다.
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = stateBeforeFullScreen;
            }
            UpdateVideoPosition();
        }
        // 손잡이를 눌러 끌면 작은 영상 창이 따라 움직인다.
        private void VideoHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                floatPosition = null;
                UpdateVideoPosition();
                return;
            }
            draggingVideo = true;
            dragStartMouse = e.GetPosition(RootGrid);
            dragStartPosition = new Point(VideoHost.Margin.Left, VideoHost.Margin.Top);
            VideoHandle.CaptureMouse();
            e.Handled = true;
        }
        private void VideoHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingVideo)
                return;
            var delta = e.GetPosition(RootGrid) - dragStartMouse;
            floatPosition = dragStartPosition + delta;
            UpdateVideoPosition();
        }
        private void VideoHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            draggingVideo = false;
            VideoHandle.ReleaseMouseCapture();
        }
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            Player.Toggle();
        }
        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            await Player.Previous();
        }
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            await Player.Next();
        }
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            Player.ToggleMute();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
        }
        private void BarYouTube_Click(object sender, RoutedEventArgs e)
        {
            var track = Player.Current;
            if (track == null)
                return;
            // 브라우저와 겹쳐서 들리지 않도록 앱 안의 재생은 멈춘다.
            if (Player.IsPlaying)
                Player.Toggle();
            Nav.OpenYouTube(track.YoutubeUrl);
        }
        private void BarTitle_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage is DetailPage page && page.PlaylistId == Player.PlaylistId)
                return;
            Nav.Go(new DetailPage(Player.PlaylistId));
        }
        private void Progress_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProgressArea.ActualWidth > 0)
                Player.Seek(e.GetPosition(ProgressArea).X / ProgressArea.ActualWidth);
        }
        private void ProgressArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProgress();
        }
    }
}
