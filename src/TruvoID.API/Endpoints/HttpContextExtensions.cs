using Microsoft.AspNetCore.Http;

namespace TruvoID.API.Endpoints;

public static class HttpContextExtensions
{
    public static Guid GetInstitutionId(this HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("institution_id")
            ?? ctx.User.FindFirst("institutionId");

        if (claim is null || !Guid.TryParse(claim.Value, out var id))
            return Guid.Empty;

        return id;
    }

    public static Guid GetUserId(this HttpContext ctx)
    {
        var claim = ctx.User.FindFirst("sub")
            ?? ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim is null || !Guid.TryParse(claim.Value, out var id))
            return Guid.Empty;

        return id;
    }
}
