// ============================================================
// Models/Entities.cs
// Mapeamento 1:1 com o schema_inicial.sql
// ============================================================

namespace AtasApi.Models;

// ──────────────────────────────────────────
// users  →  tabela de usuários/bispado
// ──────────────────────────────────────────
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // BCrypt hash
    public int AlaId { get; set; }                         // qual ala pertence
    public string Role { get; set; } = string.Empty;       // bispo, conselheiro_1, conselheiro_2, secretario
    public string? DisplayName { get; set; }                // nome exibido
}

// ──────────────────────────────────────────
// ala_keys  →  chaves de convite por ala
// ──────────────────────────────────────────
public class AlaKey
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;  // chave única
    public int AlaId { get; set; }
    public string Role { get; set; } = string.Empty; // bispo, conselheiro_1, etc.
}

// ──────────────────────────────────────────
// atas  →  tabela principal de atas
// ──────────────────────────────────────────
public class Ata
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;   // "sacramental" | "batismo"
    public string Data { get; set; } = string.Empty;   // "YYYY-MM-DD"
    public string Status { get; set; } = "pendente";
    public int AlaId { get; set; }
}

// ──────────────────────────────────────────
// sacramental  →  detalhes de ata sacramental
// Atenção: vários campos são JSON serializado
// ──────────────────────────────────────────
public class Sacramental
{
    public int Id { get; set; }
    public int AtaId { get; set; }
    public string? Presidido { get; set; }
    public string? Dirigido { get; set; }
    public string? Pianista { get; set; }
    public string? RegentMusica { get; set; }

    // JSON → List<string>
    public string? Anuncios { get; set; }

    // JSON → string[] [abertura, encerramento]
    public string? Hinos { get; set; }

    public string? HinoSacramental { get; set; }
    public string? HinoIntermediario { get; set; }

    // JSON → string[] [abertura, encerramento]
    public string? Oracoes { get; set; }

    // Colunas individuais (novo esquema)
    public string? Discursante1 { get; set; }
    public string? Discursante2 { get; set; }
    public string? Outros { get; set; }
    public string? Tema1 { get; set; }
    public string? Tema2 { get; set; }
    public string? TemaUltimo { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public string? ObsUltimo { get; set; }

    public string? Recepcionistas { get; set; }

    // JSON → List<string>
    public string? ReconhecemosPresenca { get; set; }

    // JSON → List<string>
    public string? Desobrigacoes { get; set; }
    // JSON → List<string>
    public string? Apoios { get; set; }
    // JSON → List<string>
    public string? ConfirmacoesBatismo { get; set; }
    // JSON → List<string>
    public string? ApoioMembros { get; set; }
    // JSON → List<string>
    public string? BencaoCriancas { get; set; }

    // JSON → List<string> (nomes dos que testemunharam, reunião de jejum/testemunhos)
    public string? Testemunhos { get; set; }

    public string? UltimoDiscursante { get; set; }
    public int? IdTipo { get; set; }
    public string? Tema { get; set; }

    // Populado via JOIN com atas — não existe na tabela sacramental
    public string? Date { get; set; }
}

// ──────────────────────────────────────────
// batismo  →  detalhes de ata de batismo
// ──────────────────────────────────────────
public class Batismo
{
    public int Id { get; set; }
    public int AtaId { get; set; }
    public string? Dedicado { get; set; }
    public string? Presidido { get; set; }
    public string? Dirigido { get; set; }

    // JSON → List<object> (pode conter strings simples ou dicts {nome, batizador})
    public string? Batizados { get; set; }

    public string? Testemunha1 { get; set; }
    public string? Testemunha2 { get; set; }

    // JSON complexo com programa estruturado completo (opcional)
    public string? Programa { get; set; }
}

// ──────────────────────────────────────────
// estacas
// ──────────────────────────────────────────
public class Estaca
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Presidente { get; set; }
    public string? PrimeiroConselheiro { get; set; }
    public string? SegundoConselheiro { get; set; }
}

// ──────────────────────────────────────────
// unidades  →  informações da ala
// ──────────────────────────────────────────
public class Unidade
{
    public int Id { get; set; }
    public int AlaId { get; set; }
    public string? Nome { get; set; }
    public string? Bispo { get; set; }
    public string? PrimeiroConselheiro { get; set; }
    public string? SegundoConselheiro { get; set; }
    public int EstacaId { get; set; }
    public string? Horario { get; set; }
    public string? Recepcionista { get; set; }
    public string? Pianista { get; set; }
    public string? RegenteMusica { get; set; }
    public string? Secretario1 { get; set; }
    public string? Secretario2 { get; set; }
    public string? Secretario3 { get; set; }
    public string? Secretario4 { get; set; }
}

// ──────────────────────────────────────────
// templates  →  textos padrão por ala
// tipo_template: 1=Sacramental, 2=Testemunhos
// ──────────────────────────────────────────
public class Template
{
    public int Id { get; set; }
    public int AlaId { get; set; }
    public int TipoTemplate { get; set; }  // 1 ou 2
    public string Nome { get; set; } = string.Empty;
    public string BoasVindas { get; set; } = string.Empty;
    public string Desobrigacoes { get; set; } = string.Empty;
    public string? Apoios { get; set; }
    public string ConfirmacoesBatismo { get; set; } = string.Empty;
    public string ApoioMembroNovo { get; set; } = string.Empty;
    public string BencaoCrianca { get; set; } = string.Empty;
    public string Ordenacoes { get; set; } = string.Empty;
    public string DesobrigacoesPlural { get; set; } = string.Empty;
    public string? ApoiosPlural { get; set; }
    public string ConfirmacoesBatismoPlural { get; set; } = string.Empty;
    public string ApoioMembroNovoPlural { get; set; } = string.Empty;
    public string BencaoCriancaPlural { get; set; } = string.Empty;
    public string OrdenacoesPlural { get; set; } = string.Empty;
    public string Sacramento { get; set; } = string.Empty;
    public string Mensagens { get; set; } = string.Empty;
    public string Live { get; set; } = string.Empty;
    public string Encerramento { get; set; } = string.Empty;
}

// ──────────────────────────────────────────
// tarefas  →  tarefas da secretaria
// ──────────────────────────────────────────
public class Tarefa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public bool Concluida { get; set; }
    public string? Responsavel { get; set; }
    public string? DataPrevista { get; set; }
    public string? ConcluidaEm { get; set; }
    public string CriadaEm { get; set; } = string.Empty;
    public int AlaId { get; set; }
    public string Role { get; set; } = string.Empty;
}
