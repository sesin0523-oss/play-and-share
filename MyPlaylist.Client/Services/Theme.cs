using System.IO;
using System.Windows;
namespace MyPlaylist.Client.Services
{
    // 어두운 / 밝은 테마를 바꾸고, 고른 테마를 다음 실행 때도 쓰도록 저장한다.
    public static class Theme
    {
        private static readonly string SettingFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyPlaylist", "theme.txt");
        public static bool IsDark { get; private set; } = true;
        public static event Action? Changed;
        // 앱을 시작할 때 저장된 테마를 적용한다. (기본은 어두운 테마)
        public static void Load()
        {
            try
            {
                if (File.Exists(SettingFile) && File.ReadAllText(SettingFile).Trim() == "light")
                    Apply(false);
            }
            catch (IOException)
            {
                // 설정 파일을 읽지 못하면 기본 테마로 시작한다.
            }
        }
        public static void Toggle()
        {
            Apply(!IsDark);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingFile)!);
                File.WriteAllText(SettingFile, IsDark ? "dark" : "light");
            }
            catch (IOException)
            {
                // 저장에 실패해도 지금 화면의 테마는 바뀐 상태로 둔다.
            }
        }
        private static void Apply(bool dark)
        {
            IsDark = dark;
            // App.xaml의 첫 번째 MergedDictionary가 테마 색상이다.
            // 색상을 DynamicResource로 참조하므로 교체하면 열린 화면도 바로 바뀐다.
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            dictionaries[0] = new ResourceDictionary
            {
                Source = new Uri($"Themes/{(dark ? "Dark" : "Light")}.xaml", UriKind.Relative)
            };
            Changed?.Invoke();
        }
    }
}
