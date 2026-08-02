using System;

namespace PlaxionMediator.SourceGenerators;

internal sealed class RequestHandlerModel : IEquatable<RequestHandlerModel>
{
    public RequestHandlerModel(
        string requestFullyQualifiedName,
        string responseFullyQualifiedName,
        string handlerFullyQualifiedName,
        string requestDisplayName,
        string? requestLocationPath,
        int requestLocationLine,
        int requestLocationSpanStart)
    {
        RequestFullyQualifiedName = requestFullyQualifiedName;
        ResponseFullyQualifiedName = responseFullyQualifiedName;
        HandlerFullyQualifiedName = handlerFullyQualifiedName;
        RequestDisplayName = requestDisplayName;
        RequestLocationPath = requestLocationPath;
        RequestLocationLine = requestLocationLine;
        RequestLocationSpanStart = requestLocationSpanStart;
    }

    public string RequestFullyQualifiedName { get; }
    public string ResponseFullyQualifiedName { get; }
    public string HandlerFullyQualifiedName { get; }
    public string RequestDisplayName { get; }
    public string? RequestLocationPath { get; }
    public int RequestLocationLine { get; }
    public int RequestLocationSpanStart { get; }

    public bool Equals(RequestHandlerModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName;
    }

    public override bool Equals(object? obj) => Equals(obj as RequestHandlerModel);

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

internal sealed class NotificationHandlerModel : IEquatable<NotificationHandlerModel>
{
    public NotificationHandlerModel(
        string notificationFullyQualifiedName,
        string handlerFullyQualifiedName,
        string publishStrategy)
    {
        NotificationFullyQualifiedName = notificationFullyQualifiedName;
        HandlerFullyQualifiedName = handlerFullyQualifiedName;
        PublishStrategy = publishStrategy;
    }

    public string NotificationFullyQualifiedName { get; }
    public string HandlerFullyQualifiedName { get; }

    /// <summary>
    /// "Sequential" or "Parallel" — matches <c>PublishStrategy</c> enum names.
    /// </summary>
    public string PublishStrategy { get; }

    public bool Equals(NotificationHandlerModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return NotificationFullyQualifiedName == other.NotificationFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName
               && PublishStrategy == other.PublishStrategy;
    }

    public override bool Equals(object? obj) => Equals(obj as NotificationHandlerModel);

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

internal sealed class StreamRequestHandlerModel : IEquatable<StreamRequestHandlerModel>
{
    public StreamRequestHandlerModel(
        string requestFullyQualifiedName,
        string responseFullyQualifiedName,
        string handlerFullyQualifiedName,
        string requestDisplayName)
    {
        RequestFullyQualifiedName = requestFullyQualifiedName;
        ResponseFullyQualifiedName = responseFullyQualifiedName;
        HandlerFullyQualifiedName = handlerFullyQualifiedName;
        RequestDisplayName = requestDisplayName;
    }

    public string RequestFullyQualifiedName { get; }
    public string ResponseFullyQualifiedName { get; }
    public string HandlerFullyQualifiedName { get; }
    public string RequestDisplayName { get; }

    public bool Equals(StreamRequestHandlerModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName
               && HandlerFullyQualifiedName == other.HandlerFullyQualifiedName;
    }

    public override bool Equals(object? obj) => Equals(obj as StreamRequestHandlerModel);

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

internal sealed class RequestModel : IEquatable<RequestModel>
{
    public RequestModel(
        string requestFullyQualifiedName,
        string responseFullyQualifiedName,
        string requestDisplayName,
        string? locationPath,
        int locationLine,
        int locationSpanStart)
    {
        RequestFullyQualifiedName = requestFullyQualifiedName;
        ResponseFullyQualifiedName = responseFullyQualifiedName;
        RequestDisplayName = requestDisplayName;
        LocationPath = locationPath;
        LocationLine = locationLine;
        LocationSpanStart = locationSpanStart;
    }

    public string RequestFullyQualifiedName { get; }
    public string ResponseFullyQualifiedName { get; }
    public string RequestDisplayName { get; }
    public string? LocationPath { get; }
    public int LocationLine { get; }
    public int LocationSpanStart { get; }

    public bool Equals(RequestModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return RequestFullyQualifiedName == other.RequestFullyQualifiedName
               && ResponseFullyQualifiedName == other.ResponseFullyQualifiedName;
    }

    public override bool Equals(object? obj) => Equals(obj as RequestModel);

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

internal sealed class GenerationModel : IEquatable<GenerationModel>
{
    public GenerationModel(
        EquatableArray<RequestHandlerModel> requestHandlers,
        EquatableArray<NotificationHandlerModel> notificationHandlers,
        EquatableArray<StreamRequestHandlerModel> streamRequestHandlers,
        EquatableArray<RequestModel> requests,
        string rootNamespace)
    {
        RequestHandlers = requestHandlers;
        NotificationHandlers = notificationHandlers;
        StreamRequestHandlers = streamRequestHandlers;
        Requests = requests;
        RootNamespace = rootNamespace;
    }

    public EquatableArray<RequestHandlerModel> RequestHandlers { get; }
    public EquatableArray<NotificationHandlerModel> NotificationHandlers { get; }
    public EquatableArray<StreamRequestHandlerModel> StreamRequestHandlers { get; }
    public EquatableArray<RequestModel> Requests { get; }
    public string RootNamespace { get; }

    public bool Equals(GenerationModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return RootNamespace == other.RootNamespace
               && RequestHandlers.Equals(other.RequestHandlers)
               && NotificationHandlers.Equals(other.NotificationHandlers)
               && StreamRequestHandlers.Equals(other.StreamRequestHandlers)
               && Requests.Equals(other.Requests);
    }

    public override bool Equals(object? obj) => Equals(obj as GenerationModel);

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
            return hash;
        }
    }
}
