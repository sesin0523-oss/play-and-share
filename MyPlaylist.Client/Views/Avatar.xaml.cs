using System.Windows;
using System.Windows.Controls;
namespace MyPlaylist.Client.Views
{
    public partial class Avatar : UserControl
    {
        // 표시할 아이디 (첫 글자만 사용)
        public static readonly DependencyProperty UserNameProperty =
        DependencyProperty.Register(nameof(UserName), typeof(string), typeof(Avatar),
        new PropertyMetadata("", (d, _) => ((Avatar)d).Update()));
        // 원의 지름
        public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(Avatar),
        new PropertyMetadata(26.0, (d, _) => ((Avatar)d).Update()));
        public string UserName
        {
            get => (string)GetValue(UserNameProperty);
            set => SetValue(UserNameProperty, value);
        }
        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }
        public Avatar()
        {
            InitializeComponent();
            Update();
        }
        private void Update()
        {
            Circle.Width = Circle.Height = Size;
            Circle.CornerRadius = new CornerRadius(Size / 2);
            Initial.FontSize = Size * 0.46;
            string name = UserName ?? "";
            // 탈퇴한 회원 등 이름이 없으면 빈 원으로 둔다.
            Initial.Text = name.Length == 0 || name == "탈퇴한 회원"
            ? "" : name.Substring(0, 1).ToUpperInvariant();
        }
    }
}
