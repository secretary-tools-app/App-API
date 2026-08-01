using System.Text.Json;

namespace AtasApi.Hinos;

public interface IHinoService
{
    Task<List<Hino>> GetAllAsync(string? busca);
}

public class HinoService : IHinoService
{
    private readonly string _hinosPath;
    private readonly string _hinosLarPath;
    private readonly IHinoScraperService _scraper;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public HinoService(IWebHostEnvironment env, IHinoScraperService scraper)
    {
        _hinosPath = Path.Combine(env.ContentRootPath, "public", "hinos", "hinos.json");
        _hinosLarPath = Path.Combine(env.ContentRootPath, "public", "hinos", "hinosParaOLar.json");
        _scraper = scraper;
    }

    public async Task<List<Hino>> GetAllAsync(string? busca)
    {
        if (await _scraper.NeedsRefreshAsync())
        {
            try
            {
                await _scraper.ScrapeHinosLarAsync();
            }
            catch
            {
                // Se o scraping falhar, segue com o que tem no disco
            }
        }

        var hinos = await ReadJsonAsync(_hinosPath);
        var hinosLar = await ReadJsonAsync(_hinosLarPath);

        hinos.AddRange(hinosLar);

        if (string.IsNullOrWhiteSpace(busca))
            return hinos;

        var termo = busca.Trim().ToLowerInvariant();

        return hinos.Where(h =>
            h.Nome.ToLowerInvariant().Contains(termo) ||
            h.Numero.ToString().Contains(termo)
        ).ToList();
    }

    private static async Task<List<Hino>> ReadJsonAsync(string path)
    {
        if (!File.Exists(path))
            return new List<Hino>();

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<Hino>>(json, JsonOptions) ?? new List<Hino>();
    }
}
