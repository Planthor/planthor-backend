using System;
using System.Collections.Generic;
using PlanthorWebApi.Domain.Shared;

namespace PlanthorWebApi.Domain;

public class Goal(Guid id) : IAggregateRoot, IEntity<Guid>
{
    public Guid Id { get; protected set; } = id;

    public string Name { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => throw new NotImplementedException();

    public void ClearDomainEvents()
    {
        throw new NotImplementedException();
    }

    public ValidationResult Validate()
    {
        throw new NotSupportedException();
    }
}
