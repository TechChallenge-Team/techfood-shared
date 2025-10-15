using System;
using System.Collections.Generic;
using System.Linq;
using TechFood.Shared.Domain.Interfaces;

namespace TechFood.Shared.Domain.Entities;

public class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public bool IsDeleted { get; set; }

    protected readonly List<IDomainEvent> _events = [];

    public List<IDomainEvent> PopEvents()
    {
        var copy = _events.ToList();

        _events.Clear();

        return copy;
    }
}
