using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MongoDB.Driver;
using TruvoID.Core.Interfaces;
using TruvoID.Domain.Entities;
using TruvoID.Domain.Enums;
using TruvoID.Infrastructure.Data;

namespace TruvoID.API.Endpoints;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Institution-facing: get my rates ──
        var myPricingGroup = app.MapGroup("/v1/pricing/my-rates")
            .RequireAuthorization();
        myPricingGroup.MapGet("/", GetMyRates);

        // ── Admin: manage per-institution pricing ──
        var adminPricingGroup = app.MapGroup("/v1/admin/pricing/institutions")
            .RequireAuthorization("TruvoAdmin");
        adminPricingGroup.MapGet("/", GetAllInstitutionPricing);
        adminPricingGroup.MapPut("/{institutionId:guid}", UpdateInstitutionPricing);

        return app;
    }

    /// <summary>
    /// GET /v1/pricing/my-rates
    /// Returns the logged-in institution's per-type pricing as a simple rate list.
    /// </summary>
    private static async Task<IResult> GetMyRates(
        HttpContext ctx,
        IPricingService pricingService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        var ninPrice = await pricingService.GetPriceAsync(VerificationType.Nin, institutionId);
        var bvnPrice = await pricingService.GetPriceAsync(VerificationType.Bvn, institutionId);
        var phonePrice = await pricingService.GetPriceAsync(VerificationType.Phone, institutionId);

        return Results.Ok(new object[]
        {
            new { Type = "NIN", PricePerCall = ninPrice },
            new { Type = "BVN", PricePerCall = bvnPrice },
            new { Type = "PHONE", PricePerCall = phonePrice }
        });
    }

    /// <summary>
    /// GET /v1/admin/pricing/institutions
    /// Admin endpoint: returns all institutions with their current pricing, spend, and call counts.
    /// </summary>
    private static async Task<IResult> GetAllInstitutionPricing(
        MongoDbContext db)
    {
        var institutions = await db.Institutions
            .Find(i => true)
            .ToListAsync();

        var institutionIds = institutions.Select(i => i.Id).ToList();

        var pricings = await db.InstitutionPricings
            .Find(p => institutionIds.Contains(p.InstitutionId))
            .ToListAsync();

        var pricingDict = pricings.ToDictionary(p => p.InstitutionId);

        // Get call counts and spend per institution (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var callAggregation = await db.VerificationCalls
            .Aggregate()
            .Match(c => c.CreatedAt >= thirtyDaysAgo && institutionIds.Contains(c.InstitutionId))
            .Group(c => c.InstitutionId, g => new
            {
                InstitutionId = g.Key,
                TotalCalls = g.Count(),
                TotalSpend = g.Sum(c => c.AmountCharged)
            })
            .ToListAsync();

        var callDict = callAggregation.ToDictionary(c => c.InstitutionId);

        var result = institutions.Select(inst =>
        {
            var pricing = pricingDict.GetValueOrDefault(inst.Id);
            var calls = callDict.GetValueOrDefault(inst.Id);

            return new
            {
                InstitutionId = inst.Id.ToString(),
                InstitutionName = inst.Name,
                Email = inst.ContactEmail ?? "",
                Status = inst.Status ?? "Pending",
                NinPrice = pricing?.NinPrice ?? 100m,
                BvnPrice = pricing?.BvnPrice ?? 150m,
                PhonePrice = pricing?.PhonePrice ?? 50m,
                NinCost = pricing?.NinCost ?? 45m,
                BvnCost = pricing?.BvnCost ?? 65m,
                PhoneCost = pricing?.PhoneCost ?? 20m,
                TotalCallsMtd = calls?.TotalCalls ?? 0,
                TotalSpendMtd = calls?.TotalSpend ?? 0m
            };
        }).ToList();

        return Results.Ok(result);
    }

    /// <summary>
    /// PUT /v1/admin/pricing/institutions/{institutionId}
    /// Admin endpoint: update or create per-institution pricing.
    /// </summary>
    private static async Task<IResult> UpdateInstitutionPricing(
        Guid institutionId,
        UpdateInstitutionPricingBody body,
        MongoDbContext db)
    {
        // Verify institution exists
        var institution = await db.Institutions
            .Find(i => i.Id == institutionId)
            .FirstOrDefaultAsync();

        if (institution is null)
            return Results.NotFound(new { error = "Institution not found." });

        var existing = await db.InstitutionPricings
            .Find(p => p.InstitutionId == institutionId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            var update = Builders<InstitutionPricing>.Update
                .Set(p => p.NinPrice, body.NinPrice)
                .Set(p => p.BvnPrice, body.BvnPrice)
                .Set(p => p.PhonePrice, body.PhonePrice)
                .Set(p => p.NinCost, body.NinCost)
                .Set(p => p.BvnCost, body.BvnCost)
                .Set(p => p.PhoneCost, body.PhoneCost)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);
            await db.InstitutionPricings.UpdateOneAsync(p => p.Id == existing.Id, update);
        }
        else
        {
            var newPricing = new InstitutionPricing
            {
                InstitutionId = institutionId,
                NinPrice = body.NinPrice,
                BvnPrice = body.BvnPrice,
                PhonePrice = body.PhonePrice,
                NinCost = body.NinCost,
                BvnCost = body.BvnCost,
                PhoneCost = body.PhoneCost,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await db.InstitutionPricings.InsertOneAsync(newPricing);
        }

        return Results.Ok(new { message = $"Pricing updated for {institution.Name}." });
    }

    public record UpdateInstitutionPricingBody
    {
        public decimal NinPrice { get; init; } = 100m;
        public decimal BvnPrice { get; init; } = 150m;
        public decimal PhonePrice { get; init; } = 50m;
        public decimal NinCost { get; init; } = 45m;
        public decimal BvnCost { get; init; } = 65m;
        public decimal PhoneCost { get; init; } = 20m;
    }
}
