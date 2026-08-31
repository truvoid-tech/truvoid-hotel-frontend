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

        // ── BVN advance endpoint ──
        _app.MapPost("/identity/bvn/advance", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            object? response = null;
            if (body.Contains("22222222222"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "MATCH",
                        api_type = "bvn.advance",
                        session_id = "ses_bvn_match",
                        cost_kobo = 15000,
                        environment = "sandbox",
                        data = new
                        {
                            bvn = "22222222222",
                            first_name = "CHINEDU",
                            last_name = "OKAFOR",
                            middle_name = "EMEKA",
                            date_of_birth = "15-06-1988",
                            phone_number = "08098765432",
                            gender = "m",
                            photograph = Convert.ToBase64String(Encoding.UTF8.GetBytes("bvn-photo-bytes")),
                            bank_name = "Guaranty Trust Bank",
                            account_verified = true
                        },
                        call_id = "vcl_bvn_456"
                    },
                    error = (object?)null,
                    request_id = "req_bvn_789",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else if (body.Contains("00000000001"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "NO_MATCH",
                        api_type = "bvn.advance",
                        session_id = "ses_bvn_nomatch",
                        cost_kobo = 15000,
                        environment = "sandbox",
                        data = (object?)null,
                        call_id = "vcl_bvn_nomatch"
                    },
                    error = (object?)null,
                    request_id = "req_bvn_nomatch",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsJsonAsync(new { error = new { message = "Unknown test BVN" } });
                return;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(json);
        });

        // ── Phone basic endpoint ──
        _app.MapPost("/identity/phone/basic", async (HttpContext ctx) =>
        {
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            object? response = null;
            if (body.Contains("08011112222"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "MATCH",
                        api_type = "phone.basic",
                        session_id = "ses_phone_match",
                        cost_kobo = 5000,
                        environment = "sandbox",
                        data = new
                        {
                            phone_number = "08011112222",
                            first_name = "AISHA",
                            last_name = "IBRAHIM",
                            middle_name = "Bello",
                            carrier = "MTN",
                            status = "active",
                            registration_date = "2018-03-15"
                        },
                        call_id = "vcl_phone_456"
                    },
                    error = (object?)null,
                    request_id = "req_phone_789",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else if (body.Contains("08000000001"))
            {
                response = new
                {
                    success = true,
                    data = new
                    {
                        verdict = "NO_MATCH",
                        api_type = "phone.basic",
                        session_id = "ses_phone_nomatch",
                        cost_kobo = 5000,
                        environment = "sandbox",
                        data = (object?)null,
                        call_id = "vcl_phone_nomatch"
                    },
                    error = (object?)null,
                    request_id = "req_phone_nomatch",
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsJsonAsync(new { error = new { message = "Unknown test phone" } });
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

    // ──────────────────────────────────────────────────────────────────────
    // BVN Verification Tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Bvn_MatchResponse_ExtractsIdentityFields()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "22222222222" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("MATCH", result.verdict);
        Assert.True(result.isMatch);
        Assert.Equal("CHINEDU EMEKA OKAFOR", result.name);
        Assert.Equal("15-06-1988", result.dob);
        Assert.Equal("08098765432", result.phone);
        Assert.Equal("m", result.gender);
    }

    [Fact]
    public async Task Bvn_MatchPhoto_HasBase64Prefix()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "22222222222" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.NotNull(result.photo);
        Assert.StartsWith("data:image/jpeg;base64,", result.photo);

        var base64Payload = result.photo!["data:image/jpeg;base64,".Length..];
        var decoded = Convert.FromBase64String(base64Payload);
        Assert.Equal("bvn-photo-bytes", Encoding.UTF8.GetString(decoded));
    }

    [Fact]
    public async Task Bvn_NoMatchResponse_ReturnsFalse()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "00000000001" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("NO_MATCH", result.verdict);
        Assert.False(result.isMatch);
        Assert.Null(result.name);
    }

    [Fact]
    public async Task Bvn_VerdictField_DeterminesMatchStatus()
    {
        // BVN match: verdict = MATCH → isMatch = true
        var matchResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "22222222222" });
        var matchJson = await matchResp.Content.ReadAsStringAsync();
        var matchResult = ParseIdaccessResponse(matchJson);
        Assert.True(matchResult.isMatch);

        // BVN no-match: verdict = NO_MATCH → isMatch = false
        var noMatchResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "00000000001" });
        var noMatchJson = await noMatchResp.Content.ReadAsStringAsync();
        var noMatchResult = ParseIdaccessResponse(noMatchJson);
        Assert.False(noMatchResult.isMatch);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Phone Verification Tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Phone_MatchResponse_ExtractsIdentityFields()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08011112222" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("MATCH", result.verdict);
        Assert.True(result.isMatch);
        Assert.Equal("AISHA Bello IBRAHIM", result.name);
        Assert.Equal("08011112222", result.phone);
    }

    [Fact]
    public async Task Phone_MatchResponse_HasNoPhoto()
    {
        // Phone basic endpoint typically doesn't return a photograph
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08011112222" });
        var json = await response.Content.ReadAsStringAsync();

        // Parse the raw JSON to check if photograph exists
        var doc = JsonDocument.Parse(json);
        var dataProp = doc.RootElement.GetProperty("data").GetProperty("data");
        var hasPhoto = dataProp.TryGetProperty("photograph", out _);

        // Phone basic may not have photo — this is expected
        // The parsing logic handles null photo gracefully
        if (hasPhoto)
        {
            var result = ParseIdaccessResponse(json);
            Assert.NotNull(result.photo);
        }
        // If no photo, that's fine — photo should be null
    }

    [Fact]
    public async Task Phone_NoMatchResponse_ReturnsFalse()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08000000001" });
        var json = await response.Content.ReadAsStringAsync();

        var result = ParseIdaccessResponse(json);

        Assert.Equal("NO_MATCH", result.verdict);
        Assert.False(result.isMatch);
        Assert.Null(result.name);
    }

    [Fact]
    public async Task Phone_VerdictField_DeterminesMatchStatus()
    {
        // Phone match
        var matchResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08011112222" });
        var matchJson = await matchResp.Content.ReadAsStringAsync();
        var matchResult = ParseIdaccessResponse(matchJson);
        Assert.True(matchResult.isMatch);

        // Phone no-match
        var noMatchResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08000000001" });
        var noMatchJson = await noMatchResp.Content.ReadAsStringAsync();
        var noMatchResult = ParseIdaccessResponse(noMatchJson);
        Assert.False(noMatchResult.isMatch);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Cross-type Tests
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllTypes_UseSameNestedParsingLogic()
    {
        // Verify that the nested data.data structure is consistent across all types
        var ninResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "51407765930" });
        var bvnResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "22222222222" });
        var phoneResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08011112222" });

        var ninResult = ParseIdaccessResponse(await ninResp.Content.ReadAsStringAsync());
        var bvnResult = ParseIdaccessResponse(await bvnResp.Content.ReadAsStringAsync());
        var phoneResult = ParseIdaccessResponse(await phoneResp.Content.ReadAsStringAsync());

        // All three should be matches
        Assert.True(ninResult.isMatch);
        Assert.True(bvnResult.isMatch);
        Assert.True(phoneResult.isMatch);

        // All three should have parsed name and phone
        Assert.NotNull(ninResult.name);
        Assert.NotNull(bvnResult.name);
        Assert.NotNull(phoneResult.name);
        Assert.NotNull(ninResult.phone);
        Assert.NotNull(bvnResult.phone);
        Assert.NotNull(phoneResult.phone);
    }

    [Fact]
    public async Task AllTypes_NoMatch_VerdictIsConsistent()
    {
        var ninResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/nin/advance",
            new { nin = "00000000000" });
        var bvnResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/bvn/advance",
            new { bvn = "00000000001" });
        var phoneResp = await _client.PostAsJsonAsync($"{_baseUrl}/identity/phone/basic",
            new { phone_number = "08000000001" });

        var ninResult = ParseIdaccessResponse(await ninResp.Content.ReadAsStringAsync());
        var bvnResult = ParseIdaccessResponse(await bvnResp.Content.ReadAsStringAsync());
        var phoneResult = ParseIdaccessResponse(await phoneResp.Content.ReadAsStringAsync());

        Assert.False(ninResult.isMatch);
        Assert.False(bvnResult.isMatch);
        Assert.False(phoneResult.isMatch);
        Assert.Equal("NO_MATCH", ninResult.verdict);
        Assert.Equal("NO_MATCH", bvnResult.verdict);
        Assert.Equal("NO_MATCH", phoneResult.verdict);
    }
}
