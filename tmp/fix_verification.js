const fs = require('fs');
let c = fs.readFileSync('src/TruvoID.Infrastructure/Services/VerificationService.cs', 'utf8');
c = c.replace(/\r\n/g, '\n');

// 1. Fix base URL
c = c.replace('https://api.idaccess.info/v1', 'https://idaccess.info/v1');

// 2. Replace endpoint switch expression with switch statement
const oldSwitch = `            var endpoint = type switch
            {
                VerificationType.Nin => "nin/verify",
                VerificationType.Bvn => "bvn/verify",
                VerificationType.Phone => "phone/verify",
                _ => throw new NotSupportedException($"Verification type {type} is not supported.")
            };

            var requestBody = new { number = subjectRef };`;

const newSwitch = `            string endpoint;
            string bodyField;
            switch (type)
            {
                case VerificationType.Nin: endpoint = "identity/nin"; bodyField = "nin"; break;
                case VerificationType.Bvn: endpoint = "identity/bvn"; bodyField = "bvn"; break;
                case VerificationType.Phone: endpoint = "identity/phone"; bodyField = "phone"; break;
                default: throw new NotSupportedException($"Verification type {type} is not supported.");
            }

            var requestBody = new Dictionary<string, string> { { bodyField, subjectRef.Trim() } };`;

c = c.replace(oldSwitch, newSwitch);

// 3. Add idempotency header + trim key
c = c.replace(
    'var client = _httpClientFactory.CreateClient("idaccess");\n            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);',
    `var client = _httpClientFactory.CreateClient("idaccess");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            var upstreamIdempotencyKey = idempotencyKey ?? $"req_{call.Id:N}";
            client.DefaultRequestHeaders.Remove("Idempotency-Key");
            client.DefaultRequestHeaders.Add("Idempotency-Key", upstreamIdempotencyKey);`
);

// 4. Add logging
c = c.replace(
    'var httpResponse = await client.PostAsJsonAsync($"{IdaccessBaseUrl}/{endpoint}", requestBody, ct);',
    `Console.WriteLine($"[VERIFY] Calling {IdaccessBaseUrl}/{endpoint}");
            var httpResponse = await client.PostAsJsonAsync($"{IdaccessBaseUrl}/{endpoint}", requestBody, ct);`
);
c = c.replace(
    'var responseContent = await httpResponse.Content.ReadAsStringAsync(ct);',
    `var responseContent = await httpResponse.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[VERIFY] HTTP {(int)httpResponse.StatusCode}: {responseContent}");`
);

// 5. Replace success handler - find and replace the if block
const successStart = c.indexOf('if (httpResponse.IsSuccessStatusCode)');
const successBodyStart = c.indexOf('{', successStart);
let depth = 0, successEnd = -1;
for (let i = successBodyStart; i < c.length; i++) {
    if (c[i] === '{') depth++;
    if (c[i] === '}') { depth--; if (depth === 0) { successEnd = i + 1; break; } }
}

const newSuccess = `if (httpResponse.IsSuccessStatusCode)
            {
                var resultDoc = JsonDocument.Parse(responseContent);
                var resultRoot = resultDoc.RootElement;
                var apiSuccess = resultRoot.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.True;

                if (apiSuccess && resultRoot.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
                {
                    var matchedData = new
                    {
                        name = dataProp.TryGetProperty("name", out var nProp) ? nProp.GetString() : null,
                        dob = dataProp.TryGetProperty("dob", out var dProp) ? dProp.GetString() : null,
                        phone = dataProp.TryGetProperty("phone", out var pProp) ? pProp.GetString() : null,
                        gender = dataProp.TryGetProperty("gender", out var gProp) ? gProp.GetString() : null,
                        photo = dataProp.TryGetProperty("photo", out var phProp) ? phProp.GetString() : null
                    };
                    var resultUpdate = Builders<VerificationCall>.Update
                        .Set(c => c.Status, VerificationStatus.Match)
                        .Set(c => c.MatchedFieldsJson, JsonSerializer.Serialize(matchedData))
                        .Set(c => c.RawResponseJson, responseContent)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow);
                    await _db.VerificationCalls.UpdateOneAsync(c => c.Id == call.Id, resultUpdate, cancellationToken: ct);
                }
                else
                {
                    var errorMessage = "Verification returned no match.";
                    if (resultRoot.TryGetProperty("error", out var errObj) && errObj.ValueKind == JsonValueKind.Object)
                        if (errObj.TryGetProperty("message", out var mp))
                            errorMessage = mp.GetString() ?? errorMessage;

                    var isNoMatch = errorMessage.Contains("no match", StringComparison.OrdinalIgnoreCase);
                    var callStatus = isNoMatch ? VerificationStatus.NoMatch : VerificationStatus.Error;
                    var errUpdate = Builders<VerificationCall>.Update
                        .Set(c => c.Status, callStatus)
                        .Set(c => c.ErrorMessage, errorMessage)
                        .Set(c => c.RawResponseJson, responseContent)
                        .Set(c => c.UpdatedAt, DateTime.UtcNow);
                    await _db.VerificationCalls.UpdateOneAsync(c => c.Id == call.Id, errUpdate, cancellationToken: ct);
                    if (!isNoMatch)
                        await _walletService.CreditAsync(institutionId, price, $"Refund: {type} upstream error", call.Id.ToString(), ct);
                }
            }`;

c = c.substring(0, successStart) + newSuccess + c.substring(successEnd);

// 6. Replace error handler
c = c.replace(
    'var errorMessage = "Upstream verification failed.";',
    'var errorMessage = $"Upstream returned HTTP {(int)httpResponse.StatusCode}.";'
);
c = c.replace(
    'var status = responseContent.Contains("no match", StringComparison.OrdinalIgnoreCase)\n                    ? VerificationStatus.NoMatch\n                    : VerificationStatus.Error;',
    'var status = VerificationStatus.Error;'
);

// 7. Replace the old API key lookup with ResolveIdaccessApiKey call
c = c.replace(
    /var apiKey = _configuration\["IDACCESS_API_KEY"\]\s*\?\? _configuration\["IDACCESS-API-KEY"\]\s*\?\? Environment\.GetEnvironmentVariable\("IDACCESS_API_KEY"\)\s*\?\? Environment\.GetEnvironmentVariable\("IDACCESS-API-KEY"\)\s*\?\? FindEnvVarContaining\("IDACCESS_API_KEY"\);/,
    'var apiKey = ResolveIdaccessApiKey();'
);

// 8. Replace old error message in catch
c = c.replace(
    '// API call failed entirely',
    'Console.WriteLine($"[VERIFY] Exception: {ex.Message}");\n            // API call failed entirely'
);

// 9. Add ResolveIdaccessApiKey method and remove FindEnvVarContaining
c = c.replace(
    `    private static string? FindEnvVarContaining(string partialName)
    {
        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            var keyStr = key.ToString()!.Trim();
            if (keyStr.StartsWith(partialName, StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetEnvironmentVariable(key.ToString()!);
            }
        }
        return null;
    }

    private static string HashSubjectRef(string subjectRef)`,
    `    private string? ResolveIdaccessApiKey()
    {
        var key = _configuration["IDACCESS_API_KEY"]
            ?? _configuration["IDACCESS-API-KEY"]
            ?? Environment.GetEnvironmentVariable("IDACCESS_API_KEY")
            ?? Environment.GetEnvironmentVariable("IDACCESS-API-KEY");
        if (!string.IsNullOrEmpty(key)) return key;

        foreach (var envKey in Environment.GetEnvironmentVariables().Keys)
        {
            var k = envKey.ToString()!.Trim();
            if (k.StartsWith("IDACCESS_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                var v = Environment.GetEnvironmentVariable(k);
                Console.WriteLine($"[VERIFY] Found API key via fuzzy match: '{k}'");
                return v;
            }
        }
        return null;
    }

    private static string HashSubjectRef(string subjectRef)`
);

// Restore CRLF
c = c.replace(/\n/g, '\r\n');

fs.writeFileSync('src/TruvoID.Infrastructure/Services/VerificationService.cs', c);
console.log('Done - ' + c.split('\n').length + ' lines');
