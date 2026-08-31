using Microsoft.AspNetCore.Routing;

namespace TruvoID.API.Endpoints;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapTruvoIdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapNotificationEndpoints();
        app.MapWalletAlertEndpoints();
        app.MapPasswordResetEndpoints();
        app.MapAdminApprovalEndpoints();

        return app;
    }
}
