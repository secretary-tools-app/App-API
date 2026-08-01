using System.Text.Json;
using HtmlAgilityPack;

namespace AtasApi.Hinos;

public interface IHinoScraperService
{
    Task<List<Hino>> ScrapeHinosLarAsync();
    Task<bool> NeedsRefreshAsync();
}

public class HinoScraperService : IHinoScraperService
{
    private readonly HttpClient _httpClient;
    private readonly string _hinosLarPath;
    private readonly string _metaPath;
    private const string Url = "https://www.churchofjesuschrist.org/study/music/hymns-for-home-and-church/_manifest?lang=por";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public HinoScraperService(HttpClient httpClient, IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _hinosLarPath = Path.Combine(env.ContentRootPath, "public", "hinos", "hinosParaOLar.json");
        _metaPath = Path.Combine(env.ContentRootPath, "public", "hinos", "_meta.json");
    }

    public async Task<bool> NeedsRefreshAsync()
    {
        if (!File.Exists(_metaPath))
            return true;

        var json = await File.ReadAllTextAsync(_metaPath);
        var meta = JsonSerializer.Deserialize<ScraperMeta>(json);
        if (meta is null)
            return true;

        return (DateTime.UtcNow - meta.LastFetchUtc).TotalDays >= 7;
    }

    public async Task<List<Hino>?> ScrapeHinosLarAsync()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var html = await _httpClient.GetStringAsync(Url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var hinos = new List<Hino>();

        var numberNodes = doc.DocumentNode.SelectNodes("//span[contains(@class, 'songNumber')]");

        if (numberNodes is null)
            return null;

        foreach (var numNode in numberNodes)
        {
            var numText = numNode.InnerText.Trim();
            if (!int.TryParse(numText, out var numero))
                continue;

            var nameNode = numNode.NextSibling;
            if (nameNode is null)
                continue;

            var nome = nameNode.InnerText.Trim();
            if (string.IsNullOrEmpty(nome))
                continue;

            hinos.Add(new Hino(numero, nome));
        }

        if (hinos.Count > 0)
        {
            hinos = hinos.OrderBy(h => h.Numero).ToList();

            var json = JsonSerializer.Serialize(hinos, JsonOptions);
            await File.WriteAllTextAsync(_hinosLarPath, json);

            var meta = new ScraperMeta { LastFetchUtc = DateTime.UtcNow };
            var metaJson = JsonSerializer.Serialize(meta, JsonOptions);
            await File.WriteAllTextAsync(_metaPath, metaJson);
        }

        return hinos;
    }

    private class ScraperMeta
    {
        public DateTime LastFetchUtc { get; set; }
    }
}
