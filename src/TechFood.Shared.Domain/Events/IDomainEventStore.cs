using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechFood.Shared.Domain.Events;

public interface IDomainEventStore
{
    Task<IEnumerable<IDomainEvent>> GetDomainEventsAsync();
}
