// ============================================================
// Services/Services.cs
// Lógica de negócio extraída do app.py
// ============================================================
using System.Text.Json;
using AtasApi.Data;
using AtasApi.DTOs;
using AtasApi.Middleware;
using AtasApi.Models;
using AtasApi.Repositories;

namespace AtasApi.Services;

// ─────────────────────────────────────────
// Helper: serialização/desserialização JSON
// (campos como anuncios, hinos, batizados são JSON no banco)
// ─────────────────────────────────────────
public static class JsonFieldHelper
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static string? Serialize<T>(T? value) =>
        value is null ? null : JsonSerializer.Serialize(value, Opts);

    public static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(json, Opts) ?? []; }
        catch { return json.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(); }
    }

    public static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, Opts); }
        catch { return null; }
    }

    /// <summary>Serializa [abertura, encerramento] no formato do campo 'hinos'.</summary>
    public static string SerializeHinos(string? abertura, string? encerramento) =>
        JsonSerializer.Serialize(new[] { abertura ?? "", encerramento ?? "" });

    /// <summary>Serializa [abertura, encerramento] no formato do campo 'oracoes'.</summary>
    public static string SerializeOracoes(string? abertura, string? encerramento) =>
        JsonSerializer.Serialize(new[] { abertura ?? "", encerramento ?? "" });
}

// ─────────────────────────────────────────
// AuthService
// ─────────────────────────────────────────
public interface IAuthService
{
    /// <summary>Valida username + password hash e retorna LoginResponse com JWT.</summary>
    Task<LoginResponse?> LoginAsync(string username, string password);
    /// <summary>Registra novo usuário usando chave de convite.</summary>
    Task<LoginResponse?> RegisterAsync(string username, string password, string inviteKey);
    /// <summary>Troca a senha do usuário logado (valida a senha atual).</summary>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    /// <summary>Redefine a senha de qualquer usuário (uso administrativo).</summary>
    Task<bool?> AdminResetPasswordAsync(string username, string newPassword);
}

public class AuthService(IUserRepository userRepo, IAlaKeyRepository alaKeyRepo, IJwtService jwt) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var user = await userRepo.GetByUsernameAsync(username);
        if (user is null) return null;

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        if (!isPasswordValid) return null;

        var token = jwt.GenerateToken(user.Id, user.Username, user.AlaId, user.Role);
        return new LoginResponse(user.Id, user.Username, token, DateTime.UtcNow.AddDays(7),
            user.AlaId, user.Role, user.DisplayName, AlaCatalog.GetName(user.AlaId));
    }

    public async Task<LoginResponse?> RegisterAsync(string username, string password, string inviteKey)
    {
        // Check if username already exists
        var existing = await userRepo.GetByUsernameAsync(username.Trim());
        if (existing is not null) return null;

        // Validate invite key
        var key = await alaKeyRepo.GetByKeyAsync(inviteKey.Trim());
        if (key is null) return null;

        // Create user
        var user = new User
        {
            Username = username.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            AlaId = key.AlaId,
            Role = key.Role,
            DisplayName = null
        };
        user.Id = await userRepo.CreateAsync(user);

        var token = jwt.GenerateToken(user.Id, user.Username, user.AlaId, user.Role);
        return new LoginResponse(user.Id, user.Username, token, DateTime.UtcNow.AddDays(7),
            user.AlaId, user.Role, user.DisplayName, AlaCatalog.GetName(user.AlaId));
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password)) return false;
        if (currentPassword == newPassword) return false;

        return await userRepo.UpdatePasswordAsync(userId, BCrypt.Net.BCrypt.HashPassword(newPassword));
    }

    public async Task<bool?> AdminResetPasswordAsync(string username, string newPassword)
    {
        var cleanUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(cleanUsername)) return null;

        var user = await userRepo.GetByUsernameAsync(cleanUsername);
        if (user is null) return null;

        var updated = await userRepo.UpdatePasswordAsync(user.Id, BCrypt.Net.BCrypt.HashPassword(newPassword));
        return updated;
    }
}

// ─────────────────────────────────────────
// UsuarioService
// ─────────────────────────────────────────
public interface IUsuarioService
{
    /// <summary>Usuários da ala visíveis para a role (tags/filtros de tarefas).</summary>
    Task<IEnumerable<UsuarioResponse>> GetByAlaAsync(int alaId, string role);
    /// <summary>Atualiza o primeiro nome exibido do usuário.</summary>
    Task<UsuarioResponse?> UpdateDisplayNameAsync(int userId, string? displayName);
}

public class UsuarioService(IUserRepository userRepo) : IUsuarioService
{
    public async Task<IEnumerable<UsuarioResponse>> GetByAlaAsync(int alaId, string role)
    {
        var users = await userRepo.GetByAlaAsync(alaId, role);
        return users.Select(u => new UsuarioResponse(u.Id, u.Username, u.DisplayName, u.Role));
    }

    public async Task<UsuarioResponse?> UpdateDisplayNameAsync(int userId, string? displayName)
    {
        if (!await userRepo.UpdateDisplayNameAsync(userId, displayName)) return null;
        var user = await userRepo.GetByIdAsync(userId);
        return user is null ? null : new UsuarioResponse(user.Id, user.Username, user.DisplayName, user.Role);
    }
}

// ─────────────────────────────────────────
// AtaService
// ─────────────────────────────────────────
public interface IAtaService
{
    Task<IEnumerable<AtaResponse>> GetByMesAsync(int alaId, string anoMes);
    Task<IEnumerable<AtaResponse>> GetAllAsync(int alaId);
    Task<AtaResponse?> GetByIdAsync(int id, int alaId);
    Task<AtaResponse> CreateAsync(int alaId, CreateAtaRequest req);
    Task<AtaResponse?> UpdateAsync(int id, int alaId, CreateAtaRequest req);
    Task DeleteAsync(int id, int alaId);
    Task<AtaResponse?> GetByDataTipoAsync(string data, string tipo, int alaId);
}

public class AtaService(IAtaRepository ataRepo) : IAtaService
{
    public async Task<IEnumerable<AtaResponse>> GetByMesAsync(int alaId, string anoMes) =>
        (await ataRepo.GetByMesAsync(alaId, anoMes)).Select(Map);

    public async Task<IEnumerable<AtaResponse>> GetAllAsync(int alaId) =>
        (await ataRepo.GetAllByAlaAsync(alaId)).Select(Map);

    public async Task<AtaResponse?> GetByIdAsync(int id, int alaId)
    {
        var ata = await ataRepo.GetByIdAsync(id, alaId);
        return ata is null ? null : Map(ata);
    }

    public async Task<AtaResponse> CreateAsync(int alaId, CreateAtaRequest req)
    {
        var status = req.Tipo == "sacramental" ? "rascunho" : "pendente";
        var ata = new Ata { Tipo = req.Tipo, Data = req.Data, Status = status, AlaId = alaId };
        ata.Id = await ataRepo.CreateAsync(ata);
        return Map(ata);
    }

    public async Task<AtaResponse?> UpdateAsync(int id, int alaId, CreateAtaRequest req)
    {
        var ata = await ataRepo.GetByIdAsync(id, alaId);
        if (ata is null) return null;
        ata.Tipo = req.Tipo;
        ata.Data = req.Data;
        await ataRepo.UpdateAsync(ata);
        return Map(ata);
    }

    public async Task DeleteAsync(int id, int alaId)
    {
        await ataRepo.DeleteAsync(id, alaId);
    }

    public async Task<AtaResponse?> GetByDataTipoAsync(string data, string tipo, int alaId)
    {
        var ata = await ataRepo.GetByDataTipoAsync(data, tipo, alaId);
        return ata is null ? null : Map(ata);
    }

    private static AtaResponse Map(Ata a) => new(a.Id, a.Tipo, a.Data, a.Status, a.AlaId);
}

// ─────────────────────────────────────────
// SacramentalService
// ─────────────────────────────────────────
public interface ISacramentalService
{
    Task<SacramentalResponse?> GetByAtaIdAsync(int ataId);
    Task<SacramentalResponse> CreateAsync(CreateSacramentalRequest req);
    Task<SacramentalResponse?> UpdateAsync(int ataId, CreateSacramentalRequest req);
    Task DeleteByAtaIdAsync(int ataId);

    /// <summary>
    /// Equivale ao get_discursantes_recentes() do Python:
    /// retorna os discursantes dos últimos N dias agrupados por data.
    /// </summary>
    Task<IEnumerable<DiscursantesStateResponse>> GetDiscursantesRecentesAsync(int alaId, int dias = 90);

    /// <summary>Retorna lista de discursantes únicos com última data e posição.</summary>
    Task<IEnumerable<DiscursanteSugestao>> GetSugestoesAsync(int alaId);

    /// <summary>Busca ou cria o registro sacramental de uma data, salvando discursantes/hinos.</summary>
    Task<DiscursantesStateResponse> SaveDiscursantesAsync(int alaId, SaveDiscursantesRequest req,
        IAtaRepository ataRepo);
}

public class SacramentalService(ISacramentalRepository sacRepo) : ISacramentalService
{
    public async Task<SacramentalResponse?> GetByAtaIdAsync(int ataId)
    {
        var s = await sacRepo.GetByAtaIdAsync(ataId);
        return s is null ? null : Map(s);
    }

    public async Task<SacramentalResponse> CreateAsync(CreateSacramentalRequest req)
    {
        var s = ToEntity(req);
        s.Id = await sacRepo.CreateAsync(s);
        return Map(s);
    }

    public async Task<SacramentalResponse?> UpdateAsync(int ataId, CreateSacramentalRequest req)
    {
        var existing = await sacRepo.GetByAtaIdAsync(ataId);
        if (existing is null) return null;
        var s = ToEntity(req);
        s.Id = existing.Id;
        await sacRepo.UpdateAsync(s);
        return Map(s);
    }

    public async Task DeleteByAtaIdAsync(int ataId) =>
        await sacRepo.DeleteByAtaIdAsync(ataId);

    public async Task<IEnumerable<DiscursantesStateResponse>> GetDiscursantesRecentesAsync(int alaId, int dias = 90)
    {
        var dataLimite = DateTime.Now.AddDays(-dias).ToString("yyyy-MM-dd");
        var recentes = await sacRepo.GetRecentesAsync(alaId, dataLimite);

        return recentes.Select(s => new DiscursantesStateResponse
        {
            AtaId = s.AtaId,
            Date = s.Date,
            Discursante1 = s.Discursante1,
            Discursante2 = s.Discursante2,
            Discursante3 = s.UltimoDiscursante,
            Tema = s.Tema,
            HinoAbertura = s.Hinos != null ? JsonFieldHelper.DeserializeList(s.Hinos).ElementAtOrDefault(0) : null,
            HinoEncerramento = s.Hinos != null ? JsonFieldHelper.DeserializeList(s.Hinos).ElementAtOrDefault(1) : null,
            HinoSacramental = s.HinoSacramental,
            HinoIntermediario = s.HinoIntermediario
        });
    }

    public async Task<IEnumerable<DiscursanteSugestao>> GetSugestoesAsync(int alaId)
    {
        return await sacRepo.GetSugestoesAsync(alaId);
    }

    public async Task<DiscursantesStateResponse> SaveDiscursantesAsync(
        int alaId, SaveDiscursantesRequest req, IAtaRepository ataRepo)
    {
        // Busca ou cria a ata sacramental da data
        var ata = await ataRepo.GetByDataTipoAsync(req.Date, "sacramental", alaId);
        int ataId;
        if (ata is null)
        {
            var nova = new Ata { Tipo = "sacramental", Data = req.Date, AlaId = alaId, Status = "rascunho" };
            ataId = await ataRepo.CreateAsync(nova);
        }
        else
        {
            ataId = ata.Id;
        }

        // Busca ou cria o sacramental
        var existing = await sacRepo.GetByAtaIdAsync(ataId);
        var hinosJson = JsonFieldHelper.SerializeHinos(req.HinoAbertura, req.HinoEncerramento);

        if (existing is null)
        {
            await sacRepo.CreateAsync(new Sacramental
            {
                AtaId = ataId,
                Tema = req.Tema,
                Discursante1 = req.Discursante1,
                Discursante2 = req.Discursante2,
                UltimoDiscursante = req.Discursante3,
                Outros = req.Outros,
                Tema1 = req.Tema1, Tema2 = req.Tema2, TemaUltimo = req.Tema3,
                Obs1 = req.Obs1, Obs2 = req.Obs2, ObsUltimo = req.Obs3,
                Hinos = hinosJson,
                HinoSacramental = req.HinoSacramental,
                HinoIntermediario = req.HinoIntermediario
            });
        }
        else
        {
            existing.Tema = req.Tema;
            existing.Discursante1 = req.Discursante1;
            existing.Discursante2 = req.Discursante2;
            existing.UltimoDiscursante = req.Discursante3;
            existing.Outros = req.Outros;
            existing.Tema1 = req.Tema1; existing.Tema2 = req.Tema2; existing.TemaUltimo = req.Tema3;
            existing.Obs1 = req.Obs1; existing.Obs2 = req.Obs2; existing.ObsUltimo = req.Obs3;
            existing.Hinos = hinosJson;
            existing.HinoSacramental = req.HinoSacramental;
            existing.HinoIntermediario = req.HinoIntermediario;
            await sacRepo.UpdateAsync(existing);
        }

        // Status da ata sacramental: só é concluída com os dois discursantes preenchidos.
        await ataRepo.UpdateStatusAsync(ataId, StatusParaDiscursantes(req.Discursante1, req.Discursante2));

        return new DiscursantesStateResponse
        {
            AtaId = ataId, Date = req.Date,
            Discursante1 = req.Discursante1, Discursante2 = req.Discursante2, Discursante3 = req.Discursante3,
            Tema = req.Tema, Tema1 = req.Tema1, Tema2 = req.Tema2, Tema3 = req.Tema3,
            Obs1 = req.Obs1, Obs2 = req.Obs2, Obs3 = req.Obs3,
            HinoAbertura = req.HinoAbertura, HinoSacramental = req.HinoSacramental,
            HinoIntermediario = req.HinoIntermediario, HinoEncerramento = req.HinoEncerramento
        };
    }

    /// <summary>
    /// Uma ata sacramental só está concluída quando os dois discursantes
    /// (1º e 2º) estão preenchidos. Caso contrário permanece como rascunho.
    /// </summary>
    public static string StatusParaDiscursantes(string? discursante1, string? discursante2)
    {
        bool preenchido1 = !string.IsNullOrWhiteSpace(discursante1);
        bool preenchido2 = !string.IsNullOrWhiteSpace(discursante2);
        return preenchido1 && preenchido2 ? "concluida" : "rascunho";
    }

    /// <summary>
    /// Reunião de jejum/testemunhos (primeiro domingo): não há discursantes,
    /// então a ata só é concluída quando há pelo menos um testemunho registrado.
    /// </summary>
    public static string StatusParaPrimeiroDomingo(List<string>? testemunhos)
    {
        bool algum = testemunhos?.Any(t => !string.IsNullOrWhiteSpace(t)) == true;
        return algum ? "concluida" : "rascunho";
    }

    // ── Mapeamento Entity → DTO ──
    private static SacramentalResponse Map(Sacramental s)
    {
        var hinosRaw = JsonFieldHelper.DeserializeList(s.Hinos);
        var oracoesRaw = JsonFieldHelper.DeserializeList(s.Oracoes);
        return new SacramentalResponse
        {
            Id = s.Id, AtaId = s.AtaId,
            Presidido = s.Presidido, Dirigido = s.Dirigido,
            Pianista = s.Pianista, RegenteMusica = s.RegentMusica,
            Anuncios = JsonFieldHelper.DeserializeList(s.Anuncios),
            HinoAbertura = hinosRaw.ElementAtOrDefault(0),
            HinoEncerramento = hinosRaw.ElementAtOrDefault(1),
            HinoSacramental = s.HinoSacramental, HinoIntermediario = s.HinoIntermediario,
            OracaoAbertura = oracoesRaw.ElementAtOrDefault(0),
            OracaoEncerramento = oracoesRaw.ElementAtOrDefault(1),
            Recepcionistas = s.Recepcionistas,
            ReconhecemosPresenca = JsonFieldHelper.DeserializeList(s.ReconhecemosPresenca),
            Desobrigacoes = JsonFieldHelper.Deserialize<List<ChamadoItem>>(s.Desobrigacoes) ?? [],
            Apoios = JsonFieldHelper.Deserialize<List<ChamadoItem>>(s.Apoios) ?? [],
            ConfirmacoesBatismo = JsonFieldHelper.DeserializeList(s.ConfirmacoesBatismo),
            ApoioMembros = JsonFieldHelper.DeserializeList(s.ApoioMembros),
            BencaoCriancas = JsonFieldHelper.DeserializeList(s.BencaoCriancas),
            Testemunhos = JsonFieldHelper.DeserializeList(s.Testemunhos),
            Tema = s.Tema,
            Discursante1 = s.Discursante1, Discursante2 = s.Discursante2,
            UltimoDiscursante = s.UltimoDiscursante, Outros = s.Outros,
            Tema1 = s.Tema1, Tema2 = s.Tema2, TemaUltimo = s.TemaUltimo,
            Obs1 = s.Obs1, Obs2 = s.Obs2, ObsUltimo = s.ObsUltimo,
        };
    }

    // ── Mapeamento DTO → Entity ──
    private static Sacramental ToEntity(CreateSacramentalRequest req) => new()
    {
        AtaId = req.AtaId,
        Presidido = req.Presidido, Dirigido = req.Dirigido,
        Pianista = req.Pianista, RegentMusica = req.RegenteMusica,
        Anuncios = JsonFieldHelper.Serialize(req.Anuncios),
        Hinos = JsonFieldHelper.SerializeHinos(req.HinoAbertura, req.HinoEncerramento),
        Oracoes = JsonFieldHelper.SerializeOracoes(req.OracaoAbertura, req.OracaoEncerramento),
        HinoSacramental = req.HinoSacramental, HinoIntermediario = req.HinoIntermediario,
        Recepcionistas = req.Recepcionistas,
        ReconhecemosPresenca = JsonFieldHelper.Serialize(req.ReconhecemosPresenca),
        Desobrigacoes = JsonFieldHelper.Serialize(req.Desobrigacoes),
        Apoios = JsonFieldHelper.Serialize(req.Apoios),
        ConfirmacoesBatismo = JsonFieldHelper.Serialize(req.ConfirmacoesBatismo),
        ApoioMembros = JsonFieldHelper.Serialize(req.ApoioMembros),
        BencaoCriancas = JsonFieldHelper.Serialize(req.BencaoCriancas),
        Testemunhos = JsonFieldHelper.Serialize(req.Testemunhos),
        Tema = req.Tema,
        Discursante1 = req.Discursante1, Discursante2 = req.Discursante2,
        UltimoDiscursante = req.UltimoDiscursante, Outros = req.Outros,
        Tema1 = req.Tema1, Tema2 = req.Tema2, TemaUltimo = req.TemaUltimo,
        Obs1 = req.Obs1, Obs2 = req.Obs2, ObsUltimo = req.ObsUltimo
    };
}

// ─────────────────────────────────────────
// BatismoService
// ─────────────────────────────────────────
public interface IBatismoService
{
    Task<BatismoResponse?> GetByAtaIdAsync(int ataId);
    Task<BatismoResponse> CreateAsync(CreateBatismoRequest req);
    Task<BatismoResponse?> UpdateAsync(int ataId, CreateBatismoRequest req);
    Task DeleteByAtaIdAsync(int ataId);
}

public class BatismoService(IBatismoRepository batRepo) : IBatismoService
{
    public async Task<BatismoResponse?> GetByAtaIdAsync(int ataId)
    {
        var b = await batRepo.GetByAtaIdAsync(ataId);
        return b is null ? null : Map(b);
    }

    public async Task<BatismoResponse> CreateAsync(CreateBatismoRequest req)
    {
        var b = ToEntity(req);
        b.Id = await batRepo.CreateAsync(b);
        return Map(b);
    }

    public async Task<BatismoResponse?> UpdateAsync(int ataId, CreateBatismoRequest req)
    {
        var existing = await batRepo.GetByAtaIdAsync(ataId);
        if (existing is null) return null;
        var b = ToEntity(req);
        b.Id = existing.Id;
        await batRepo.UpdateAsync(b);
        return Map(b);
    }

    public async Task DeleteByAtaIdAsync(int ataId) => await batRepo.DeleteByAtaIdAsync(ataId);

    private static BatismoResponse Map(Batismo b)
    {
        var batizados = JsonFieldHelper.Deserialize<List<BatizadoItem>>(b.Batizados) ?? [];
        var programa = JsonFieldHelper.Deserialize<ProgramaBatismoDto>(b.Programa);
        return new BatismoResponse
        {
            Id = b.Id, AtaId = b.AtaId,
            Dedicado = b.Dedicado, Presidido = b.Presidido, Dirigido = b.Dirigido,
            Batizados = batizados,
            Testemunha1 = b.Testemunha1, Testemunha2 = b.Testemunha2,
            Programa = programa
        };
    }

    private static Batismo ToEntity(CreateBatismoRequest req) => new()
    {
        AtaId = req.AtaId,
        Dedicado = req.Dedicado, Presidido = req.Presidido, Dirigido = req.Dirigido,
        Batizados = JsonFieldHelper.Serialize(req.Batizados),
        Testemunha1 = req.Testemunha1, Testemunha2 = req.Testemunha2,
        Programa = JsonFieldHelper.Serialize(req.Programa)
    };
}

// ─────────────────────────────────────────
// TemplateService
// ─────────────────────────────────────────
public interface ITemplateService
{
    Task<IEnumerable<Template>> GetTemplatesAsync(int alaId);
    Task<Template?> GetByIdAsync(int id);
    Task<Template> CreateAsync(int alaId, SaveTemplateRequest req);
    Task<Template?> UpdateAsync(int id, int alaId, SaveTemplateRequest req);
    Task<bool> DeleteAsync(int id, int alaId);
}

public class TemplateService(ITemplateRepository templateRepo) : ITemplateService
{
    public async Task<IEnumerable<Template>> GetTemplatesAsync(int alaId)
    {
        var templates = (await templateRepo.GetByAlaAsync(alaId)).ToList();

        // Clona os padrões se a ala ainda não tem nenhum (equivale ao bloco do configuracoes())
        if (templates.Count == 0)
        {
            await templateRepo.CloneDefaultsForAlaAsync(alaId);
            templates = (await templateRepo.GetByAlaAsync(alaId)).ToList();
        }

        return templates;
    }

    public Task<Template?> GetByIdAsync(int id) => templateRepo.GetByIdAsync(id);

    public async Task<Template> CreateAsync(int alaId, SaveTemplateRequest req)
    {
        var t = MapToEntity(req, alaId);
        t.Id = await templateRepo.CreateAsync(t);
        return t;
    }

    public async Task<Template?> UpdateAsync(int id, int alaId, SaveTemplateRequest req)
    {
        var existing = await templateRepo.GetByIdAsync(id);
        if (existing is null || existing.AlaId != alaId) return null;
        var t = MapToEntity(req, alaId);
        t.Id = id;
        await templateRepo.UpdateAsync(t);
        return t;
    }

    public async Task<bool> DeleteAsync(int id, int alaId)
    {
        var t = await templateRepo.GetByIdAsync(id);
        if (t is null || t.AlaId != alaId) return false;
        await templateRepo.DeleteAsync(id);
        return true;
    }

    private static Template MapToEntity(SaveTemplateRequest req, int alaId) => new()
    {
        AlaId = alaId, TipoTemplate = req.TipoTemplate, Nome = req.Nome,
        BoasVindas = req.BoasVindas, Desobrigacoes = req.Desobrigacoes, Apoios = req.Apoios,
        ConfirmacoesBatismo = req.ConfirmacoesBatismo, ApoioMembroNovo = req.ApoioMembroNovo,
        BencaoCrianca = req.BencaoCrianca, Ordenacoes = req.Ordenacoes, Sacramento = req.Sacramento,
        DesobrigacoesPlural = req.DesobrigacoesPlural, ApoiosPlural = req.ApoiosPlural,
        ConfirmacoesBatismoPlural = req.ConfirmacoesBatismoPlural, ApoioMembroNovoPlural = req.ApoioMembroNovoPlural,
        BencaoCriancaPlural = req.BencaoCriancaPlural, OrdenacoesPlural = req.OrdenacoesPlural,
        Mensagens = req.Mensagens, Live = req.Live, Encerramento = req.Encerramento
    };
}

// ─────────────────────────────────────────
// UnidadeService
// ─────────────────────────────────────────
public interface IUnidadeService
{
    Task<UnidadeResponse?> GetAsync(int alaId);
    Task<UnidadeResponse> UpsertAsync(int alaId, SaveUnidadeRequest req);
}

public class UnidadeService(IUnidadeRepository unidadeRepo) : IUnidadeService
{
    public async Task<UnidadeResponse?> GetAsync(int alaId)
    {
        var u = await unidadeRepo.GetByAlaAsync(alaId);
        return u is null ? null : Map(u);
    }

    public async Task<UnidadeResponse> UpsertAsync(int alaId, SaveUnidadeRequest req)
    {
        var u = new Unidade
        {
            AlaId = alaId, Nome = req.Nome, Bispo = req.Bispo,
            PrimeiroConselheiro = req.PrimeiroConselheiro,
            SegundoConselheiro = req.SegundoConselheiro,
            Recepcionista = req.Recepcionista, Pianista = req.Pianista,
            RegenteMusica = req.RegenteMusica, Horario = req.Horario,
            Secretario1 = req.Secretario1, Secretario2 = req.Secretario2,
            Secretario3 = req.Secretario3, Secretario4 = req.Secretario4,
            EstacaId = 1
        };
        await unidadeRepo.UpsertAsync(u);
        return Map(u);
    }

    private static UnidadeResponse Map(Unidade u) =>
        new(u.Id, u.AlaId, u.Nome, u.Bispo, u.PrimeiroConselheiro,
            u.SegundoConselheiro, u.EstacaId, u.Horario,
            u.Recepcionista, u.Pianista, u.RegenteMusica,
            u.Secretario1, u.Secretario2, u.Secretario3, u.Secretario4);
}

// ─────────────────────────────────────────
// TarefaService
// ─────────────────────────────────────────
public interface ITarefaService
{
    Task<IEnumerable<TarefaResponse>> GetAllAsync(int alaId, string role);
    Task<TarefaResponse?> GetByIdAsync(int id, int alaId, string role);
    Task<TarefaResponse> CreateAsync(int alaId, string role, CreateTarefaRequest req);
    Task<TarefaResponse?> UpdateAsync(int id, int alaId, string role, UpdateTarefaRequest req);
    Task<bool> DeleteAsync(int id, int alaId, string role);
}

public class TarefaService(ITarefaRepository tarefaRepo) : ITarefaService
{
    public async Task<IEnumerable<TarefaResponse>> GetAllAsync(int alaId, string role) =>
        (await tarefaRepo.GetByAlaAsync(alaId, role)).Select(Map);

    /// <summary>
    /// Regra de visibilidade de tarefas por role:
    /// bispo vê tudo; conselheiros veem tarefas de conselheiros;
    /// secretários veem somente tarefas de secretários.
    /// </summary>
    private static bool PodeVer(string callerRole, string tarefaRole) => callerRole switch
    {
        "bispo" => true,
        "conselheiro_1" or "conselheiro_2" => tarefaRole is "conselheiro_1" or "conselheiro_2",
        "secretario" => tarefaRole == "secretario",
        _ => false
    };

    public async Task<TarefaResponse?> GetByIdAsync(int id, int alaId, string role)
    {
        var t = await tarefaRepo.GetByIdAsync(id, alaId);
        return t is null || !PodeVer(role, t.Role) ? null : Map(t);
    }

    public async Task<TarefaResponse> CreateAsync(int alaId, string role, CreateTarefaRequest req)
    {
        var t = new Tarefa
        {
            Titulo = req.Titulo,
            Responsavel = req.Responsavel,
            DataPrevista = req.DataPrevista,
            Concluida = false,
            CriadaEm = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            AlaId = alaId,
            Role = role
        };
        t.Id = await tarefaRepo.CreateAsync(t);
        return Map(t);
    }

    public async Task<TarefaResponse?> UpdateAsync(int id, int alaId, string role, UpdateTarefaRequest req)
    {
        var existing = await tarefaRepo.GetByIdAsync(id, alaId);
        if (existing is null || !PodeVer(role, existing.Role)) return null;

        if (req.Titulo is not null) existing.Titulo = req.Titulo;
        if (req.Responsavel is not null) existing.Responsavel = req.Responsavel;
        if (req.DataPrevista is not null) existing.DataPrevista = req.DataPrevista;
        if (req.Concluida.HasValue)
        {
            existing.Concluida = req.Concluida.Value;
            existing.ConcluidaEm = req.Concluida.Value
                ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                : null;
        }

        await tarefaRepo.UpdateAsync(existing);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(int id, int alaId, string role)
    {
        var existing = await tarefaRepo.GetByIdAsync(id, alaId);
        if (existing is null || !PodeVer(role, existing.Role)) return false;
        await tarefaRepo.DeleteAsync(id, alaId);
        return true;
    }

    private static TarefaResponse Map(Tarefa t) =>
        new(t.Id, t.Titulo, t.Concluida, t.Responsavel, t.DataPrevista,
            t.ConcluidaEm, t.CriadaEm, t.AlaId, t.Role);
}
