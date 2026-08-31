using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TruvoID.Core.DTOs;
using TruvoID.Infrastructure.Services;

namespace TruvoID.API.Endpoints;

public static class WalletAlertEndpoints
{
    public static IEndpointRouteBuilder MapWalletAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var alertGroup = app.MapGroup("/v1/wallet/alerts")
            .RequireAuthorization();

        alertGroup.MapGet("/", GetWalletAlerts);
        alertGroup.MapPost("/", UpdateWalletAlerts);

        var billingGroup = app.MapGroup("/v1/wallet/billing-contact")
            .RequireAuthorization();

        billingGroup.MapGet("/", GetBillingContact);
        billingGroup.MapPost("/", UpdateBillingContact);

        return app;
    }

    private static async Task<IResult> GetWalletAlerts(
        HttpContext ctx,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        var prefs = await prefService.GetOrCreateAsync(institutionId);

        return Results.Ok(new WalletAlertSettingsDto
        {
            Threshold = prefs.AlertThreshold,
            EmailEnabled = prefs.EmailAlertsEnabled,
            SmsEnabled = prefs.SmsAlertsEnabled
        });
    }

    private static async Task<IResult> UpdateWalletAlerts(
        HttpContext ctx,
        UpdateWalletAlertsRequest req,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        await prefService.UpdateWalletAlertsAsync(
            institutionId,
            req.Threshold,
            req.EmailEnabled,
            req.SmsEnabled);

        return Results.Ok(new { message = "Wallet alert settings saved." });
    }

    private static async Task<IResult> GetBillingContact(
        HttpContext ctx,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        var prefs = await prefService.GetOrCreateAsync(institutionId);

        return Results.Ok(new BillingContactDto
        {
            Name = prefs.BillingContactName ?? string.Empty,
            Email = prefs.BillingContactEmail ?? string.Empty
        });
    }

    private static async Task<IResult> UpdateBillingContact(
        HttpContext ctx,
        UpdateBillingContactRequest req,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        await prefService.UpdateBillingContactAsync(
            institutionId,
            req.Name,
            req.Email);

        return Results.Ok(new { message = "Billing contact updated." });
    }
}
