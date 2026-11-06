using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MyPlaylist.Client.Models;
namespace MyPlaylist.Client.Services
{
    // 앱 안의 YouTube 플레이어(Web/player.html)로 곡을 재생하고 재생 목록을 관리한다.
    public static class Player
    {
        private const string Host = "player.myplaylist";
        private static WebView2CompositionControl web = null!;
        private static Task? initTask;
        private static bool ready;
        // 플레이어가 준비되기 전에 누른 재생 (준비되면 실행한다)
        private static string? pendingPlay;
        private static List<PlaylistTrack> queue = new();
        public static int PlaylistId { get; private set; }
        public static string PlaylistTitle { get; private set; } = "";
        public static int Index { get; private set; } = -1;
        public static PlaylistTrack? Current =>
        Index >= 0 && Index < queue.Count ? queue[Index] : null;
        public static bool HasNext => Index + 1 < queue.Count;
        public static bool IsPlaying { get; private set; }
        public static bool IsMuted { get; private set; }
        public static double Position { get; private set; }
        public static double Duration { get; private set; }
        // 재생할 수 없는 영상을 만났을 때 하단 바에 표시할 안내
        public static string? Notice { get; private set; }
        // 곡, 재생 상태가 바뀔 때
        public static event Action? Changed;
        // 목록에서 곡을 눌러 새로 재생을 시작할 때 (닫아 둔 영상 창을 다시 띄운다)
        public static event Action? Started;
        // 재생 위치가 바뀔 때 (0.5초마다)
        public static event Action? TimeChanged;
        // YouTube 플레이어의 전체화면 버튼을 눌렀을 때 / 해제했을 때
        public static bool IsFullScreen { get; private set; }
        public static event Action? FullScreenChanged;
        // 재생 중인 플레이리스트의 상세 화면이면 썸네일 칸에 영상을 겹쳐 표시한다.
        public static FrameworkElement? VideoSlot { get; private set; }
        public static int SlotPlaylistId { get; private set; }
        public static void Attach(WebView2CompositionControl control)
        {
            web = control;
        }
        public static void SetSlot(FrameworkElement slot, int playlistId)
        {
            VideoSlot = slot;
            SlotPlaylistId = playlistId;
        }
        public static void ClearSlot(FrameworkElement slot)
        {
            if (VideoSlot == slot)
                VideoSlot = null;
        }
        public static async Task Play(
        int playlistId, string playlistTitle, List<PlaylistTrack> tracks, int index)
        {
            if (index < 0 || index >= tracks.Count)
                return;
            queue = tracks.ToList();
            PlaylistId = playlistId;
            PlaylistTitle = playlistTitle;
            Started?.Invoke();
            await PlayAt(index, null);
        }
        public static void Toggle()
        {
            if (Current == null)
                return;
            // 앱을 다시 켜서 불러온 곡은 아직 플레이어에 없으므로 저장된 위치부터 새로 연다.
            if (needsLoad)
            {
                _ = PlayAt(Index, null, resumeAt);
                return;
            }
            IsPlaying = !IsPlaying;
            Call(IsPlaying ? "resume()" : "pause()");
            Changed?.Invoke();
        }
        public static async Task Previous()
        {
            // 곡 앞부분이 지났으면 처음부터, 아니면 이전 곡을 재생한다.
            if (needsLoad)
                await PlayAt(Position > 3 || Index == 0 ? Index : Index - 1, null);
            else if (Position > 3 || Index == 0)
            {
                IsPlaying = true;
                Call("restart()");
                Changed?.Invoke();
            }
            else if (Index > 0)
                await PlayAt(Index - 1, null);
        }
        public static async Task Next()
        {
            if (HasNext)
                await PlayAt(Index + 1, null);
        }
        public static void Seek(double ratio)
        {
            ratio = Math.Clamp(ratio, 0, 1);
            // 아직 열지 않은 곡이면 재생을 시작할 위치만 바꿔 둔다.
            if (needsLoad)
            {
                resumeAt = Position = Duration * ratio;
                TimeChanged?.Invoke();
                return;
            }
            Call($"seek({Math.Clamp(ratio, 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)})");
        }
        public static void ToggleMute()
        {
            IsMuted = !IsMuted;
            Call($"setMuted({(IsMuted ? "true" : "false")})");
            Changed?.Invoke();
        }
        public static void ExitFullScreen()
        {
            Call("if (document.fullscreenElement) document.exitFullscreen()");
        }
        public static void Stop()
        {
            ExitFullScreen();
            Call("stop()");
            queue = new();
            Index = -1;
            IsPlaying = false;
            Position = Duration = 0;
            Notice = null;
            pendingPlay = null;
            needsLoad = false;
            Changed?.Invoke();
        }
        // ===== 마지막으로 듣던 곡 저장 / 불러오기 =====
        private static readonly string SessionFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyPlaylist", "last-played.json");
        // 앱을 다시 켜서 불러온 곡이면 true (재생 버튼을 누를 때 플레이어에 연다)
        private static bool needsLoad;
        public static bool WaitingToResume => needsLoad;
        private static double resumeAt;
        private class Session
        {
            public int PlaylistId { get; set; }
            public string PlaylistTitle { get; set; } = "";
            public int Index { get; set; }
            public double Position { get; set; }
            public double Duration { get; set; }
            public List<PlaylistTrack> Tracks { get; set; } = new();
        }
        // 앱을 닫을 때 호출한다. 재생 중인 곡이 없으면 저장 파일을 지운다.
        public static void SaveSession()
        {
            try
            {
                if (Current == null)
                {
                    File.Delete(SessionFile);
                    return;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(SessionFile)!);
                File.WriteAllText(SessionFile, JsonSerializer.Serialize(new Session
                {
                    PlaylistId = PlaylistId,
                    PlaylistTitle = PlaylistTitle,
                    Index = Index,
                    Position = Position,
                    Duration = Duration,
                    Tracks = queue
                }));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 저장하지 못해도 앱 종료는 막지 않는다.
            }
        }
        // 앱을 켤 때 호출한다. 하단 바에 마지막 곡을 일시정지 상태로 띄운다.
        public static void RestoreSession()
        {
            try
            {
                if (!File.Exists(SessionFile))
                    return;
                var session = JsonSerializer.Deserialize<Session>(File.ReadAllText(SessionFile));
                if (session == null || session.Index < 0 || session.Index >= session.Tracks.Count)
                    return;
                queue = session.Tracks;
                PlaylistId = session.PlaylistId;
                PlaylistTitle = session.PlaylistTitle;
                Index = session.Index;
                Duration = session.Duration;
                // 거의 끝까지 들은 곡이면 처음부터 다시 듣는다.
                Position = session.Position < session.Duration - 3 ? session.Position : 0;
                resumeAt = Position;
                IsPlaying = false;
                needsLoad = true;
                Changed?.Invoke();
                TimeChanged?.Invoke();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // 파일이 깨졌으면 무시하고 빈 상태로 시작한다.
            }
        }
        private static async Task PlayAt(int index, string? notice, double startSeconds = 0)
        {
            needsLoad = false;
            Index = index;
            IsPlaying = true;
            Position = startSeconds;
            Duration = 0;
            Notice = notice;
            Changed?.Invoke();
            TimeChanged?.Invoke();
            var track = queue[index];
            try
            {
                await (initTask ??= Init());
            }
            catch (Exception)
            {
                // WebView2 런타임이 없는 PC에서는 브라우저로 대신 연다.
                initTask = null;
                Stop();
                await Nav.Alert("앱 안에서 재생하려면 Microsoft Edge WebView2 런타임이 필요합니다. YouTube에서 대신 엽니다.");
                Nav.OpenYouTube(track.YoutubeUrl);
                return;
            }
            string script = $"play({JsonSerializer.Serialize(track.YoutubeVideoId)}, "
            + $"{Math.Floor(startSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)})";
            if (ready)
                Call(script);
            else
                pendingPlay = script;
        }
        private static async Task Init()
        {
            // 소리가 있는 자동 재생을 허용한다. 캐시는 OneDrive 밖(LocalAppData)에 둔다.
            var options = new CoreWebView2EnvironmentOptions(
            "--autoplay-policy=no-user-gesture-required");
            string dataFolder = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "MyPlaylist", "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, dataFolder, options);
            await web.EnsureCoreWebView2Async(env);
            var core = web.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            // file:// 로 열면 YouTube가 출처가 없다며 재생을 막으므로(오류 153) https 주소로 연다.
            core.SetVirtualHostNameToFolderMapping(Host,
            Path.Combine(AppContext.BaseDirectory, "Web"),
            CoreWebView2HostResourceAccessKind.Allow);
            core.WebMessageReceived += OnMessage;
            // WebView2는 전체화면 요청을 알려주기만 하므로 창을 키우는 일은 MainWindow가 한다.
            core.ContainsFullScreenElementChanged += (_, _) =>
            {
                IsFullScreen = core.ContainsFullScreenElement;
                FullScreenChanged?.Invoke();
            };
            // 플레이어 안의 YouTube 로고 등을 누르면 기본 브라우저로 연다.
            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                Nav.OpenYouTube(e.Uri);
            };
            core.Navigate($"https://{Host}/player.html");
        }
        private static void Call(string script)
        {
            if (ready)
                _ = web.ExecuteScriptAsync(script);
        }
        private static void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            using var doc = JsonDocument.Parse(e.TryGetWebMessageAsString());
            var root = doc.RootElement;
            switch (root.GetProperty("type").GetString())
            {
                case "ready":
                    ready = true;
                    if (IsMuted)
                        Call("setMuted(true)");
                    if (pendingPlay != null)
                    {
                        Call(pendingPlay);
                        pendingPlay = null;
                    }
                    break;
                case "state":
                    OnState(root.GetProperty("state").GetInt32());
                    break;
                case "error":
                    _ = OnError(root.GetProperty("code").GetInt32());
                    break;
                case "time":
                    Position = root.GetProperty("cur").GetDouble();
                    Duration = root.GetProperty("dur").GetDouble();
                    TimeChanged?.Invoke();
                    break;
            }
        }
        // YouTube 상태: 0 끝, 1 재생, 2 일시정지
        private static void OnState(int state)
        {
            if (Current == null)
                return;
            if (state == 0)
            {
                if (HasNext)
                    _ = PlayAt(Index + 1, null);
                else
                {
                    IsPlaying = false;
                    Changed?.Invoke();
                }
            }
            else if (state == 1 || state == 2)
            {
                IsPlaying = state == 1;
                Changed?.Invoke();
            }
        }
        // 101, 150: 퍼가기가 허용되지 않은 영상 / 100: 삭제되었거나 비공개인 영상
        private static async Task OnError(int code)
        {
            var track = Current;
            if (track == null)
                return;
            if (HasNext)
            {
                await PlayAt(Index + 1, $"'{track.MusicTitle}'은(는) 앱에서 재생할 수 없어 건너뛰었어요");
                return;
            }
            IsPlaying = false;
            Notice = code is 101 or 150
            ? "앱에서 재생할 수 없는 영상이에요. YouTube에서 보기를 눌러 주세요"
            : "재생할 수 없는 영상이에요. 삭제되었거나 비공개일 수 있어요";
            Changed?.Invoke();
        }
    }
}
