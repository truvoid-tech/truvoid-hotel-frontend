namespace TruvoID.Components.Services;

// ──────────────────────────── Admin DTOs ────────────────────────────

public record AdminOverviewDto
{
    public decimal RevenueMtd { get; init; }
    public decimal CostsMtd { get; init; }
    public decimal NetMargin { get; init; }
    public int ActiveInstitutions { get; init; }
    public int PendingInstitutions { get; init; }
    public int TotalApiCallsMtd { get; init; }
    public decimal TotalWalletBalances { get; init; }
    public int PendingTopUpApprovals { get; init; }
    public decimal RevenueGrowthPct { get; init; }
    public int NewInstitutionsThisMonth { get; init; }
    public List<InstitutionVolumeDto> TopInstitutions { get; init; } = [];
    public CallBreakdownDto CallBreakdown { get; init; } = new();
}

public record InstitutionVolumeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public int CallsMtd { get; init; }
    public decimal RevenueMtd { get; init; }
    public bool Active { get; init; }
}

public record CallBreakdownDto
{
    public int NinCalls { get; init; }
    public int BvnCalls { get; init; }
    public int PhoneCalls { get; init; }
    public int Total => NinCalls + BvnCalls + PhoneCalls;
}

public record AdminInstitutionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Status { get; init; } = "Active"; // Active, Pending, Suspended
    public decimal WalletBalance { get; init; }
    public int ApiCallsMtd { get; init; }
    public DateTime JoinedDate { get; init; }
    public string Type { get; init; } = "";
}

public record AdminTopUpDto
{
    public string Id { get; init; } = string.Empty;
    public string Institution { get; init; } = "";
    public string Email { get; init; } = "";
    public decimal Amount { get; init; }
    public string Reference { get; init; } = "";
    public string Submitted { get; init; } = "";
    public string Status { get; init; } = "Pending";
}

public record AdminTransactionDto
{
    public string Reference { get; init; } = "";
    public string Institution { get; init; } = "";
    public string Type { get; init; } = ""; // Wallet Top-Up, API Call
    public decimal Amount { get; init; }
    public string Date { get; init; } = "";
}

public record AdminFinancialsDto
{
    public decimal GrossRevenue { get; init; }
    public decimal NimcPayouts { get; init; }
    public decimal NetProfit { get; init; }
    public decimal MarginPct { get; init; }
    public int TotalCalls { get; init; }
    public List<AdminTopUpDto> PendingTopUps { get; init; } = [];
    public List<AdminTransactionDto> Transactions { get; init; } = [];
}

public record PricingDto
{
    public string Type { get; init; } = ""; // NIN, BVN, Phone
    public decimal InstitutionCharge { get; init; }
    public decimal NimcCost { get; init; }
    public decimal Margin => InstitutionCharge - NimcCost;
    public decimal MarginPct => InstitutionCharge > 0 ? Math.Round(Margin / InstitutionCharge * 100, 1) : 0;
}

public record UpdatePricingRequest
{
    public decimal InstitutionCharge { get; set; }
    public decimal NimcCost { get; set; }
}

// ──────────────────────────── API Audit ─────────────────────────────────

public class AdminApiKeyDto
{
    public string Id { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

// ──────────────────────────── Paginated List ────────────────────────────

public record PaginatedList<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
