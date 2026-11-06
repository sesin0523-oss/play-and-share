using System.Net;
using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class UserEditPage : UserControl, INavPage
    {
        public UserEditPage()
        {
            InitializeComponent();
            UserNameBox.Text = Nav.CurrentUser?.UserName;
            NameResultLabel.Visibility = Visibility.Collapsed;
        }
        public Task OnShown(bool firstTime)
        {
            return Task.CompletedTask;
        }
        private void UserNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NameResultLabel.Visibility = Visibility.Collapsed;
        }
        private async void CheckName_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text.Trim();
            if (!CheckUserName(userName))
                return;
            await Nav.Run(async () =>
            {
                // 지금 쓰는 아이디는 그대로 쓸 수 있다.
                bool available = userName == Nav.CurrentUser?.UserName
                || await Nav.Api.CheckName(userName);
                NameResultLabel.Text = available
                ? "사용 가능한 아이디입니다" : "이미 사용 중인 아이디입니다";
                NameResultLabel.SetResourceReference(TextBlock.ForegroundProperty,
                available ? "GoldBrush" : "DangerBrush");
                NameResultLabel.Visibility = Visibility.Visible;
            });
        }
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text.Trim();
            string currentPassword = CurrentPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            var user = Nav.CurrentUser!;
            if (!CheckUserName(userName))
                return;
            bool changePassword = currentPassword.Length > 0 || newPassword.Length > 0;
            if (changePassword && currentPassword.Length == 0)
            {
                ShowError("현재 비밀번호를 입력해 주세요.");
                return;
            }
            if (changePassword && (newPassword.Length < 8 || newPassword.Length > 100))
            {
                ShowError("새 비밀번호는 8~100자로 입력해 주세요.");
                return;
            }
            await Nav.Action(async () =>
            {
                try
                {
                    await Nav.Api.UpdateUser(user.UserId, userName,
                    changePassword ? currentPassword : null,
                    changePassword ? newPassword : null);
                }
                // 아이디 중복, 현재 비밀번호 오류는 입력칸 아래에 표시한다.
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict
                || ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    ShowError(ex.Message);
                    return;
                }
                Nav.SetUser(new UserInfo
                {
                    UserId = user.UserId,
                    UserName = userName,
                    CreatedAt = user.CreatedAt
                });
                Nav.Back();
            });
        }
        private async void Withdraw_Click(object sender, RoutedEventArgs e)
        {
            bool confirmed = await Nav.Confirm("정말 탈퇴할까요?",
            "내 플레이리스트, 곡, 좋아요가 모두 삭제되며 되돌릴 수 없습니다.",
            "탈퇴하기", true);
            if (!confirmed)
                return;
            await Nav.Action(async () =>
            {
                await Nav.Api.DeleteUser(Nav.CurrentUser!.UserId);
                Nav.SetUser(null);
                Nav.Home();
            });
        }
        private bool CheckUserName(string userName)
        {
            if (userName.Length >= 2 && userName.Length <= 50)
                return true;
            ShowError("아이디는 2~50자로 입력해 주세요.");
            return false;
        }
        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Visibility = Visibility.Visible;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Nav.Back();
        }
    }
}
