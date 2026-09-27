using LithographERP.Domain.Modules.Calculator;

namespace LithographERP.Application.Modules.Calculator;

public static class CalculatorErrorCodes
{
    public const string TemplateNotFound = "CALCULATOR_TEMPLATE_NOT_FOUND";
    public const string TemplateInactive = "CALCULATOR_TEMPLATE_INACTIVE";
    public const string TemplateNameAlreadyExists = "CALCULATOR_TEMPLATE_NAME_ALREADY_EXISTS";
    public const string VersionNotFound = "TEMPLATE_VERSION_NOT_FOUND";
    public const string VersionImmutable = "TEMPLATE_VERSION_IMMUTABLE";
    public const string DraftAlreadyExists = "TEMPLATE_DRAFT_ALREADY_EXISTS";
    public const string ValidationFailed = "TEMPLATE_VALIDATION_FAILED";
    public const string PublishFailed = "TEMPLATE_PUBLISH_FAILED";
    public const string SchemaVersionUnsupported = "TEMPLATE_SCHEMA_VERSION_UNSUPPORTED";
}

public static class CalculatorIssueCodes
{
    public const string InvalidFieldKey = "INVALID_FIELD_KEY";
    public const string DuplicateFieldKey = "DUPLICATE_FIELD_KEY";
    public const string UnknownFieldReference = "UNKNOWN_FIELD_REFERENCE";
    public const string CircularReference = "CIRCULAR_REFERENCE";
    public const string UnknownFunction = "UNKNOWN_FUNCTION";
    public const string InvalidFormulaSyntax = "INVALID_FORMULA_SYNTAX";
    public const string InvalidFormulaArguments = "INVALID_FORMULA_ARGUMENTS";
    public const string SellingPriceFieldMissing = "SELLING_PRICE_FIELD_MISSING";
    public const string SellingPriceFieldInvalid = "SELLING_PRICE_FIELD_INVALID";
    public const string UnknownElementType = "UNKNOWN_ELEMENT_TYPE";
    public const string InvalidDropdown = "INVALID_DROPDOWN";
    public const string InvalidVisibility = "INVALID_VISIBILITY";
    public const string MissingRequiredProperty = "MISSING_REQUIRED_PROPERTY";
    public const string InvalidElement = "INVALID_ELEMENT";
    public const string DuplicateElementId = "DUPLICATE_ELEMENT_ID";
    public const string UnsupportedSchemaVersion = CalculatorErrorCodes.SchemaVersionUnsupported;
}

public static class FormulaErrorCodes
{
    public const string DivisionByZero = "DIVISION_BY_ZERO";
    public const string MissingValue = "MISSING_VALUE";
    public const string InvalidArguments = CalculatorIssueCodes.InvalidFormulaArguments;
    public const string NumericOverflow = "NUMERIC_OVERFLOW";
}

public sealed record CalculatorValidationIssue(
    string Code,
    string Message,
    string? ElementId = null,
    string? FieldKey = null);

public sealed record TemplateValidationResult(bool IsValid, IReadOnlyList<CalculatorValidationIssue> Errors)
{
    public static TemplateValidationResult Valid { get; } = new(true, []);
}

public sealed class CalculatorRequestException(
    string code,
    string message,
    int statusCode,
    IReadOnlyList<CalculatorValidationIssue>? validation = null,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public string Code { get; } = code;

    public int StatusCode { get; } = statusCode;

    public IReadOnlyList<CalculatorValidationIssue>? Validation { get; } = validation;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;

    public static CalculatorRequestException Field(string field, string message) =>
        new(
            "VALIDATION_FAILED",
            "One or more fields are invalid.",
            400,
            errors: new Dictionary<string, string[]> { [field] = [message] });
}

public sealed record CalculatorTemplateListItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int? LatestPublishedVersion,
    int? DraftVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CalculatorTemplateOption(Guid Id, string Name);

public sealed record CalculatorTemplateVersionSummary(
    Guid Id,
    int VersionNumber,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy);

public sealed record CalculatorTemplateDetail(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int? LatestPublishedVersion,
    int? DraftVersion,
    Guid? DraftVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<CalculatorTemplateVersionSummary> Versions);

public sealed record CalculatorTemplateVersionDetail(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    string Status,
    CalculatorTemplateDefinition Definition,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? PublishedAt,
    Guid? PublishedBy);

public sealed record SaveCalculatorTemplateRequest(string Name, string? Description);

public sealed record CalculatorTemplateReference(Guid Id, string Name, bool IsActive);

public interface ICalculatorTemplateLookup
{
    Task<CalculatorTemplateReference?> FindAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, CalculatorTemplateReference>> FindManyAsync(
        IReadOnlyCollection<Guid> templateIds,
        CancellationToken cancellationToken = default);
}

public interface ICalculatorTemplateAdminService
{
    Task<IReadOnlyList<CalculatorTemplateListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalculatorTemplateOption>> ListActiveOptionsAsync(CancellationToken cancellationToken = default);

    Task<CalculatorTemplateDetail> GetAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<CalculatorTemplateDetail> CreateAsync(
        Guid actorId,
        SaveCalculatorTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateDetail> UpdateAsync(
        Guid actorId,
        Guid templateId,
        SaveCalculatorTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateDetail> SetActiveAsync(
        Guid actorId,
        Guid templateId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalculatorTemplateVersionSummary>> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateVersionDetail> GetVersionAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateVersionDetail> CreateVersionAsync(
        Guid actorId,
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateVersionDetail> UpdateDraftAsync(
        Guid actorId,
        Guid templateId,
        Guid versionId,
        CalculatorTemplateDefinition definition,
        CancellationToken cancellationToken = default);

    Task<TemplateValidationResult> ValidateAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<CalculatorTemplateVersionDetail> PublishAsync(
        Guid actorId,
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default);
}
