using System.Net;
using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class LoginPage : UserControl, INavPage
    {
        public bool LogoOnly => true;
        public LoginPage()
        {
            InitializeComponent();
        }
        public Task OnShown(bool firstTime)
        {
            return Task.CompletedTask;
        }
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text.Trim();
            string password = PasswordBox.Password;
            if (userName.Length < 2 || password.Length < 8)
            {
                ShowError("아이디 또는 비밀번호가 올바르지 않습니다.");
                return;
            }
            await Nav.Action(async () =>
            {
                try
                {
                    Nav.SetUser(await Nav.Api.Login(userName, password));
                    Nav.BackFromAuth();
                }
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized
                || ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    ShowError(ex.Message);
                }
            });
        }
        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.Visibility = Visibility.Visible;
        }
        private void Signup_Click(object sender, RoutedEventArgs e)
        {
            Nav.Replace(new SignupPage());
        }
    }
}
