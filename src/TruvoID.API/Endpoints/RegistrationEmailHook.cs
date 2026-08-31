using TruvoID.Infrastructure.Services;

namespace TruvoID.API.Endpoints;

/// <summary>
/// Drop this call into the existing registration handler immediately after
/// the institution and admin user are persisted to the database.
///
/// Usage — add to the end of your POST /v1/auth/register success path:
///
///   await RegistrationEmailHook.SendWelcomeAsync(
///       notifications,
///       adminEmail: user.Email,
///       adminName: user.FullName ?? "Admin",
///       institutionName: institution.Name);
/// </summary>
public static class RegistrationEmailHook
{
    public static async Task SendWelcomeAsync(
        INotificationService notifications,
        string adminEmail,
        string adminName,
        string institutionName)
    {
        // Fire-and-forget: don't let email failure block the registration response
        _ = Task.Run(async () =>
        {
            await notifications.SendWelcomeAsync(adminEmail, adminName, institutionName);
        });

        await Task.CompletedTask;
    }
}
