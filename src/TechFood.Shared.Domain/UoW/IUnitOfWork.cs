using System.Threading.Tasks;

namespace TechFood.Shared.Domain.UoW;

public interface IUnitOfWork
{
    Task<bool> CommitAsync();

    Task RollbackAsync();
}
