namespace TruvoID.Components.Services;

// ── Auth ────────────────────────────────────────────────────────────────────

public class RegisterResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// ── Verification ─────────────────────────────────────────────────────────────

public enum VerificationType { Nin, Bvn, Phone }

public class VerificationResponse
{
    public string Status { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public decimal WalletBalanceAfter { get; set; }
    public VerificationData? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

public class VerificationData
{
    public string? Name { get; set; }
    public string? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? PhotoUrl { get; set; }
    public string? IdentityHash { get; set; }
    public decimal? ConfidenceScore { get; set; }
}

// ── API Keys ─────────────────────────────────────────────────────────────────

public class ApiKeyResponse
{
    public string Id { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApiKeyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RawKey { get; set; }
}

public enum ApiKeyStatus { Active, Revoked }

public class CreateApiKeyRequest
{
    public string Description { get; set; } = string.Empty;
}

// ── Wallet ───────────────────────────────────────────────────────────────────

public class WalletBalanceResponse
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "NGN";
}

public class WalletTransactionResponse
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public WalletTransactionType Type { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum WalletTransactionType { Credit, Debit }

// ── Team ─────────────────────────────────────────────────────────────────────

public enum UserRole { Admin, Staff, ReadOnly }

public enum UserStatus { Active, PendingInvitation, Suspended }

public class StaffInviteResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public int DailyCallLimit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StaffInviteRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int DailyCallLimit { get; set; }
}

// ── History ──────────────────────────────────────────────────────────────────

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public enum VerificationStatus { Match, NoMatch, Error, Pending }

public class CallHistoryResponse
{
    public string Id { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string? ApiKeyId { get; set; }
    public VerificationType Type { get; set; }
    public VerificationStatus Status { get; set; }
    public decimal Cost { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Document Upload ──────────────────────────────────────────────────────────

public class UploadDocumentResponse
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

// ── Institution Profile ──────────────────────────────────────────────────────

public class InstitutionProfileResponse
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Active, Suspended
    public bool OnboardingComplete { get; set; }
    public decimal WalletBalance { get; set; }
    public int ApiCallsMtd { get; set; }
    public decimal SpentThisMonth { get; set; }
    public int SuccessfulCallsMtd { get; set; }
    public int FailedCallsMtd { get; set; }
}

// ── Wallet Top-Up ────────────────────────────────────────────────────────────

public class TopupInitiateResponse
{
    public string? AuthorizationUrl { get; set; }
    public string? Reference { get; set; }
}

// ── Settings ─────────────────────────────────────────────────────────────────

public class OnboardingStatusResponse
{
    public InstitutionInfo? Institution { get; set; }
    public bool IsComplete { get; set; }
}

public class InstitutionInfo
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

public class InstitutionSetupRequest
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

