using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TruvoID.Core.DTOs;
using TruvoID.Infrastructure.Services;

namespace TruvoID.API.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/settings/notifications")
            .RequireAuthorization();

        group.MapGet("/", GetNotificationPreferences);
        group.MapPost("/", UpdateNotificationPreferences);

        return app;
    }

    private static async Task<IResult> GetNotificationPreferences(
        HttpContext ctx,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        var prefs = await prefService.GetOrCreateAsync(institutionId);

        return Results.Ok(new NotificationPreferencesDto
        {
            AlertThreshold = prefs.AlertThreshold,
            EmailAlerts = prefs.EmailAlertsEnabled,
            SmsAlerts = prefs.SmsAlertsEnabled,
            VerifyEmailResults = prefs.VerifyEmailResults
        });
    }

    private static async Task<IResult> UpdateNotificationPreferences(
        HttpContext ctx,
        UpdateNotificationPreferencesRequest req,
        NotificationPreferenceService prefService)
    {
        var institutionId = ctx.GetInstitutionId();
        if (institutionId == Guid.Empty) return Results.Unauthorized();

        await prefService.UpdateNotificationPrefsAsync(
            institutionId,
            req.AlertThreshold,
            req.EmailAlerts,
            req.SmsAlerts,
            req.VerifyEmailResults);

        return Results.Ok(new { message = "Notification preferences updated." });
    }
}
