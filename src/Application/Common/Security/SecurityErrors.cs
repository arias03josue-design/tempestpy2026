using Tempest.Application.Common.Results;

namespace Tempest.Application.Common.Security;

public static class SecurityErrors
{
    public static readonly Error AuthenticationRequired =
        Error.Unauthorized("auth.required", "Tu sesión expiró. Vuelve a iniciar sesión para continuar.");
}
