using MediatR;

namespace Application.Shared;

/// <summary>
/// Represents a handler for a <see cref="TCommand"/> command without response.
/// https://code-maze.com/cqrs-mediatr-fluentvalidation/
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;
