using Tempest.Application.Common.Messaging;
using Tempest.Application.Maintenance.Commands;

namespace Tempest.WebUI.App.Security;

internal static class BackupEndpoints
{
    /// <summary>
    /// GET /admin/db/backup → downloads a .zip with a consistent snapshot of the database and the uploaded images.
    /// A plain HTTP endpoint (not the Blazor circuit) so large files stream straight to the browser.
    /// </summary>
    public static IEndpointRouteBuilder MapBackupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/db/backup", async (ISender sender, HttpContext context) =>
        {
            var result = await sender.Send(new CreateBackupCommand(), context.RequestAborted);
            if (result.IsFailure)
            {
                return Results.Problem(result.Error.Message, statusCode: StatusCodes.Status500InternalServerError);
            }

            // The temporary archive is deleted as soon as the response stream is closed.
            var archive = new FileStream(
                result.Value.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                81_920,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);

            context.Response.Headers.CacheControl = "no-store";
            return Results.File(archive, "application/zip", result.Value.FileName);
        })
        .RequireAuthorization();

        return endpoints;
    }
}
