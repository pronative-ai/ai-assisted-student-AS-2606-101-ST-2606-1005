using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookkeeping.Tests;

public class ApiAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiAuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ReturnsOk_WithoutAuth()
    {
        var res = await _client.GetAsync("/api/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Accounts_Returns401_WithoutToken()
    {
        var res = await _client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Ledger_Returns401_WithoutToken()
    {
        var res = await _client.GetAsync("/api/accounts/ACC-F01/ledger");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Summary_Returns401_WithoutToken()
    {
        var res = await _client.GetAsync("/api/financial-summary");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/token", new { Username = "nobody", Password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/token", new { Username = "admin", Password = "admin123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body?.Token);
        Assert.Equal("admin", body.Username);
    }

    [Fact]
    public async Task Accounts_ReturnsOk_WithValidToken()
    {
        var token = await GetToken("viewer", "viewer123");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Accounts_Returns401_WithFakeToken()
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "totally-fake-token");

        var res = await _client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task AccountNotFound_Returns404()
    {
        var token = await GetToken("admin", "admin123");
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/api/accounts/DOES-NOT-EXIST");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private async Task<string> GetToken(string username, string password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/token", new { Username = username, Password = password });
        var body = await res.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.Token;
    }

    private record TokenResponse(string Token, string Username, string Role);
}
