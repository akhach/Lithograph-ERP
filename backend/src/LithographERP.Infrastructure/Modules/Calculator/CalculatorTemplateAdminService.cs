using System.Data;
using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Calculator;

public sealed class CalculatorTemplateAdminService(LithographDbContext db, TimeProvider time)
    : ICalculatorTemplateAdminService, ICalculatorTemplateLookup
{
    public async Task<IReadOnlyList<CalculatorTemplateListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var templates = await db.CalculatorTemplates.AsNoTracking()
            .OrderBy(template => template.Name)
            .ThenBy(template => template.Id)
            .ToListAsync(cancellationToken);
        var markers = await LoadMarkersAsync(templates.Select(template => template.Id).ToArray(), cancellationToken);
        return templates.Select(template => ToListItem(template, markers)).ToArray();
    }

    public async Task<IReadOnlyList<CalculatorTemplateOption>> ListActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await db.CalculatorTemplates.AsNoTracking()
            .Where(template => template.IsActive)
            .OrderBy(template => template.Name)
            .ThenBy(template => template.Id)
            .Select(template => new CalculatorTemplateOption(template.Id, template.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<CalculatorTemplateDetail> GetAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        await DetailAsync(templateId, cancellationToken);

    public async Task<CalculatorTemplateDetail> CreateAsync(
        Guid actorId,
        SaveCalculatorTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = RequireName(request.Name);
        var now = time.GetUtcNow();
        var template = new CalculatorTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = Text(request.Description),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorId,
        };
        var version = new CalculatorTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 1,
            Status = TemplateVersionStatuses.Draft,
            Definition = CalculatorTemplateDefinition.Empty(),
            CreatedAt = now,
            CreatedBy = actorId,
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.CalculatorTemplates.Add(template);
        db.CalculatorTemplateVersions.Add(version);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await DetailAsync(template.Id, cancellationToken);
    }

    public async Task<CalculatorTemplateDetail> UpdateAsync(
        Guid actorId,
        Guid templateId,
        SaveCalculatorTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadTemplateAsync(templateId, cancellationToken);
        template.Name = RequireName(request.Name);
        template.Description = Text(request.Description);
        Touch(template, actorId);
        await SaveAsync(cancellationToken);
        return await DetailAsync(template.Id, cancellationToken);
    }

    public async Task<CalculatorTemplateDetail> SetActiveAsync(
        Guid actorId,
        Guid templateId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var template = await LoadTemplateAsync(templateId, cancellationToken);
        if (template.IsActive != isActive)
        {
            template.IsActive = isActive;
            Touch(template, actorId);
            await SaveAsync(cancellationToken);
        }

        return await DetailAsync(template.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<CalculatorTemplateVersionSummary>> ListVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTemplateExistsAsync(templateId, cancellationToken);
        var versions = await db.CalculatorTemplateVersions.AsNoTracking()
            .Where(version => version.TemplateId == templateId)
            .OrderBy(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        return versions.Select(ToSummary).ToArray();
    }

    public async Task<CalculatorTemplateVersionDetail> GetVersionAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await db.CalculatorTemplateVersions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId && candidate.TemplateId == templateId, cancellationToken);
        if (version is null)
        {
            throw VersionNotFound();
        }

        return ToVersion(version);
    }

    public async Task<CalculatorTemplateVersionDetail> CreateVersionAsync(
        Guid actorId,
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockTemplateAsync(templateId, cancellationToken);
        var versions = await db.CalculatorTemplateVersions
            .Where(version => version.TemplateId == templateId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        if (versions.Any(version => version.Status == TemplateVersionStatuses.Draft))
        {
            throw DraftExists();
        }

        var source = versions.FirstOrDefault(version => version.Status == TemplateVersionStatuses.Published)
            ?? versions.FirstOrDefault();
        var nextNumber = (versions.FirstOrDefault()?.VersionNumber ?? 0) + 1;
        var created = new CalculatorTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            VersionNumber = nextNumber,
            Status = TemplateVersionStatuses.Draft,
            Definition = source is null
                ? CalculatorTemplateDefinition.Empty()
                : CalculatorDefinitionJson.Clone(source.Definition),
            CreatedAt = time.GetUtcNow(),
            CreatedBy = actorId,
        };
        db.CalculatorTemplateVersions.Add(created);
        await TouchLoadedTemplateAsync(templateId, actorId, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToVersion(created);
    }

    public async Task<CalculatorTemplateVersionDetail> UpdateDraftAsync(
        Guid actorId,
        Guid templateId,
        Guid versionId,
        CalculatorTemplateDefinition definition,
        CancellationToken cancellationToken = default)
    {
        var version = await LoadVersionAsync(templateId, versionId, cancellationToken);
        if (version.Status != TemplateVersionStatuses.Draft)
        {
            throw Immutable();
        }

        TemplateDefinitionValidator.EnsureCanStore(definition);
        version.Definition = definition;
        await TouchLoadedTemplateAsync(templateId, actorId, cancellationToken);
        await SaveAsync(cancellationToken);
        return ToVersion(version);
    }

    public async Task<TemplateValidationResult> ValidateAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await db.CalculatorTemplateVersions.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId && candidate.TemplateId == templateId, cancellationToken);
        if (version is null)
        {
            throw VersionNotFound();
        }

        return TemplateDefinitionValidator.Validate(version.Definition);
    }

    public async Task<CalculatorTemplateVersionDetail> PublishAsync(
        Guid actorId,
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockTemplateAsync(templateId, cancellationToken);
        var version = await LoadVersionAsync(templateId, versionId, cancellationToken);
        if (version.Status != TemplateVersionStatuses.Draft)
        {
            throw new CalculatorRequestException(
                CalculatorErrorCodes.PublishFailed,
                "Only a Draft Version can be published.",
                409);
        }

        var validation = TemplateDefinitionValidator.Validate(version.Definition);
        if (!validation.IsValid)
        {
            if (validation.Errors.All(issue => issue.Code == CalculatorIssueCodes.UnsupportedSchemaVersion))
            {
                throw new CalculatorRequestException(
                    CalculatorErrorCodes.SchemaVersionUnsupported,
                    "This Calculator Template uses an unsupported definition version.",
                    409,
                    validation.Errors);
            }

            throw new CalculatorRequestException(
                CalculatorErrorCodes.ValidationFailed,
                "The Calculator Template is not valid.",
                400,
                validation.Errors);
        }

        var previous = await db.CalculatorTemplateVersions
            .Where(candidate => candidate.TemplateId == templateId && candidate.Status == TemplateVersionStatuses.Published)
            .ToListAsync(cancellationToken);
        foreach (var published in previous)
        {
            published.Status = TemplateVersionStatuses.Retired;
        }

        version.Status = TemplateVersionStatuses.Published;
        version.PublishedAt = time.GetUtcNow();
        version.PublishedBy = actorId;
        await TouchLoadedTemplateAsync(templateId, actorId, cancellationToken);
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToVersion(version);
    }

    public async Task<CalculatorTemplateReference?> FindAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await db.CalculatorTemplates.AsNoTracking()
            .Where(template => template.Id == templateId)
            .Select(template => new CalculatorTemplateReference(template.Id, template.Name, template.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CalculatorTemplateReference>> FindManyAsync(
        IReadOnlyCollection<Guid> templateIds,
        CancellationToken cancellationToken = default)
    {
        if (templateIds.Count == 0)
        {
            return new Dictionary<Guid, CalculatorTemplateReference>();
        }

        var templates = await db.CalculatorTemplates.AsNoTracking()
            .Where(template => templateIds.Contains(template.Id))
            .Select(template => new CalculatorTemplateReference(template.Id, template.Name, template.IsActive))
            .ToListAsync(cancellationToken);
        return templates.ToDictionary(template => template.Id);
    }

    private async Task<CalculatorTemplateDetail> DetailAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await db.CalculatorTemplates.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == templateId, cancellationToken);
        if (template is null)
        {
            throw TemplateNotFound();
        }

        var versions = await db.CalculatorTemplateVersions.AsNoTracking()
            .Where(version => version.TemplateId == templateId)
            .OrderBy(version => version.VersionNumber)
            .ToListAsync(cancellationToken);
        return ToDetail(template, versions);
    }

    private async Task<List<VersionMarker>> LoadMarkersAsync(Guid[] templateIds, CancellationToken cancellationToken)
    {
        if (templateIds.Length == 0)
        {
            return [];
        }

        return await db.CalculatorTemplateVersions.AsNoTracking()
            .Where(version => templateIds.Contains(version.TemplateId))
            .Select(version => new VersionMarker(version.TemplateId, version.Id, version.VersionNumber, version.Status))
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureTemplateExistsAsync(Guid templateId, CancellationToken cancellationToken)
    {
        if (!await db.CalculatorTemplates.AsNoTracking().AnyAsync(template => template.Id == templateId, cancellationToken))
        {
            throw TemplateNotFound();
        }
    }

    private async Task<CalculatorTemplate> LoadTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await db.CalculatorTemplates.SingleOrDefaultAsync(candidate => candidate.Id == templateId, cancellationToken);
        if (template is null)
        {
            throw TemplateNotFound();
        }

        return template;
    }

    private async Task<CalculatorTemplateVersion> LoadVersionAsync(
        Guid templateId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await db.CalculatorTemplateVersions
            .SingleOrDefaultAsync(candidate => candidate.Id == versionId && candidate.TemplateId == templateId, cancellationToken);
        if (version is null)
        {
            throw VersionNotFound();
        }

        return version;
    }

    private async Task TouchLoadedTemplateAsync(Guid templateId, Guid actorId, CancellationToken cancellationToken)
    {
        var template = await db.CalculatorTemplates.SingleOrDefaultAsync(candidate => candidate.Id == templateId, cancellationToken);
        if (template is null)
        {
            throw TemplateNotFound();
        }

        Touch(template, actorId);
    }

    private async Task LockTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM calculator.templates WHERE id = @id FOR UPDATE";
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = templateId;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw TemplateNotFound();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnique(exception, "templates_name"))
        {
            throw NameExists();
        }
        catch (DbUpdateException exception) when (IsUnique(exception, "template_versions_one_draft"))
        {
            throw DraftExists();
        }
        catch (DbUpdateException exception) when (IsUnique(exception, "template_versions_template_id_version_number"))
        {
            throw DraftExists();
        }
    }

    private static bool IsUnique(DbUpdateException exception, string constraintFragment) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation
        && (postgres.ConstraintName ?? string.Empty).Contains(constraintFragment, StringComparison.Ordinal);

    private void Touch(CalculatorTemplate template, Guid actorId)
    {
        template.UpdatedAt = time.GetUtcNow();
        template.UpdatedBy = actorId;
    }

    private static string RequireName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CalculatorRequestException.Field("name", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > CalculatorLimits.TemplateNameMaxLength)
        {
            throw CalculatorRequestException.Field("name", $"Must be at most {CalculatorLimits.TemplateNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > CalculatorLimits.DescriptionMaxLength)
        {
            throw CalculatorRequestException.Field("description", $"Must be at most {CalculatorLimits.DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    private static CalculatorTemplateListItem ToListItem(CalculatorTemplate template, IReadOnlyList<VersionMarker> markers)
    {
        var owned = markers.Where(marker => marker.TemplateId == template.Id).ToArray();
        var published = owned.Where(marker => marker.Status == TemplateVersionStatuses.Published)
            .Select(marker => (int?)marker.VersionNumber)
            .Max();
        var draft = owned.SingleOrDefault(marker => marker.Status == TemplateVersionStatuses.Draft);
        return new CalculatorTemplateListItem(
            template.Id,
            template.Name,
            template.Description,
            template.IsActive,
            published,
            draft?.VersionNumber,
            template.CreatedAt,
            template.UpdatedAt);
    }

    private static CalculatorTemplateDetail ToDetail(CalculatorTemplate template, IReadOnlyList<CalculatorTemplateVersion> versions)
    {
        var published = versions.Where(version => version.Status == TemplateVersionStatuses.Published)
            .Select(version => (int?)version.VersionNumber)
            .Max();
        var draft = versions.SingleOrDefault(version => version.Status == TemplateVersionStatuses.Draft);
        return new CalculatorTemplateDetail(
            template.Id,
            template.Name,
            template.Description,
            template.IsActive,
            published,
            draft?.VersionNumber,
            draft?.Id,
            template.CreatedAt,
            template.UpdatedAt,
            versions.Select(ToSummary).ToArray());
    }

    private static CalculatorTemplateVersionSummary ToSummary(CalculatorTemplateVersion version) =>
        new(version.Id, version.VersionNumber, version.Status, version.CreatedAt, version.PublishedAt, version.PublishedBy);

    private static CalculatorTemplateVersionDetail ToVersion(CalculatorTemplateVersion version) =>
        new(
            version.Id,
            version.TemplateId,
            version.VersionNumber,
            version.Status,
            version.Definition,
            version.CreatedAt,
            version.CreatedBy,
            version.PublishedAt,
            version.PublishedBy);

    private static CalculatorRequestException TemplateNotFound() =>
        new(CalculatorErrorCodes.TemplateNotFound, "The Calculator Template could not be found.", 404);

    private static CalculatorRequestException VersionNotFound() =>
        new(CalculatorErrorCodes.VersionNotFound, "The Template Version could not be found.", 404);

    private static CalculatorRequestException Immutable() =>
        new(
            CalculatorErrorCodes.VersionImmutable,
            "Published and retired Template Versions cannot be edited.",
            409);

    private static CalculatorRequestException DraftExists() =>
        new(
            CalculatorErrorCodes.DraftAlreadyExists,
            "This Calculator Template already has a Draft Version.",
            409);

    private static CalculatorRequestException NameExists() =>
        new(
            CalculatorErrorCodes.TemplateNameAlreadyExists,
            "A Calculator Template with that name already exists.",
            409);

    private sealed record VersionMarker(Guid TemplateId, Guid Id, int VersionNumber, string Status);
}
