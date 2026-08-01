// ============================================================
// Data/Alas.cs
// Catálogo das alas fixas (nome de exibição por alaId).
// ============================================================

namespace AtasApi.Data;

public static class AlaCatalog
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [1] = "Criciúma 1",
        [2] = "Criciúma 2",
        [3] = "Criciúma 3",
        [4] = "Içara",
        [5] = "Araranguá",
        [6] = "Obra",
    };

    public static string GetName(int alaId) =>
        Names.TryGetValue(alaId, out var n) ? n : $"Ala {alaId}";
}
