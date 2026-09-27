using LithographERP.Application.Modules.Numbering;
using LithographERP.Domain.Modules.Clients;
using LithographERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LithographERP.Infrastructure.Modules.Numbering;

public sealed class BusinessIdGenerator(LithographDbContext db) : IBusinessIdGenerator
{
    public const string ClientSequenceName = "clients.client_business_id_seq";

    public async Task<string> GenerateClientBusinessIdAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT nextval('{ClientSequenceName}')";
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        return ClientBusinessIds.Format(value);
    }
}
