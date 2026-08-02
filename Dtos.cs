// ============================================================
// DTOs/Requests.cs  +  DTOs/Responses.cs (arquivo único)
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace AtasApi.DTOs;

// ──────────────────────────────────────────
// AUTH
// ──────────────────────────────────────────
public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record LoginResponse(
    int Id,
    string Username,
    string Token,          // JWT
    DateTime ExpiresAt,
    int AlaId,
    string Role,
    string? DisplayName,
    string AlaName
);

public record RegisterRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, StringLength(64)] string InviteKey
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword
);

public record AdminResetPasswordRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, MinLength(8), MaxLength(128)] string NewPassword
);

public record UsuarioResponse(
    int Id,
    string Username,
    string? DisplayName,
    string Role
);

public record UpdateProfileRequest(
    string? DisplayName
);

// ──────────────────────────────────────────
// ATAS
// ──────────────────────────────────────────
public record CreateAtaRequest(
    [Required] string Tipo,   // "sacramental" | "batismo"
    [Required] string Data    // "YYYY-MM-DD"
);

public record AtaResponse(
    int Id,
    string Tipo,
    string Data,
    string Status,
    int AlaId
);

// ──────────────────────────────────────────
// SACRAMENTAL
// ──────────────────────────────────────────
/// <summary>Item de apoio/desobrigação: a pessoa e o chamado/cargo.</summary>
public record ChamadoItem(string Nome, string Chamado);

public record CreateSacramentalRequest
{
    public int AtaId { get; init; }
    public string? Presidido { get; init; }
    public string? Dirigido { get; init; }
    public string? Pianista { get; init; }
    public string? RegenteMusica { get; init; }
    public List<string>? Anuncios { get; init; }
    public string? HinoAbertura { get; init; }
    public string? HinoEncerramento { get; init; }
    public string? HinoSacramental { get; init; }
    public string? HinoIntermediario { get; init; }
    public string? OracaoAbertura { get; init; }
    public string? OracaoEncerramento { get; init; }
    public string? Recepcionistas { get; init; }
    public List<string>? ReconhecemosPresenca { get; init; }
    public List<ChamadoItem>? Desobrigacoes { get; init; }
    public List<ChamadoItem>? Apoios { get; init; }
    public List<string>? ConfirmacoesBatismo { get; init; }
    public List<string>? ApoioMembros { get; init; }
    public List<string>? BencaoCriancas { get; init; }
    public List<string>? Testemunhos { get; init; }
    public string? Tema { get; init; }
    public string? Discursante1 { get; init; }
    public string? Discursante2 { get; init; }
    public string? UltimoDiscursante { get; init; }
    public string? Outros { get; init; }
    public string? Tema1 { get; init; }
    public string? Tema2 { get; init; }
    public string? TemaUltimo { get; init; }
    public string? Obs1 { get; init; }
    public string? Obs2 { get; init; }
    public string? ObsUltimo { get; init; }
}

// Response: expande os campos JSON para listas nativas
public record SacramentalResponse
{
    public int Id { get; init; }
    public int AtaId { get; init; }
    public string? Presidido { get; init; }
    public string? Dirigido { get; init; }
    public string? Pianista { get; init; }
    public string? RegenteMusica { get; init; }
    public List<string> Anuncios { get; init; } = [];
    public string? HinoAbertura { get; init; }
    public string? HinoEncerramento { get; init; }
    public string? HinoSacramental { get; init; }
    public string? HinoIntermediario { get; init; }
    public string? OracaoAbertura { get; init; }
    public string? OracaoEncerramento { get; init; }
    public string? Recepcionistas { get; init; }
    public List<string> ReconhecemosPresenca { get; init; } = [];
    public List<ChamadoItem> Desobrigacoes { get; init; } = [];
    public List<ChamadoItem> Apoios { get; init; } = [];
    public List<string> ConfirmacoesBatismo { get; init; } = [];
    public List<string> ApoioMembros { get; init; } = [];
    public List<string> BencaoCriancas { get; init; } = [];
    public List<string> Testemunhos { get; init; } = [];
    public string? Tema { get; init; }
    public string? Discursante1 { get; init; }
    public string? Discursante2 { get; init; }
    public string? UltimoDiscursante { get; init; }
    public string? Outros { get; init; }
    public string? Tema1 { get; init; }
    public string? Tema2 { get; init; }
    public string? TemaUltimo { get; init; }
    public string? Obs1 { get; init; }
    public string? Obs2 { get; init; }
    public string? ObsUltimo { get; init; }
}

// ──────────────────────────────────────────
// BATISMO
// ──────────────────────────────────────────

// Batizado pode ser simples (string) ou estruturado (dict)
public record BatizadoItem(
    string Nome,
    string? Batizador = null
);

public record CreateBatismoRequest
{
    public int AtaId { get; init; }
    public string? Dedicado { get; init; }
    public string? Presidido { get; init; }
    public string? Dirigido { get; init; }
    public List<BatizadoItem>? Batizados { get; init; }
    public string? Testemunha1 { get; init; }
    public string? Testemunha2 { get; init; }
    public ProgramaBatismoDto? Programa { get; init; }
}

public record ProgramaBatismoDto(
    string? Preludio,
    string? BoasVindasPor,
    string? HinoAbertura,
    string? OracaoAbertura,
    List<string>? Mensagens,
    string? ApresentacaoMusical,
    bool TemEspera,
    List<string>? HinosEspera,
    List<BatizadoItem>? Batizados,
    List<string>? Confirmacoes,
    string? TestemunhosNovos,
    string? HinoEncerramento,
    string? OracaoEncerramento,
    string? Posludio,
    string? Observacoes
);

public record BatismoResponse
{
    public int Id { get; init; }
    public int AtaId { get; init; }
    public string? Dedicado { get; init; }
    public string? Presidido { get; init; }
    public string? Dirigido { get; init; }
    public List<BatizadoItem> Batizados { get; init; } = [];
    public string? Testemunha1 { get; init; }
    public string? Testemunha2 { get; init; }
    public ProgramaBatismoDto? Programa { get; init; }
}

// ──────────────────────────────────────────
// DISCURSANTES & TEMAS (página de planejamento)
// ──────────────────────────────────────────
public record SaveDiscursantesRequest
{
    [Required] public string Date { get; init; } = string.Empty; // "YYYY-MM-DD"
    public string? Tema { get; init; }
    public string? Discursante1 { get; init; }
    public string? Discursante2 { get; init; }
    public string? Discursante3 { get; init; }  // ultimo_discursante
    public string? Outros { get; init; }
    public string? Tema1 { get; init; }
    public string? Tema2 { get; init; }
    public string? Tema3 { get; init; }
    public string? Obs1 { get; init; }
    public string? Obs2 { get; init; }
    public string? Obs3 { get; init; }
    public string? HinoAbertura { get; init; }
    public string? HinoSacramental { get; init; }
    public string? HinoIntermediario { get; init; }
    public string? HinoEncerramento { get; init; }
    public string? OracaoAbertura { get; init; }
    public string? OracaoEncerramento { get; init; }
}

public record DiscursantesStateResponse
{
    public int? AtaId { get; init; }
    public string? Date { get; init; }
    public string? Discursante1 { get; init; }
    public string? Discursante2 { get; init; }
    public string? Discursante3 { get; init; }
    public string? Tema { get; init; }
    public string? Tema1 { get; init; }
    public string? Tema2 { get; init; }
    public string? Tema3 { get; init; }
    public string? Obs1 { get; init; }
    public string? Obs2 { get; init; }
    public string? Obs3 { get; init; }
    public string? HinoAbertura { get; init; }
    public string? HinoSacramental { get; init; }
    public string? HinoIntermediario { get; init; }
    public string? HinoEncerramento { get; init; }
    public string? OracaoAbertura { get; init; }
    public string? OracaoEncerramento { get; init; }
}

public record DiscursanteSugestao
{
    public string Nome { get; init; } = string.Empty;
    public string? UltimaData { get; init; }
    public string Posicao { get; init; } = string.Empty;
}

// ──────────────────────────────────────────
// TEMPLATE
// ──────────────────────────────────────────
public record SaveTemplateRequest
{
    [Required] public string Nome { get; init; } = string.Empty;
    public int TipoTemplate { get; init; } = 1;
    public string BoasVindas { get; init; } = string.Empty;
    public string Desobrigacoes { get; init; } = string.Empty;
    public string? Apoios { get; init; }
    public string ConfirmacoesBatismo { get; init; } = string.Empty;
    public string ApoioMembroNovo { get; init; } = string.Empty;
    public string BencaoCrianca { get; init; } = string.Empty;
    public string Ordenacoes { get; init; } = string.Empty;
    public string DesobrigacoesPlural { get; init; } = string.Empty;
    public string? ApoiosPlural { get; init; }
    public string ConfirmacoesBatismoPlural { get; init; } = string.Empty;
    public string ApoioMembroNovoPlural { get; init; } = string.Empty;
    public string BencaoCriancaPlural { get; init; } = string.Empty;
    public string OrdenacoesPlural { get; init; } = string.Empty;
    public string Sacramento { get; init; } = string.Empty;
    public string Mensagens { get; init; } = string.Empty;
    public string Live { get; init; } = string.Empty;
    public string Encerramento { get; init; } = string.Empty;
}

// ──────────────────────────────────────────
// UNIDADE (configurações da ala)
// ──────────────────────────────────────────
public record SaveUnidadeRequest(
    string? Nome,
    string? Bispo,
    string? PrimeiroConselheiro,
    string? SegundoConselheiro,
    string? Recepcionista,
    string? Pianista,
    string? RegenteMusica,
    string? Horario,
    string? Secretario1,
    string? Secretario2,
    string? Secretario3,
    string? Secretario4
);

public record UnidadeResponse(
    int Id,
    int AlaId,
    string? Nome,
    string? Bispo,
    string? PrimeiroConselheiro,
    string? SegundoConselheiro,
    int EstacaId,
    string? Horario,
    string? Recepcionista,
    string? Pianista,
    string? RegenteMusica,
    string? Secretario1,
    string? Secretario2,
    string? Secretario3,
    string? Secretario4
);

// ──────────────────────────────────────────
// Respostas genéricas de erro/sucesso
// ──────────────────────────────────────────
public record ApiError(string Message, int StatusCode = 400);
public record ApiSuccess<T>(T Data, string? Message = null);

// ──────────────────────────────────────────
// TAREFAS
// ──────────────────────────────────────────
public record CreateTarefaRequest
{
    [Required] public string Titulo { get; init; } = string.Empty;
    public string? Responsavel { get; init; }
    public string? DataPrevista { get; init; }
}

public record UpdateTarefaRequest
{
    public string? Titulo { get; init; }
    public string? Responsavel { get; init; }
    public string? DataPrevista { get; init; }
    public bool? Concluida { get; init; }
}

public record TarefaResponse(
    int Id,
    string Titulo,
    bool Concluida,
    string? Responsavel,
    string? DataPrevista,
    string? ConcluidaEm,
    string CriadaEm,
    int AlaId,
    string Role
);
