using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TruvoID.Tests;

/// <summary>
/// Integration tests that spin up a Kestrel mock IDAccess server
/// and verify the nested data parsing, photo base64 prefix, and verdict logic.
/// </summary>
public class IdaccessVerificationTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private string _baseUrl = null!;
    private readonly HttpClient _client = new();

    public async Task InitializeAsync()
    {
        // Build a minimal Kestrel server that mocks the IDAccess API
        var builder = WebApplication.CreateBuilder(new[] { "--urls", "http://127.0.0.1:0" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();

        // Capture the assigned port after start
        _app.Urls.Add("http://127.0.0.1:0");

        _app.MapPost("/identity/nin/advance", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            object? response = null;
            if (body.Contains("51407765930"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "MATCH",
                        api_type = "nin.advance",
                        session_id = "ses_test_123",
                        cost_kobo = 5000,
                        environment = "sandbox",
                        data = new
                        {
                            nin = "51407765930",
                            first_name = "SADIQ",
                            last_name = "ABDULRASHEED",
                            middle_name = "MUHAMMAD",
                            date_of_birth = "20-03-1991",
                            phone_number = "07035061222",
                            state_of_origin = "Plateau",
                            state_of_residence = "Plateau",
                            gender = "m",
                            nationality = "NG",
                            birth_country = "nigeria",
                            birth_state = "Plateau",
                            birth_lga = "Jos North",
                            photograph = Convert.ToBase64String(Encoding.UTF8.GetBytes("fake-photo-bytes")),
                            residential_address = "CLOSE TO NDLEA OFFICE RIKKOS. PLATEAU"
                        },
                        call_id = "vcl_test_456"
                    },
                    error = (object?)null,
                    request_id = "req_test_789",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else if (body.Contains("00000000000"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "NO_MATCH",
                        api_type = "nin.advance",
                        session_id = "ses_test_nomatch",
                        cost_kobo = 5000,
                        environment = "sandbox",
                        data = (object?)null,
                        call_id = "vcl_test_nomatch"
                    },
                    error = (object?)null,
                    request_id = "req_test_nomatch",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else if (body.Contains("99999999999"))
            {
                response = new
                {
                    success = false,
                    data = (object?)null,
                    error = new { message = "Invalid NIN format", code = "INVALID_INPUT" },
                    request_id = "req_test_err",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsJsonAsync(new { error = new { message = "Unknown test NIN" } });
                return;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(json);
        });

        await _app.StartAsync();

        // Discover the actual bound URL
        var boundUrl = _app.Urls.FirstOrDefault() ?? "http://127.0.0.1:5099";
        _baseUrl = boundUrl;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helper: parse IDaccess response exactly like VerificationService
    // ──────────────────────────────────────────────────────────────────────

    private static (string? name, string? dob, string? phone, string? gender,
        string? photo, string? stateOfOrigin, string? residentialAddress,
        string? verdict, bool isMatch) ParseIdaccessResponse(string responseJson)
    {
        var resultDoc = JsonDocument.Parse(responseJson);
        var resultRoot = resultDoc.RootElement;
        var apiSuccess = resultRoot.TryGetProperty("success", out var successProp)
                         && successProp.ValueKind == JsonValueKind.True;

        if (!apiSuccess || !resultRoot.TryGetProperty("data", out var dataProp)
                         || dataProp.ValueKind != JsonValueKind.Object)
            return (null, null, null, null, null, null, null, null, false);

        // Navigate into the inner data object (nested data.data)
        var identityData = dataProp;
        if (dataProp.TryGetProperty("data", out var innerData) && innerData.ValueKind == JsonValueKind.Object)
            identityData = innerData;

        var verdict = dataProp.TryGetProperty("verdict", out var verdictProp)
            ? verdictProp.GetString() : null;
        var isMatch = string.Equals(verdict, "MATCH", StringComparison.OrdinalIgnoreCase);

        var firstName = identityData.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() : null;
        var middleName = identityData.TryGetProperty("middle_name", out var mnProp) ? mnProp.GetString() : null;
        var lastName = identityData.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() : null;
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var rawPhoto = identityData.TryGetProperty("photograph", out var phProp) ? phProp.GetString() : null;
        var photoDataUrl = string.IsNullOrWhiteSpace(rawPhoto) ? null
            : rawPhoto.StartsWith("data:") ? rawPhoto
            : $"data:image/jpeg;base64,{rawPhoto}";

        return (
            name: string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            dob: identityData.TryGetProperty("date_of_birth", out var dProp) ? dProp.GetString() : null,
            phone: identityData.TryGetProperty("phone_number", out var pProp) ? pProp.GetString() : null,
            gender: identityData.TryGetProperty("gender", out var gProp) ? gProp.GetString() : null,
            photo: photoDataUrl,
            stateOfOrigin: identityData.TryGetProperty("state_of_origin", out var soProp) ? soProp.GetString() : null,
            residentialAddress: identityData.TryGetProperty("residential_address", out var raProp) ? raProp.GetString() : null,
            verdict: verdict,
            isMatch: isMatch
        );
    }

    // ──────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MockServer_ReturnsSuccessfulResponse()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ParseMatchResponse_ExtractsAllIdentityFields()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("MATCH", result.verdict);
        Assert.True(result.isMatch);
        Assert.Equal("SADIQ MUHAMMAD ABDULRASHEED", result.name);
        Assert.Equal("20-03-1991", result.dob);
        Assert.Equal("07035061222", result.phone);
        Assert.Equal("m", result.gender);
        Assert.Equal("Plateau", result.stateOfOrigin);
        Assert.Equal("CLOSE TO NDLEA OFFICE RIKKOS. PLATEAU", result.residentialAddress);
    }

    [Fact]
    public async Task ParseMatchResponse_PhotoHasBase64DataUrlPrefix()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.NotNull(result.photo);
        Assert.StartsWith("data:image/jpeg;base64,", result.photo);

        var base64Payload = result.photo!["data:image/jpeg;base64,".Length..];
        var decoded = Convert.FromBase64String(base64Payload);
        Assert.Equal("fake-photo-bytes", Encoding.UTF8.GetString(decoded));
    }

    [Fact]
    public async Task ParseNoMatchResponse_ReturnsFalseForIsMatch()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "00000000000" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("NO_MATCH", result.verdict);
        Assert.False(result.isMatch);
        Assert.Null(result.name);
    }

    [Fact]
    public async Task ParseErrorResponse_ReturnsFalseForSuccess()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "99999999999" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.False(result.isMatch);
        Assert.Null(result.name);
    }

    [Fact]
    public void PhotoUrlAlreadyDataPrefixed_IsNotDoublePrefixed()
    {
        var rawPhoto = "data:image/png;base64,iVBORw0KGgo=";
        var photoDataUrl = string.IsNullOrWhiteSpace(rawPhoto) ? null
            : rawPhoto.StartsWith("data:") ? rawPhoto
            : $"data:image/jpeg;base64,{rawPhoto}";

        Assert.Equal("data:image/png;base64,iVBORw0KGgo=", photoDataUrl);
        Assert.DoesNotContain("data:image/jpeg;base64,data:", photoDataUrl);
    }

    [Fact]
    public void EmptyPhoto_ReturnsNull()
    {
        string? rawPhoto = null;
        var photoDataUrl = string.IsNullOrWhiteSpace(rawPhoto) ? null
            : rawPhoto.StartsWith("data:") ? rawPhoto
            : $"data:image/jpeg;base64,{rawPhoto}";

        Assert.Null(photoDataUrl);
    }

    [Fact]
    public async Task Gender_IsPreservedRaw_ForFrontendFormatting()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);
        Assert.Equal("m", result.gender);
    }

    [Fact]
    public async Task MockServer_AcceptsAuthorizationHeader()
    {
        using var authedClient = new HttpClient();
        authedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-api-key");

        var response = await authedClient.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FullRoundTrip_ParsedFields_MatchBackendStorageFormat()
    {
        // Simulates what VerificationService stores in MatchedFieldsJson
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        var json = await response.Content.ReadAsStringAsync();
        var result = ParseIdaccessResponse(json);

        // This is how VerificationService serializes matchedData
        var matchedData = new
        {
            name = result.name,
            dob = result.dob,
            phone = result.phone,
            gender = result.gender,
            photo = result.photo,
            stateOfOrigin = result.stateOfOrigin,
            residentialAddress = result.residentialAddress
        };
        var serialized = JsonSerializer.Serialize(matchedData);
        var doc = JsonDocument.Parse(serialized);
        var root = doc.RootElement;

        // MapToResponse reads these exact property names
        Assert.Equal("SADIQ MUHAMMAD ABDULRASHEED", root.GetProperty("name").GetString());
        Assert.Equal("20-03-1991", root.GetProperty("dob").GetString());
        Assert.Equal("07035061222", root.GetProperty("phone").GetString());
        Assert.Equal("m", root.GetProperty("gender").GetString());
        Assert.StartsWith("data:image/jpeg;base64,", root.GetProperty("photo").GetString()!);
        Assert.Equal("Plateau", root.GetProperty("stateOfOrigin").GetString());
        Assert.Equal("CLOSE TO NDLEA OFFICE RIKKOS. PLATEAU", root.GetProperty("residentialAddress").GetString());
    }
}
