namespace LithographERP.Application.Modules.Authentication;

public static class AuthErrorCodes
{
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string SessionInvalid = "SESSION_INVALID";
    public const string SessionExpired = "SESSION_EXPIRED";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string SetupAlreadyCompleted = "SETUP_ALREADY_COMPLETED";
    public const string UsernameAlreadyExists = "USERNAME_ALREADY_EXISTS";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string FinalDirectorRequired = "FINAL_DIRECTOR_REQUIRED";
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string RoleNameAlreadyExists = "ROLE_NAME_ALREADY_EXISTS";
    public const string SystemRoleProtected = "SYSTEM_ROLE_PROTECTED";
    public const string RoleAlreadyAssigned = "ROLE_ALREADY_ASSIGNED";
    public const string RoleNotAssigned = "ROLE_NOT_ASSIGNED";
    public const string PermissionNotFound = "PERMISSION_NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
}

public sealed class AuthException(
    string code,
    string message,
    int statusCode,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;

    public static AuthException Validation(string field, string message) =>
        new(
            AuthErrorCodes.ValidationFailed,
            "One or more fields are invalid.",
            400,
            new Dictionary<string, string[]> { [field] = [message] });
}
