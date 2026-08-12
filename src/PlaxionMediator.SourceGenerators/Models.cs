using System;

namespace PlaxionMediator.SourceGenerators;

/// <summary>
/// Value-equality model for a discovered request handler.
/// Location fields are intentionally excluded from equality so the incremental generator
/// cache treats semantically identical inputs as unchanged across non-semantic edits
/// (e.g. whitespace / line-number shifts).
/// </summary>
internal readonly record struct RequestHandlerModel(
    string RequestFullyQualifiedName,
    string ResponseFullyQualifiedName,
    string HandlerFullyQualifiedName,
    string RequestDisplayName,
    string? RequestLocationPath,
    int RequestLocationLine,
    int RequestLocationSpanStart) : IEquatable<RequestHandlerModel>
{
    public bool Equals(RequestHandlerModel other)
    {
        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + RequestFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + ResponseFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + HandlerFullyQualifiedName.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Value-equality model for a discovered notification handler.
/// </summary>
internal readonly record struct NotificationHandlerModel(
    string NotificationFullyQualifiedName,
    string HandlerFullyQualifiedName,
    string PublishStrategy) : IEquatable<NotificationHandlerModel>
{
    public bool Equals(NotificationHandlerModel other)
    {
        return NotificationFullyQualifiedName == other.NotificationFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName
               && PublishStrategy == other.PublishStrategy;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + NotificationFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + HandlerFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + PublishStrategy.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Value-equality model for a discovered stream request handler.
/// </summary>
internal readonly record struct StreamRequestHandlerModel(
    string RequestFullyQualifiedName,
    string ResponseFullyQualifiedName,
    string HandlerFullyQualifiedName,
    string RequestDisplayName) : IEquatable<StreamRequestHandlerModel>
{
    public bool Equals(StreamRequestHandlerModel other)
    {
        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + RequestFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + ResponseFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + HandlerFullyQualifiedName.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Value-equality model for a discovered request type.
/// Location fields are intentionally excluded from equality (see <see cref="RequestHandlerModel"/>).
/// </summary>
internal readonly record struct RequestModel(
    string RequestFullyQualifiedName,
    string ResponseFullyQualifiedName,
    string RequestDisplayName,
    string? LocationPath,
    int LocationLine,
    int LocationSpanStart) : IEquatable<RequestModel>
{
    public bool Equals(RequestModel other)
    {
        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + RequestFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + ResponseFullyQualifiedName.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Value-equality model for a discovered <c>IRequestAuthorization&lt;TRequest&gt;</c> implementation.
/// </summary>
internal readonly record struct AuthorizationCheckModel(
    string RequestFullyQualifiedName,
    string CheckFullyQualifiedName) : IEquatable<AuthorizationCheckModel>
{
    public bool Equals(AuthorizationCheckModel other)
    {
        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && CheckFullyQualifiedName == other.CheckFullyQualifiedName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + RequestFullyQualifiedName.GetHashCode();
            hash = (hash * 31) + CheckFullyQualifiedName.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Aggregate generation input. Value equality drives incremental source-output caching.
/// </summary>
internal readonly record struct GenerationModel(
    EquatableArray<RequestHandlerModel> RequestHandlers,
    EquatableArray<NotificationHandlerModel> NotificationHandlers,
    EquatableArray<StreamRequestHandlerModel> StreamRequestHandlers,
    EquatableArray<RequestModel> Requests,
    EquatableArray<AuthorizationCheckModel> AuthorizationChecks,
    string RootNamespace) : IEquatable<GenerationModel>
{
    public bool Equals(GenerationModel other)
    {
        return RootNamespace == other.RootNamespace
               && RequestHandlers.Equals(other.RequestHandlers)
               && NotificationHandlers.Equals(other.NotificationHandlers)
               && StreamRequestHandlers.Equals(other.StreamRequestHandlers)
               && Requests.Equals(other.Requests)
               && AuthorizationChecks.Equals(other.AuthorizationChecks);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + RootNamespace.GetHashCode();
            hash = (hash * 31) + RequestHandlers.GetHashCode();
            hash = (hash * 31) + NotificationHandlers.GetHashCode();
            hash = (hash * 31) + StreamRequestHandlers.GetHashCode();
            hash = (hash * 31) + Requests.GetHashCode();
            hash = (hash * 31) + AuthorizationChecks.GetHashCode();
            return hash;
        }
    }
}
