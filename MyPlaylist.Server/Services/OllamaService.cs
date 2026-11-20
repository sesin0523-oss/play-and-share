using System.Net.Http.Json;
using System.Text.Json;
namespace MyPlaylist.Server.Services
{
    // AI가 만든 플레이리스트 아이디어. 곡은 들어 있지 않다.
    public class PlaylistIdea
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        // YouTube 검색어로 쓸 가수 이름
        public List<string> SearchQueries { get; set; } = new();
    }
    // 이 PC에서 실행하는 오픈소스 언어 모델(Ollama) 호출
    // 작은 모델은 실제 곡 이름을 지어내므로 곡은 맡기지 않고
    // 제목·소개·태그·어울리는 가수처럼 "분위기를 말로 바꾸는 일"만 맡긴다.
    // 분위기 문장으로 검색하면 한 시간짜리 모음 영상만 나오므로 가수 이름으로 검색한다.
    public class OllamaService
    {
        private readonly HttpClient http;
        private readonly string model;
        public OllamaService(HttpClient client, IConfiguration config)
        {
            http = client;
            http.BaseAddress = new Uri(config["Ai:BaseUrl"] ?? "http://localhost:11434");
            model = config["Ai:Model"] ?? "exaone3.5:7.8b";
        }
        public string Model => model;
        private const string SystemPrompt =
        """
        너는 음악 플레이리스트 큐레이터다. 사용자가 원하는 분위기를 듣고 플레이리스트의 틀을 만든다.
        곡 이름은 절대 쓰지 않는다. 대신 분위기에 어울리는 실제 가수(밴드) 이름을 고른다.
        반드시 아래 JSON 형식으로만 답한다.
        {"title": "플레이리스트 제목(20자 이내)", "description": "한두 문장 소개", "tags": ["태그1", "태그2", "태그3"], "artists": ["가수1", "가수2", "가수3"]}
        - tags: 3개 이하. 한글·영문·숫자로 된 짧은 단어. 아래 기존 태그 중 어울리는 것이 있으면 그대로 쓴다.
        - artists: 3명. 누구나 아는 유명한 가수나 밴드의 이름만 쓴다. 장르, 분위기, 물건 이름은 쓰지 않는다.
          이름은 원래 표기 하나만 쓴다(한국 가수는 한글, 외국 가수는 영문). 이름을 번역하거나 소리 나는 대로 옮기지 않는다.
          조금이라도 확실하지 않은 가수는 빼고, 더 유명한 가수를 고른다.
        """;
        public async Task<PlaylistIdea> CreateIdeaAsync(
        string request, IEnumerable<string> knownTags, CancellationToken token)
        {
            string system = SystemPrompt
            + "\n기존 태그: " + string.Join(", ", knownTags);
            var body = new
            {
                model,
                system,
                prompt = request,
                format = "json",
                stream = false,
                // 첫 요청은 모델을 메모리에 올리느라 느리므로 30분 동안 내려놓지 않는다.
                keep_alive = "30m",
                // 온도를 높이면 없는 가수 이름을 지어내는 일이 잦아진다.
                options = new { temperature = 0.3 }
            };
            using var response = await http.PostAsJsonAsync("api/generate", body, token);
            response.EnsureSuccessStatusCode();
            using var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: token);
            string text = result!.RootElement.GetProperty("response").GetString() ?? "";
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            return new PlaylistIdea
            {
                Title = GetText(root, "title"),
                Description = GetText(root, "description"),
                Tags = GetList(root, "tags"),
                SearchQueries = GetList(root, "artists")
            };
        }
        private static string GetText(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()!.Trim() : "";
        }
        // 모델이 배열 대신 쉼표로 이은 문자열을 줄 때도 있어 둘 다 받는다.
        private static List<string> GetList(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value))
                return new();
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString()!.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (value.ValueKind != JsonValueKind.Array)
                return new();
            return value.EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        }
    }
}
