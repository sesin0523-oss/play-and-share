using System.Windows;
using System.Windows.Controls;
namespace MyPlaylist.Client.Views
{
    public partial class ConfirmDialog : UserControl, IDialog
    {
        private readonly TaskCompletionSource<bool> result = new();
        public Task<bool> Result => result.Task;
        public ConfirmDialog(string title, string message,
        string confirmText, bool danger, bool showCancel)
        {
            InitializeComponent();
            TitleLabel.Text = title;
            MessageLabel.Text = message;
            ConfirmButton.Content = confirmText;
            if (danger)
                ConfirmButton.Style = (Style)FindResource("DangerButton");
            if (!showCancel)
                CancelButton.Visibility = Visibility.Collapsed;
        }
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            result.TrySetResult(true);
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            result.TrySetResult(false);
        }
    }
}
