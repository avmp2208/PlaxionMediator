namespace PlaxionMediator.Authorization;

/// <summary>
/// Classifies the origin of the current logical operation for authorization purposes.
/// </summary>
public enum CallerKind
{
    /// <summary>Unknown/unclassified caller.</summary>
    Unknown,

    /// <summary>The operation originated from an HTTP request.</summary>
    Http,

    /// <summary>The operation originated from a background worker or scheduled task.</summary>
    Background,

    /// <summary>The operation originated from internal application code (e.g. another handler, an internal service).</summary>
    Internal,
}
