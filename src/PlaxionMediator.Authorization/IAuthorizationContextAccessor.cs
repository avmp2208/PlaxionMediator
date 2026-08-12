namespace PlaxionMediator.Authorization;

/// <summary>
/// Resolves the <see cref="IAuthorizationContext"/> for the current logical operation.
/// Implementations are typically scoped: one instance per <c>ISender.Send</c> call / DI scope.
/// </summary>
public interface IAuthorizationContextAccessor
{
    /// <summary>The authorization context for the current logical operation.</summary>
    IAuthorizationContext Current { get; }
}
