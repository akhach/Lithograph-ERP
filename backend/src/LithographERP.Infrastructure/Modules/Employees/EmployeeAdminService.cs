using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Employees;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Employees;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LithographERP.Infrastructure.Modules.Employees;

public sealed class EmployeeAdminService(LithographDbContext db) : IEmployeeAdminService
{
    private const int DefaultPageSize = 50;
    private const int MaximumPageSize = 200;

    public Task<EmployeePage<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken = default) =>
        ListCoreAsync(request, selector: false, cancellationToken);

    public async Task<EmployeePage<EmployeeSelectorItem>> ListSelectorAsync(EmployeeListRequest request, CancellationToken cancellationToken = default)
    {
        var page = await ListCoreAsync(request, selector: true, cancellationToken);
        return new EmployeePage<EmployeeSelectorItem>(
            page.Items.Select(item => new EmployeeSelectorItem(item.Id, item.FullName, item.Position)).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems,
            page.TotalPages);
    }

    public async Task<EmployeeDetail> GetAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        ToDetail(await FindAsync(employeeId, cancellationToken));

    public async Task<EmployeeDetail> CreateAsync(Guid actorId, CreateEmployeeRequest request, bool canLinkUser, CancellationToken cancellationToken = default)
    {
        if (request.UserId is not null && !canLinkUser)
        {
            throw Denied();
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FullName = RequireFullName(request.FullName),
            Position = Optional(request.Position, "position", 150),
            Phone = Optional(request.Phone, "phone", 50),
            Email = Optional(request.Email, "email", 200),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actorId,
        };
        if (request.UserId is Guid userId)
        {
            employee.User = await RequireLinkableUserAsync(userId, employee.Id, cancellationToken);
            employee.UserId = userId;
        }

        db.Employees.Add(employee);
        await SaveAsync(cancellationToken);
        return ToDetail(employee);
    }

    public async Task<EmployeeDetail> UpdateAsync(Guid actorId, Guid employeeId, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        employee.FullName = RequireFullName(request.FullName);
        employee.Position = Optional(request.Position, "position", 150);
        employee.Phone = Optional(request.Phone, "phone", 50);
        employee.Email = Optional(request.Email, "email", 200);
        Touch(employee, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(employee);
    }

    public Task<EmployeeDetail> ActivateAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actorId, employeeId, true, cancellationToken);

    public Task<EmployeeDetail> DeactivateAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default) =>
        SetActiveAsync(actorId, employeeId, false, cancellationToken);

    public async Task<EmployeeDetail> LinkUserAsync(Guid actorId, Guid employeeId, Guid userId, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        if (employee.UserId is not null)
        {
            throw new AuthException(
                EmployeeErrorCodes.EmployeeAlreadyLinked,
                "This Employee is already linked to a User. Unlink the current User first.",
                409);
        }

        employee.User = await RequireLinkableUserAsync(userId, employee.Id, cancellationToken);
        employee.UserId = userId;
        Touch(employee, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(employee);
    }

    public async Task<EmployeeDetail> UnlinkUserAsync(Guid actorId, Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        if (employee.UserId is null)
        {
            throw new AuthException(
                EmployeeErrorCodes.UserLinkNotFound,
                "This Employee is not linked to a User.",
                404);
        }

        employee.UserId = null;
        employee.User = null;
        Touch(employee, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(employee);
    }

    public async Task<IReadOnlyList<LinkedUserSummary>> ListAvailableUsersAsync(CancellationToken cancellationToken = default)
    {
        var linkedUserIds = db.Employees.Where(employee => employee.UserId != null).Select(employee => employee.UserId);
        var users = await db.Users
            .Where(user => !linkedUserIds.Contains(user.Id))
            .OrderBy(user => user.Username)
            .Select(user => new LinkedUserSummary(user.Id, user.Username, user.IsActive))
            .ToListAsync(cancellationToken);
        return users;
    }

    private async Task<EmployeePage<EmployeeListItem>> ListCoreAsync(EmployeeListRequest request, bool selector, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? throw AuthException.Validation("page", "Page must be at least 1.") : request.Page;
        var pageSize = request.PageSize < 1 || request.PageSize > MaximumPageSize
            ? throw AuthException.Validation("pageSize", $"Page size must be from 1 to {MaximumPageSize}.")
            : request.PageSize;

        var employees = Filter(db.Employees.AsQueryable(), request);
        var total = await employees.CountAsync(cancellationToken);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        var items = await Order(employees, request)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(employee => new EmployeeListItem(
                employee.Id,
                employee.FullName,
                employee.Position,
                selector ? null : employee.Phone,
                selector ? null : employee.Email,
                employee.IsActive,
                employee.User == null
                    ? null
                    : new LinkedUserSummary(employee.User.Id, employee.User.Username, employee.User.IsActive)))
            .ToListAsync(cancellationToken);

        if (selector)
        {
            items = items.Select(item => item with { Phone = null, Email = null, LinkedUser = null }).ToList();
        }

        return new EmployeePage<EmployeeListItem>(items, page, pageSize, total, totalPages);
    }

    private async Task<EmployeeDetail> SetActiveAsync(Guid actorId, Guid employeeId, bool isActive, CancellationToken cancellationToken)
    {
        var employee = await FindAsync(employeeId, cancellationToken);
        employee.IsActive = isActive;
        Touch(employee, actorId);
        await SaveAsync(cancellationToken);
        return ToDetail(employee);
    }

    private static IQueryable<Employee> Filter(IQueryable<Employee> employees, EmployeeListRequest request)
    {
        if (request.IsActive is bool isActive)
        {
            employees = employees.Where(employee => employee.IsActive == isActive);
        }

        if (request.HasUser is bool hasUser)
        {
            employees = employees.Where(employee => (employee.UserId != null) == hasUser);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = LikePattern(request.Search);
            employees = employees.Where(employee =>
                EF.Functions.ILike(employee.FullName, pattern, "\\")
                || (employee.Position != null && EF.Functions.ILike(employee.Position, pattern, "\\"))
                || (employee.Phone != null && EF.Functions.ILike(employee.Phone, pattern, "\\"))
                || (employee.Email != null && EF.Functions.ILike(employee.Email, pattern, "\\"))
                || (employee.User != null && EF.Functions.ILike(employee.User.Username, pattern, "\\")));
        }

        return employees;
    }

    private static IOrderedQueryable<Employee> Order(IQueryable<Employee> employees, EmployeeListRequest request)
    {
        IOrderedQueryable<Employee> ordered = request.Sort switch
        {
            "full_name" => request.Descending
                ? employees.OrderByDescending(employee => employee.FullName)
                : employees.OrderBy(employee => employee.FullName),
            "position" => request.Descending
                ? employees.OrderByDescending(employee => employee.Position)
                : employees.OrderBy(employee => employee.Position),
            "is_active" => request.Descending
                ? employees.OrderByDescending(employee => employee.IsActive)
                : employees.OrderBy(employee => employee.IsActive),
            _ => throw AuthException.Validation("sort", "Sort must be full_name, position, or is_active."),
        };
        return ordered.ThenBy(employee => employee.Id);
    }

    private async Task<User> RequireLinkableUserAsync(Guid userId, Guid employeeId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AuthErrorCodes.UserNotFound, "The user could not be found.", 404);
        }

        var taken = await db.Employees.AnyAsync(
            employee => employee.UserId == userId && employee.Id != employeeId,
            cancellationToken);
        if (taken)
        {
            throw new AuthException(
                EmployeeErrorCodes.UserAlreadyLinked,
                "This User is already linked to another Employee.",
                409);
        }

        return user;
    }

    private async Task<Employee> FindAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            throw new AuthException(EmployeeErrorCodes.EmployeeNotFound, "The Employee could not be found.", 404);
        }

        return employee;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            if ((postgres.ConstraintName ?? string.Empty).Contains("user_id", StringComparison.Ordinal))
            {
                throw new AuthException(
                    EmployeeErrorCodes.UserAlreadyLinked,
                    "This User is already linked to another Employee.",
                    409);
            }

            throw;
        }
    }

    private static EmployeeDetail ToDetail(Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Position,
            employee.Phone,
            employee.Email,
            employee.IsActive,
            employee.User is null ? null : new LinkedUserSummary(employee.User.Id, employee.User.Username, employee.User.IsActive),
            employee.CreatedAt,
            employee.UpdatedAt);

    private static void Touch(Employee employee, Guid actorId)
    {
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        employee.UpdatedBy = actorId;
    }

    private static string RequireFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AuthException.Validation("fullName", "A value is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            throw AuthException.Validation("fullName", "Must be at most 200 characters.");
        }

        return trimmed;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw AuthException.Validation(field, $"Must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string LikePattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private static AuthException Denied() =>
        new(AuthErrorCodes.PermissionDenied, "You do not have permission to perform this action.", 403);

    public static EmployeeListRequest Normalize(
        string? search,
        bool? isActive,
        bool? hasUser,
        int? page,
        int? pageSize,
        string? sort,
        string? direction,
        string? view)
    {
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "full_name" : sort.Trim().ToLowerInvariant();
        var normalizedDirection = string.IsNullOrWhiteSpace(direction) ? "asc" : direction.Trim().ToLowerInvariant();
        if (normalizedDirection is not ("asc" or "desc"))
        {
            throw AuthException.Validation("direction", "Direction must be asc or desc.");
        }

        var normalizedView = string.IsNullOrWhiteSpace(view) ? "list" : view.Trim().ToLowerInvariant();
        if (normalizedView is not ("list" or "selector"))
        {
            throw AuthException.Validation("view", "View must be list or selector.");
        }

        return new EmployeeListRequest(
            string.IsNullOrWhiteSpace(search) ? null : search,
            isActive,
            hasUser,
            page ?? 1,
            pageSize ?? DefaultPageSize,
            normalizedSort,
            normalizedDirection == "desc",
            normalizedView == "selector");
    }
}
