namespace LithographERP.Domain.Modules.Authentication;

public sealed record PermissionDefinition(string Code, string Name, string Module, string Description);

public static class PermissionCatalog
{
    public static class Users
    {
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Edit = "users.edit";
        public const string Activate = "users.activate";
        public const string ResetPassword = "users.reset_password";
        public const string ManageRoles = "users.manage_roles";
    }

    public static class Roles
    {
        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Edit = "roles.edit";
        public const string ManagePermissions = "roles.manage_permissions";
    }

    public static class Employees
    {
        public const string View = "employees.view";
        public const string Create = "employees.create";
        public const string Edit = "employees.edit";
        public const string Activate = "employees.activate";
        public const string LinkUser = "employees.link_user";
    }

    public static class Clients
    {
        public const string View = "clients.view";
        public const string Create = "clients.create";
        public const string Edit = "clients.edit";
        public const string Activate = "clients.activate";
    }

    public static class Projects
    {
        public const string View = "projects.view";
        public const string Create = "projects.create";
        public const string Edit = "projects.edit";
        public const string ManageTeam = "projects.manage_team";
        public const string ChangeStatus = "projects.change_status";
    }

    public static class Orders
    {
        public const string View = "orders.view";
        public const string Create = "orders.create";
        public const string Edit = "orders.edit";
        public const string ChangeStatus = "orders.change_status";
        public const string ManageChecklist = "orders.manage_checklist";
        public const string ManageFolderLinks = "orders.manage_folder_links";
        public const string ManageTypes = "orders.manage_types";
        public const string ViewSellingPrice = "orders.view_selling_price";
        public const string ViewCostPrice = "orders.view_cost_price";
    }

    public static class Calculator
    {
        public const string View = "calculator.view";
        public const string Edit = "calculator.edit";
        public const string ManageTemplates = "calculator.manage_templates";
        public const string PublishTemplates = "calculator.publish_templates";
        public const string ViewCosts = "calculator.view_costs";
        public const string EditCosts = "calculator.edit_costs";
    }

    public static class Reports
    {
        public const string View = "reports.view";
    }

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        Def(Users.View, "View users", "Users", "View user accounts, roles, and active state."),
        Def(Users.Create, "Create users", "Users", "Create local ERP user accounts."),
        Def(Users.Edit, "Edit users", "Users", "Edit a user's username."),
        Def(Users.Activate, "Activate or deactivate users", "Users", "Activate or deactivate a user account."),
        Def(Users.ResetPassword, "Reset passwords", "Users", "Set a new password for another user."),
        Def(Users.ManageRoles, "Manage user roles", "Users", "Assign or remove roles on a user."),
        Def(Roles.View, "View roles", "Roles", "View roles and their permissions."),
        Def(Roles.Create, "Create roles", "Roles", "Create roles."),
        Def(Roles.Edit, "Edit roles", "Roles", "Edit a role name and description."),
        Def(Roles.ManagePermissions, "Manage role permissions", "Roles", "Assign or remove permissions on a role."),
        Def(Employees.View, "View employees", "Employees", "View employee records."),
        Def(Employees.Create, "Create employees", "Employees", "Create employee records."),
        Def(Employees.Edit, "Edit employees", "Employees", "Edit employee records."),
        Def(Employees.Activate, "Activate or deactivate employees", "Employees", "Activate or deactivate an employee."),
        Def(Employees.LinkUser, "Link employees to users", "Employees", "Link or unlink an employee and a user account."),
        Def(Clients.View, "View clients", "Clients", "View client records."),
        Def(Clients.Create, "Create clients", "Clients", "Create client records."),
        Def(Clients.Edit, "Edit clients", "Clients", "Edit client records."),
        Def(Clients.Activate, "Activate or deactivate clients", "Clients", "Activate or deactivate a client."),
        Def(Projects.View, "View projects", "Projects", "View projects."),
        Def(Projects.Create, "Create projects", "Projects", "Create projects."),
        Def(Projects.Edit, "Edit projects", "Projects", "Edit project details."),
        Def(Projects.ManageTeam, "Manage project teams", "Projects", "Manage project team membership."),
        Def(Projects.ChangeStatus, "Change project status", "Projects", "Change project status."),
        Def(Orders.View, "View orders", "Orders", "View orders."),
        Def(Orders.Create, "Create orders", "Orders", "Create orders."),
        Def(Orders.Edit, "Edit orders", "Orders", "Edit order details."),
        Def(Orders.ChangeStatus, "Change order status", "Orders", "Change order status."),
        Def(Orders.ManageChecklist, "Manage order checklists", "Orders", "Manage order checklist items."),
        Def(Orders.ManageFolderLinks, "Manage folder links", "Orders", "Manage order folder links."),
        Def(Orders.ManageTypes, "Manage order types", "Orders", "Manage order types."),
        Def(Orders.ViewSellingPrice, "View selling prices", "Orders", "View selling prices."),
        Def(Orders.ViewCostPrice, "View cost prices", "Orders", "View aggregate cost prices."),
        Def(Calculator.View, "View calculators", "Calculator", "View order calculator structure and values."),
        Def(Calculator.Edit, "Edit calculators", "Calculator", "Edit permitted calculator inputs."),
        Def(Calculator.ManageTemplates, "Manage calculator templates", "Calculator", "Manage calculator templates and drafts."),
        Def(Calculator.PublishTemplates, "Publish calculator templates", "Calculator", "Publish a calculator template version."),
        Def(Calculator.ViewCosts, "View cost items", "Calculator", "View detailed cost items."),
        Def(Calculator.EditCosts, "Edit cost items", "Calculator", "Create, edit, or delete cost items."),
        Def(Reports.View, "View reports", "Reports", "Access the reports area."),
    ];

    public static readonly IReadOnlySet<string> Codes = All.Select(permission => permission.Code).ToHashSet(StringComparer.Ordinal);

    private static PermissionDefinition Def(string code, string name, string module, string description) =>
        new(code, name, module, description);
}
