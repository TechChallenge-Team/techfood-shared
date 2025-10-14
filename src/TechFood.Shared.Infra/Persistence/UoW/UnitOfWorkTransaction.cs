using System.Collections.Generic;
using System.Threading.Tasks;
using TechFood.Shared.Domain.UoW;

namespace TechFood.Shared.Infra.Persistence.UoW;

public class UnitOfWorkTransaction(IEnumerable<IUnitOfWork> uows) : IUnitOfWorkTransaction
{
    private readonly IEnumerable<IUnitOfWork> _uows = uows;

    public async Task<bool> CommitAsync()
    {
        foreach (var uow in _uows)
        {
            await uow.CommitAsync();
        }

        return await Task.FromResult(true);
    }

    public async Task RollbackAsync()
    {
        foreach (var uow in _uows)
        {
            await uow.RollbackAsync();
        }
    }
}
