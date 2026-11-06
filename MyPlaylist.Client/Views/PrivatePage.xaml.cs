using System.Windows;
using System.Windows.Controls;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    public partial class PrivatePage : UserControl, INavPage
    {
        public PrivatePage()
        {
            InitializeComponent();
        }
        public Task OnShown(bool firstTime)
        {
            return Task.CompletedTask;
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Nav.Back();
        }
    }
}
