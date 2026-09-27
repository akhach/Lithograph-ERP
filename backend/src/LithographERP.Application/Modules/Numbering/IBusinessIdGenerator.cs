namespace LithographERP.Application.Modules.Numbering;

public interface IBusinessIdGenerator
{
    Task<string> GenerateClientBusinessIdAsync(CancellationToken cancellationToken = default);
}
