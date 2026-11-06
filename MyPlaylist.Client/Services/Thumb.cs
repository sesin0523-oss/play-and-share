using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace MyPlaylist.Client.Services
{
    // Image에 썸네일 주소를 연결한다. 주소가 없으면 빈 칸으로 둔다.
    public static class Thumb
    {
        public static readonly DependencyProperty UrlProperty =
        DependencyProperty.RegisterAttached("Url", typeof(string),
        typeof(Thumb), new PropertyMetadata(null, OnUrlChanged));
        public static string? GetUrl(DependencyObject target)
        {
            return (string?)target.GetValue(UrlProperty);
        }
        public static void SetUrl(DependencyObject target, string? value)
        {
            target.SetValue(UrlProperty, value);
        }
        private static void OnUrlChanged(
        DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is Image image)
                Load(image, e.NewValue as string);
        }
        // 큰 영역에는 고화질 썸네일을 쓰고, 없는 영상이면 기본 썸네일로 대신한다.
        public static void SetLarge(Image image, string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                image.Source = null;
                return;
            }
            var bitmap = CreateBitmap(url.Replace("mqdefault", "maxresdefault"));
            bitmap.DownloadFailed += (_, _) => Load(image, url);
            image.Source = bitmap;
        }
        private static void Load(Image image, string? url)
        {
            image.Source = string.IsNullOrEmpty(url) ? null : CreateBitmap(url);
        }
        private static BitmapImage CreateBitmap(string url)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url);
            bitmap.EndInit();
            return bitmap;
        }
    }
}
