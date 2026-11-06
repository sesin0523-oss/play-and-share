using System.Windows;
using System.Windows.Controls;
namespace MyPlaylist.Client.Views
{
    // "‹ 2 ›" 형태의 페이지 넘김. 현재 페이지 번호만 표시한다.
    public partial class Pager : UserControl
    {
        private int page = 1;
        public event Action<int>? PageChanged;
        public Pager()
        {
            InitializeComponent();
        }
        public void Set(int currentPage, int totalPages)
        {
            page = currentPage;
            PageLabel.Text = currentPage.ToString();
            PreviousButton.IsEnabled = currentPage > 1;
            NextButton.IsEnabled = currentPage < totalPages;
        }
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            PageChanged?.Invoke(page - 1);
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            PageChanged?.Invoke(page + 1);
        }
    }
}
