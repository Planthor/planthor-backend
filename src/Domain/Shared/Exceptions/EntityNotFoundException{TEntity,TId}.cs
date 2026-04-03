using System;

namespace Domain.Shared.Exceptions;

/// <summary>
/// Exception thrown when a requested entity cannot be found in the data store.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type that was not found. Used to produce a meaningful message.
/// </typeparam>
/// <typeparam name="TId">
/// The type of the entity's identifier. Matches <see cref="IEntity{TId}.Id"/>.
/// </typeparam>
public class EntityNotFoundException<TEntity, TId> : Exception
    where TEntity : IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets the identifier of the entity that was not found,
    /// or <c>null</c> if no identifier was provided.
    /// </summary>
    public TId? EntityId { get; }

    /// <summary>
    /// Gets the name of the entity type that was not found.
    /// </summary>
    public string EntityType { get; } = typeof(TEntity).Name;

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException{TEntity, TId}"/>
    /// without a specific identifier.
    /// </summary>
    public EntityNotFoundException()
        : base($"{typeof(TEntity).Name} was not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException{TEntity, TId}"/>
    /// with the identifier of the entity that could not be located.
    /// </summary>
    /// <param name="id">
    /// The identifier that was searched for. Included in the exception message
    /// to aid debugging and logging.
    /// </param>
    public EntityNotFoundException(TId id)
        : base($"{typeof(TEntity).Name} with ID '{id}' was not found.")
    {
        EntityId = id;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException{TEntity, TId}"/>
    /// with a custom message.
    /// </summary>
    /// <param name="message">A custom message describing the not-found condition.</param>
    /// <param name="id">The identifier that was searched for.</param>
    public EntityNotFoundException(string message, TId id)
        : base(message)
    {
        EntityId = id;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException{TEntity, TId}"/>
    /// wrapping an inner exception.
    /// </summary>
    /// <param name="id">The identifier that was searched for.</param>
    /// <param name="innerException">The underlying exception that caused this one.</param>
    public EntityNotFoundException(TId id, Exception innerException)
        : base($"{typeof(TEntity).Name} with ID '{id}' was not found.", innerException)
    {
        EntityId = id;
    }
}
