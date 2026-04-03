using MediatR;

namespace Application.Shared;

/// <summary>
/// Represents a single action or operation that changes the state of the application.
/// You can find the article at: https://code-maze.com/cqrs-mediatr-fluentvalidation/
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
