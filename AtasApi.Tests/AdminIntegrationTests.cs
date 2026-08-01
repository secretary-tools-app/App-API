using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AtasApi.Tests;

public class AdminIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminSecret = "local-dev-admin-secret-32chars-minimum-required";
    private readonly HttpClient _client;

    public AdminIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdminResetPassword_SemSecret_DeveRetornar403()
    {
        var req = new { username = "usuario_nao_existente", newPassword = "NovaSenha@123" };

        var response = await _client.PutAsJsonAsync("/api/admin/users/password", req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminResetPassword_ComSecretInvalido_DeveRetornar403()
    {
        var req = new { username = "usuario_nao_existente", newPassword = "NovaSenha@123" };
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/users/password")
        {
            Content = JsonContent.Create(req),
        };
        request.Headers.Add("X-Admin-Secret", "segredo-errado-com-32-caracteres-xx");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminResetPassword_UsuarioInexistente_DeveRetornar404()
    {
        var req = new { username = "usuario_nao_existente_xyz", newPassword = "NovaSenha@123" };
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/admin/users/password")
        {
            Content = JsonContent.Create(req),
        };
        request.Headers.Add("X-Admin-Secret", AdminSecret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminResetPassword_ComSecretValido_DevePermitirLoginComNovaSenha()
    {
        var username = Environment.GetEnvironmentVariable("TEST_USERNAME");
        var currentPassword = Environment.GetEnvironmentVariable("TEST_PASSWORD");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(currentPassword))
            return;

        var newPassword = "AdminReset@Test123";
        var resetReq = new { username, newPassword };
        using (var resetRequest = new HttpRequestMessage(HttpMethod.Put, "/api/admin/users/password")
        {
            Content = JsonContent.Create(resetReq),
        })
        {
            resetRequest.Headers.Add("X-Admin-Secret", AdminSecret);
            var resetResponse = await _client.SendAsync(resetRequest);
            Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { username, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var restoreRequest = new HttpRequestMessage(HttpMethod.Put, "/api/admin/users/password")
        {
            Content = JsonContent.Create(new { username, newPassword = currentPassword }),
        };
        restoreRequest.Headers.Add("X-Admin-Secret", AdminSecret);
        var restoreResponse = await _client.SendAsync(restoreRequest);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
    }
}
