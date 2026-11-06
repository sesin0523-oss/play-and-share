using System.Net;
using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class SignupPage : UserControl, INavPage
    {
        public bool LogoOnly => true;
        public SignupPage()
        {
            InitializeComponent();
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
            ErrorLabel.Visibility = Visibility.Collapsed;
            await Nav.Run(async () =>
            {
                bool available = await Nav.Api.CheckName(userName);
                NameResultLabel.Text = available
                ? "사용 가능한 아이디입니다" : "이미 사용 중인 아이디입니다";
                NameResultLabel.SetResourceReference(TextBlock.ForegroundProperty,
                available ? "GoldBrush" : "DangerBrush");
                NameResultLabel.Visibility = Visibility.Visible;
            });
        }
        private async void Signup_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text.Trim();
            string password = PasswordBox.Password;
            if (!CheckUserName(userName))
                return;
            if (password.Length < 8 || password.Length > 100)
            {
                ShowError("비밀번호는 8~100자로 입력해 주세요.");
                return;
            }
            if (password != PasswordConfirmBox.Password)
            {
                ShowError("비밀번호 확인이 일치하지 않습니다.");
                return;
            }
            await Nav.Action(async () =>
            {
                try
                {
                    await Nav.Api.Signup(userName, password);
                    // 가입하면 바로 로그인 상태로 시작한다.
                    Nav.SetUser(await Nav.Api.Login(userName, password));
                    Nav.BackFromAuth();
                }
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict
                || ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    ShowError(ex.Message);
                }
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
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            Nav.Replace(new LoginPage());
        }
    }
}
