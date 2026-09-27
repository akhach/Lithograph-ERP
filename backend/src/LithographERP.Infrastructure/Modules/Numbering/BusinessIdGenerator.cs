using LithographERP.Application.Modules.Numbering;
using LithographERP.Domain.Modules.Clients;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LithographERP.Infrastructure.Modules.Numbering;

public sealed class BusinessIdGenerator(LithographDbContext db, TimeProvider time) : IBusinessIdGenerator
{
    public const string ClientSequenceName = "clients.client_business_id_seq";

    public async Task<string> GenerateClientBusinessIdAsync(CancellationToken cancellationToken = default)
    {
        var value = await NextValueAsync($"SELECT nextval('{ClientSequenceName}')", cancellationToken);
        return ClientBusinessIds.Format(value);
    }

    public async Task<string> GenerateProjectBusinessIdAsync(CancellationToken cancellationToken = default)
    {
        var year = time.GetUtcNow().Year;
        var value = await NextValueAsync("SELECT projects.next_project_business_id($1)", cancellationToken, year);
        return ProjectBusinessIds.Format(year, value);
    }

    public async Task<string> GenerateOrderBusinessIdAsync(CancellationToken cancellationToken = default)
    {
        var year = time.GetUtcNow().Year;
        var value = await NextValueAsync("SELECT orders.next_order_business_id($1)", cancellationToken, year);
        return OrderBusinessIds.Format(year, value);
    }

    private async Task<long> NextValueAsync(string sql, CancellationToken cancellationToken, int? year = null)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        if (year is not null)
        {
            var parameter = command.CreateParameter();
            parameter.Value = year.Value;
            command.Parameters.Add(parameter);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }
}
