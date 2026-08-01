using AtasApi.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AtasApi.Tests;

// A WebApplicationFactory sobe a sua API na memória para os testes
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    // Credenciais reais vêm de variáveis de ambiente (nunca do código-fonte).
    private static readonly string? TestUser = Environment.GetEnvironmentVariable("TEST_USERNAME");
    private static readonly string? TestPassword = Environment.GetEnvironmentVariable("TEST_PASSWORD");

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Função auxiliar para facilitar nossa vida: logar e pegar o token
    private async Task<string> GetAuthTokenAsync()
    {
        RequireTestCredentials();

        var loginData = new { Username = TestUser, Password = TestPassword };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.Token;
    }

    private static void RequireTestCredentials()
    {
        if (string.IsNullOrWhiteSpace(TestUser) || string.IsNullOrWhiteSpace(TestPassword))
            throw new InvalidOperationException(
                "Defina as variáveis de ambiente TEST_USERNAME e TEST_PASSWORD para rodar os testes autenticados.");
    }
    // ── TESTES DE AUTENTICAÇÃO (Auth) ──────────────────────────────────

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        RequireTestCredentials();

        // Arrange
        var req = new { Username = TestUser, Password = TestPassword };
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", req);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_DeveRetornar401()
    {
        // Arrange (credenciais obviamente inválidas — nada de secreto aqui)
        var req = new { Username = "usuario_nao_existente", Password = "SenhaInvalida@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", req);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComJsonCamelCase_DeveRetornar401EmVezDe400()
    {
        // Arrange: o frontend envia camelCase (username/password)
        var req = new { username = "usuario_nao_existente", password = "SenhaInvalida@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", req);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── TESTES DE ATAS (Requerem Token) ────────────────────────────────

    [Fact]
    public async Task GetAllAtas_SemToken_DeveRetornar401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/atas/all");

        // Assert (Garante que a nossa segurança está funcionando!)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllAtas_ComTokenValido_DeveRetornar200OK()
    {
        // Arrange (Pega o token e injeta no cabeçalho, igual fizemos no curl)
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/atas/all");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Opcional: validar se retorna uma lista
        // var atas = await response.Content.ReadFromJsonAsync<List<AtaResponse>>();
        // Assert.NotNull(atas);
    }

    [Fact]
    public async Task GetAtaById_NaoExistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/atas/999999"); // ID que não existe

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── TESTES DE CRIAÇÃO (POST) ──────────────────────────────────────

    [Fact]
    public async Task CreateAta_DadosValidos_DeveRetornar201Created()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var novaAta = new
        {
            Tipo = "sacramental",
            Data = DateTime.Now.ToString("yyyy-MM-dd")
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/atas", novaAta);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"O erro foi: {await response.Content.ReadAsStringAsync()}");
        // Quando criamos algo, o certo é retornar 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── OUTROS FILTROS DE ATAS ────────────────────────────────────────

    [Fact]
    public async Task GetByMes_ComTokenValido_DeveRetornar200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var mesFiltro = DateTime.Now.ToString("yyyy-MM");

        // Act
        var response = await _client.GetAsync($"/api/atas?mes={mesFiltro}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByDataTipo_Inexistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/atas/by-data?data=2000-01-01&tipo=sacramental");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── ATUALIZAÇÃO E EXCLUSÃO DE ATAS ────────────────────────────────

    [Fact]
    public async Task UpdateAta_Inexistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dadosAtualizados = new
        {
            Tipo = "sacramental",
            Data = DateTime.Now.ToString("yyyy-MM-dd")
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/atas/999999", dadosAtualizados);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAta_Inexistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/atas/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    // ── TESTES DE DETALHES (Sacramental e Batismo) ────────────────────

    [Fact]
    public async Task Sacramental_Get_Inexistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/sacramental/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Batismo_Get_Inexistente_DeveRetornar404NotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/batismo/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    // ── TESTES DE REGRAS DE NEGÓCIO (Discursantes) ────────────────────
    private object CriarPayloadValido(string date)
    {
        return new
        {
            Date = date,
            Tema = "Fé",
            HinoAbertura = "10", // 🔥 AQUI
            Discursante1 = "João",
            Discursante2 = "Maria",
            Discursante3 = "Pedro"
        };
    }


    [Fact]
    public async Task SalvarDiscursantes_DiaDeSemana_DeveRetornar400BadRequest()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = CriarPayloadValido("2026-06-03"); // quarta-feira

        var response = await _client.PostAsJsonAsync("/api/discursantes/salvar", req);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = JsonDocument.Parse(content);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Contains("domingo", message!.ToLower());
    }


    [Fact]
    public async Task SalvarDiscursantes_PrimeiroDomingo_DeveRetornar400BadRequest()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = CriarPayloadValido("2026-06-07"); // primeiro domingo

        var response = await _client.PostAsJsonAsync("/api/discursantes/salvar", req);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(content);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Contains("testemunhos", message!.ToLower());
    }

    [Fact]
    public async Task GetRecentes_ComParametroValido_DeveRetornar200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/discursantes/recentes?dias=90");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


    // ── TESTES DE CONFIGURAÇÕES E ESTATÍSTICAS ────────────────────────

    [Fact]
    public async Task GetTemplates_ComTokenValido_DeveRetornar200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/configuracoes/templates");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUnidade_ComTokenValido_DeveRetornar200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/configuracoes/unidade");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEstatisticas_ComTokenValido_DeveRetornar200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/configuracoes/estatisticas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

}