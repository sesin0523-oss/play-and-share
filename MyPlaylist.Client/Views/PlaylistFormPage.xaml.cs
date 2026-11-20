using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using MyPlaylist.Client.Models;
using MyPlaylist.Client.Services;
namespace MyPlaylist.Client.Views
{
    // existing이 null이면 만들기, 있으면 수정 화면이다.
    public partial class PlaylistFormPage : UserControl, INavPage
    {
        private readonly PlaylistSummary? existing;
        public PlaylistFormPage(PlaylistSummary? playlist)
        {
            InitializeComponent();
            existing = playlist;
            TitleLabel.Text = existing == null ? "새 플레이리스트" : "플레이리스트 수정";
            SaveButton.Content = existing == null ? "만들기" : "저장";
            if (existing != null)
            {
                // AI 초안은 새로 만들 때만 쓴다. (수정 화면에서 곡까지 바꾸면 헷갈린다)
                AiPanel.Visibility = Visibility.Collapsed;
                PlaylistTitleBox.Text = existing.PlaylistTitle;
                DescriptionBox.Text = existing.Description;
                PrivateRadio.IsChecked = !existing.IsPublic;
            }
        }
        public async Task OnShown(bool firstTime)
        {
            if (!firstTime)
                return;
            // 추천용 태그 목록(많이 쓰인 순)을 받고, 수정 화면이면 원래 태그를 넣어 둔다.
            allTags = await Nav.Api.GetTags();
            SelectedTags = existing?.Tags.ToList() ?? new();
            RenderSelectedTags();
        }
        // ===== 태그 =====
        private const int MaxTags = 3;
        private const int MaxTagLength = 20;
        private List<string> allTags = new();
        private List<string> SelectedTags { get; set; } = new();
        private static readonly Regex AllowedTag = new(@"^[\p{L}\p{N} ]+$");
        // 서버와 같은 방식으로 정리: 앞뒤 공백과 #을 떼고 연속 공백은 하나로
        private static string NormalizeTag(string text)
        {
            return Regex.Replace(text.Trim().TrimStart('#').Trim(), @"\s+", " ");
        }
        private void RenderSelectedTags()
        {
            SelectedTagPanel.Children.Clear();
            foreach (string name in SelectedTags)
            {
                var remove = new Button
                {
                    Content = "",
                    Style = (Style)FindResource("IconButton"),
                    FontSize = 9,
                    Padding = new Thickness(8, 0, 0, 0),
                    ToolTip = "태그 빼기",
                    Tag = name
                };
                AutomationProperties.SetName(remove, $"{name} 태그 빼기");
                remove.Click += RemoveTag_Click;
                var text = new TextBlock { Text = $"#{name}", FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
                text.SetResourceReference(TextBlock.ForegroundProperty, "GoldBrush");
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 4, 8, 5),
                    Margin = new Thickness(0, 0, 8, 10),
                    Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { text, remove } }
                };
                chip.SetResourceReference(Border.BorderBrushProperty, "GoldBrush");
                SelectedTagPanel.Children.Add(chip);
            }
            // 3개를 다 고르면 입력칸을 막는다.
            bool full = SelectedTags.Count >= MaxTags;
            TagInput.IsEnabled = !full;
            TagHint.Text = full ? "태그를 3개 모두 골랐어요" : "태그를 검색하거나 새로 입력하고 Enter";
        }
        private void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            SelectedTags.Remove((string)((Button)sender).Tag);
            ErrorLabel.Visibility = Visibility.Collapsed;
            RenderSelectedTags();
            TagInput.Focus();
        }
        // 태그를 넣는다. 목록에 없는 이름이면 새 태그가 된다. 실패하면 안내 문구를 보여 준다.
        private void AddTag(string text)
        {
            string name = NormalizeTag(text);
            string? error = name.Length == 0 ? null
            : name.Length > MaxTagLength ? $"태그는 {MaxTagLength}자 이내로 입력해 주세요."
            : !AllowedTag.IsMatch(name) ? "태그에는 한글, 영문, 숫자, 공백만 쓸 수 있어요."
            : SelectedTags.Count >= MaxTags ? $"태그는 최대 {MaxTags}개까지 고를 수 있어요."
            : SelectedTags.Contains(name, StringComparer.OrdinalIgnoreCase) ? "이미 고른 태그예요."
            : null;
            if (error != null)
            {
                ErrorLabel.Text = error;
                ErrorLabel.Visibility = Visibility.Visible;
                return;
            }
            if (name.Length == 0)
                return;
            // 대소문자만 다른 기존 태그가 있으면 그 이름을 쓴다.
            name = allTags.FirstOrDefault(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)) ?? name;
            SelectedTags.Add(name);
            ErrorLabel.Visibility = Visibility.Collapsed;
            TagInput.Clear();
            TagSuggestPopup.IsOpen = false;
            RenderSelectedTags();
            if (TagInput.IsEnabled)
                TagInput.Focus();
        }
        private void TagInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TagHint.Visibility = TagInput.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            ShowSuggestions();
        }
        private void TagInput_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ShowSuggestions();
        }
        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AddTag(TagInput.Text);
            }
            else if (e.Key == Key.Escape)
                TagSuggestPopup.IsOpen = false;
        }
        // 입력한 글자가 들어간 태그를 최대 8개 보여 주고, 같은 이름이 없으면 "새 태그 만들기"를 맨 위에 둔다.
        private void ShowSuggestions()
        {
            string text = NormalizeTag(TagInput.Text);
            var matches = allTags
            .Where(t => !SelectedTags.Contains(t, StringComparer.OrdinalIgnoreCase))
            .Where(t => text.Length == 0 || t.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
            TagSuggestList.Children.Clear();
            bool exists = allTags.Any(t => string.Equals(t, text, StringComparison.OrdinalIgnoreCase));
            if (text.Length > 0 && !exists)
                TagSuggestList.Children.Add(MakeSuggestion(text, $"＋  '#{text}' 새 태그 만들기", true));
            foreach (string name in matches)
                TagSuggestList.Children.Add(MakeSuggestion(name, $"#{name}", false));
            TagSuggestPopup.IsOpen = TagInput.IsKeyboardFocused && TagSuggestList.Children.Count > 0;
        }
        private Button MakeSuggestion(string name, string label, bool isNew)
        {
            var text = new TextBlock { Text = label, FontSize = 15, Padding = new Thickness(14, 9, 14, 9) };
            text.SetResourceReference(TextBlock.ForegroundProperty, isNew ? "GoldBrush" : "TextBrush");
            var button = new Button { Content = text, Style = (Style)FindResource("AreaButton"), Tag = name };
            button.Click += (_, _) => AddTag(name);
            return button;
        }
        // ===== AI 초안 =====
        private bool aiBusy;
        private void AiPromptBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AiPromptHint.Visibility = AiPromptBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        private void AiPromptBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            e.Handled = true;
            AiDraft_Click(sender, e);
        }
        private async void AiDraft_Click(object sender, RoutedEventArgs e)
        {
            string prompt = AiPromptBox.Text.Trim();
            if (aiBusy)
                return;
            if (prompt.Length < 2)
            {
                ShowAiStatus("듣고 싶은 분위기를 두 글자 이상 적어 주세요.", true);
                return;
            }
            aiBusy = true;
            AiButton.IsEnabled = false;
            AiButton.Content = "만드는 중…";
            // 노트북에서는 10초 안팎 걸리므로 기다리는 동안 지난 시간을 보여 준다.
            var started = DateTime.Now;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => ShowAiStatus($"AI가 분위기를 읽고 어울리는 가수를 고르는 중이에요… {(int)(DateTime.Now - started).TotalSeconds}초", false);
            ShowAiStatus("AI가 분위기를 읽고 어울리는 가수를 고르는 중이에요…", false);
            timer.Start();
            try
            {
                // 로그인이 풀린 경우(401)는 Nav.Run이 로그인 화면으로 보낸다.
                await Nav.Run(async () =>
                {
                    try
                    {
                        ApplyDraft(await Nav.Api.CreateAiDraft(prompt));
                    }
                    catch (ApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                    {
                        ShowAiStatus(ex.Message, true);
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        ShowAiStatus("서버에 연결할 수 없어요. 서버가 실행 중인지 확인해 주세요.", true);
                    }
                    catch (TaskCanceledException)
                    {
                        ShowAiStatus("AI 응답이 너무 오래 걸려요. 잠시 후 다시 시도해 주세요.", true);
                    }
                });
            }
            finally
            {
                timer.Stop();
                aiBusy = false;
                AiButton.IsEnabled = true;
                AiButton.Content = "초안 만들기";
            }
        }
        private void ShowAiStatus(string text, bool error)
        {
            AiStatus.Text = text;
            AiStatus.SetResourceReference(TextBlock.ForegroundProperty, error ? "DangerBrush" : "SubTextBrush");
            AiStatus.Visibility = Visibility.Visible;
        }
        // 초안을 입력칸에 채운다. 저장하기 전까지 마음대로 고칠 수 있다.
        private void ApplyDraft(AiDraft draft)
        {
            PlaylistTitleBox.Text = draft.PlaylistTitle;
            DescriptionBox.Text = draft.Description ?? "";
            SelectedTags = draft.Tags.Take(MaxTags).ToList();
            ErrorLabel.Visibility = Visibility.Collapsed;
            RenderSelectedTags();
            AiTrackList.Children.Clear();
            foreach (var track in draft.Tracks)
                AiTrackList.Children.Add(MakeTrackRow(track));
            bool hasTracks = draft.Tracks.Count > 0;
            var trackVisibility = hasTracks ? Visibility.Visible : Visibility.Collapsed;
            AiTracksLabel.Visibility = trackVisibility;
            AiTracksPanel.Visibility = trackVisibility;
            AiTracksCount.Visibility = trackVisibility;
            AiTracksInfo.Text = draft.SearchQueries.Count > 0
            ? $"AI가 고른 가수: {string.Join(", ", draft.SearchQueries)}  ·  체크한 곡이 만들기와 함께 담겨요"
            : "체크한 곡이 만들기와 함께 담겨요";
            UpdateTrackCount();
            string done = hasTracks
            ? $"초안을 채웠어요. 곡 후보 {draft.Tracks.Count}개를 확인하고 마음대로 고쳐서 만들어 주세요."
            : "제목·설명·태그를 채웠어요. 어울리는 곡은 찾지 못해서, 만든 뒤 직접 추가해 주세요.";
            ShowAiStatus(draft.Notice == null ? done : $"{done} ({draft.Notice})", false);
        }
        private CheckBox MakeTrackRow(AiDraftTrack track)
        {
            var image = new Image { Width = 96, Height = 54, Stretch = System.Windows.Media.Stretch.UniformToFill, Margin = new Thickness(0, 0, 14, 0) };
            Thumb.SetUrl(image, track.ThumbnailUrl);
            var title = new TextBlock { Text = track.MusicTitle, FontSize = 15, TextTrimming = TextTrimming.CharacterEllipsis };
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            var sub = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(track.Artist) ? track.SourceText : $"{track.Artist}  ·  {track.SourceText}",
                Style = (Style)FindResource("SubText"),
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { title, sub } };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(text, 1);
            grid.Children.Add(image);
            grid.Children.Add(text);
            var row = new CheckBox { Content = grid, Style = (Style)FindResource("CheckRow"), IsChecked = true, Tag = track };
            AutomationProperties.SetName(row, $"{track.MusicTitle} 담기");
            row.Checked += (_, _) => UpdateTrackCount();
            row.Unchecked += (_, _) => UpdateTrackCount();
            return row;
        }
        private List<AiDraftTrack> CheckedTracks()
        {
            return AiTrackList.Children.OfType<CheckBox>()
            .Where(c => c.IsChecked == true)
            .Select(c => (AiDraftTrack)c.Tag)
            .ToList();
        }
        private void UpdateTrackCount()
        {
            AiTracksCount.Text = $"{CheckedTracks().Count}곡 선택";
        }
        private void PlaylistTitleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TitleHint.Visibility = PlaylistTitleBox.Text.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
        }
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = PlaylistTitleBox.Text.Trim();
            string? description = string.IsNullOrWhiteSpace(DescriptionBox.Text)
            ? null : DescriptionBox.Text;
            bool isPublic = PublicRadio.IsChecked == true;
            if (title.Length == 0)
            {
                ErrorLabel.Text = "제목을 입력해 주세요.";
                ErrorLabel.Visibility = Visibility.Visible;
                return;
            }
            var tags = SelectedTags;
            var tracks = existing == null ? CheckedTracks() : new();
            await Nav.Action(async () =>
            {
                if (existing == null)
                {
                    var created = await Nav.Api.CreatePlaylist(title, description, isPublic, tags);
                    // AI 초안에서 체크한 곡을 순서대로 담는다. 한 곡이 실패해도 나머지는 담는다.
                    int failed = 0;
                    foreach (var track in tracks)
                    {
                        try
                        {
                            await Nav.Api.AddTrack(created.PlaylistId, track.YoutubeVideoId, track.MusicTitle, track.Artist);
                        }
                        catch (ApiException ex) when (ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        {
                            failed++;
                        }
                    }
                    Nav.Replace(new DetailPage(created.PlaylistId));
                    if (failed > 0)
                        await Nav.Alert($"곡 {failed}개를 담지 못했어요. 상세 화면에서 직접 추가해 주세요.");
                    return;
                }
                await Nav.Api.UpdatePlaylist(
                existing.PlaylistId, title, description, isPublic, tags);
                // 상세 API를 다시 호출하지 않도록 상세 화면의 값을 직접 바꾼다.
                existing.PlaylistTitle = title;
                existing.Description = description;
                existing.IsPublic = isPublic;
                existing.Tags = tags;
                Nav.Back();
            });
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Nav.Back();
        }
    }
}
