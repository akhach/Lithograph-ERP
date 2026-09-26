using System.Reflection;

namespace LithographERP.UnitTests;

public class ArchitectureDependencyTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
    ];

    [Theory]
    [InlineData("LithographERP.Domain")]
    [InlineData("LithographERP.Application")]
    public void DomainAndApplication_DoNotReferenceFrameworkAssemblies(string assemblyName)
    {
        var assembly = Assembly.Load(new AssemblyName(assemblyName));

        var forbiddenReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => ForbiddenAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }
}
