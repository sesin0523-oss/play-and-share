using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Windows.Controls;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Views;
namespace MyPlaylist.Client.Services
{
    // 화면에 나타날 때 데이터를 불러오는 페이지
    public interface INavPage
    {
        // firstTime이 false이면 다른 화면에서 돌아온 경우이다.
        Task OnShown(bool firstTime);
        // 로그인, 회원가입 화면은 상단 바에 로고만 표시한다.
        bool LogoOnly => false;
    }
    // 화면 이동, 로그인 상태, 공통 오류 처리를 맡는다.
    public static class Nav
    {
        public static readonly ApiService Api = new();
        public static UserInfo? CurrentUser { get; private set; }
        private static readonly Stack<UserControl> history = new();
        private static MainWindow window = null!;
        private static bool busy;
        public static void Start(MainWindow mainWindow)
        {
            window = mainWindow;
            Show(new MainPage(), true);
        }
        public static void Go(UserControl page)
        {
            if (window.CurrentPage != null)
                history.Push(window.CurrentPage);
            Show(page, true);
        }
        // 현재 화면을 기록에 남기지 않고 바꾼다.
        public static void Replace(UserControl page)
        {
            Show(page, true);
        }
        public static void Back()
        {
            if (history.Count == 0)
            {
                Show(new MainPage(), true);
                return;
            }
            Show(history.Pop(), false);
        }
        public static void Home()
        {
            history.Clear();
            Show(new MainPage(), true);
        }
        // 로그인 후 로그인, 회원가입 화면을 건너뛰고 이전 화면으로 돌아간다.
        public static void BackFromAuth()
        {
            while (history.Count > 0 && ((INavPage)history.Peek()).LogoOnly)
                history.Pop();
            Back();
        }
        public static void SetUser(UserInfo? user)
        {
            CurrentUser = user;
            window.UpdateTopBar();
        }
        // 화면 데이터를 불러올 때 사용한다.
        public static async Task Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                // 탈퇴나 세션 만료로 로그인이 풀린 경우
                SetUser(null);
                if (window.CurrentPage is not LoginPage)
                    Go(new LoginPage());
            }
            catch (ApiException ex)
            {
                await Alert(ex.Message);
            }
            catch (HttpRequestException)
            {
                await Alert("서버에 연결할 수 없습니다. 서버가 실행 중인지 확인해 주세요.");
            }
            catch (TaskCanceledException)
            {
                await Alert("서버 응답 시간이 초과되었습니다.");
            }
        }
        // 등록, 수정, 삭제 버튼에 사용한다. 처리 중에 다시 눌러도 한 번만 실행된다.
        public static async Task Action(Func<Task> action)
        {
            if (busy)
                return;
            busy = true;
            try
            {
                await Run(action);
            }
            finally
            {
                busy = false;
            }
        }
        public static Task<bool> Confirm(
        string title, string message, string confirmText, bool danger)
        {
            return window.ShowDialog(
            new ConfirmDialog(title, message, confirmText, danger, true));
        }
        public static Task Alert(string message)
        {
            return window.ShowDialog(
            new ConfirmDialog("알림", message, "확인", false, false));
        }
        public static Task<bool> ShowDialog(IDialog dialog)
        {
            return window.ShowDialog(dialog);
        }
        public static void OpenYouTube(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        private static void Show(UserControl page, bool firstTime)
        {
            window.ShowPage(page);
            _ = Run(() => ((INavPage)page).OnShown(firstTime));
        }
    }
}
