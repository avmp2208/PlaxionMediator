using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;

namespace PlaxionMediator.MinimalApis;

/// <summary>
/// Minimal API endpoint mapping helpers that dispatch requests through <see cref="ISender"/>.
/// </summary>
public static class PlaxionMediatorEndpointRouteBuilderExtensions
{
    private const string MinimalApiAotMessage =
        "This API registers a Minimal API route handler delegate. ASP.NET Core may require unreferenced/dynamic code for request delegate generation and JSON binding of TRequest/TResponse. Prefer known request/response types and enable ASP.NET Core JSON/AOT configuration in the host application when publishing Native AOT.";

    /// <summary>
    /// Maps an HTTP POST endpoint that deserializes <typeparamref name="TRequest"/> from the JSON body,
    /// dispatches it via <see cref="ISender"/>, and returns <c>200 OK</c> with the handler response.
    /// </summary>
    /// <typeparam name="TRequest">Request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    /// <remarks>
    /// Body binding uses ASP.NET Core Minimal API's built-in complex-type body binding
    /// (equivalent to an implicit <c>[FromBody]</c> for a complex reference-type parameter).
    /// PlaxionMediator does not perform reflection-based model binding.
    /// </remarks>
    [RequiresUnreferencedCode(MinimalApiAotMessage)]
    [RequiresDynamicCode(MinimalApiAotMessage)]
    public static RouteHandlerBuilder MapPlaxionMediatorPost<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        return endpoints.MapPost(pattern, async Task<IResult> (TRequest request, ISender sender, CancellationToken ct) =>
        {
            TResponse response = await sender.Send(request, ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        });
    }

    /// <summary>
    /// Maps an HTTP PUT endpoint that deserializes <typeparamref name="TRequest"/> from the JSON body,
    /// dispatches it via <see cref="ISender"/>, and returns <c>200 OK</c> with the handler response.
    /// </summary>
    /// <typeparam name="TRequest">Request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    /// <remarks>
    /// Body binding uses ASP.NET Core Minimal API's built-in complex-type body binding
    /// (equivalent to an implicit <c>[FromBody]</c> for a complex reference-type parameter).
    /// PlaxionMediator does not perform reflection-based model binding.
    /// </remarks>
    [RequiresUnreferencedCode(MinimalApiAotMessage)]
    [RequiresDynamicCode(MinimalApiAotMessage)]
    public static RouteHandlerBuilder MapPlaxionMediatorPut<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        return endpoints.MapPut(pattern, async Task<IResult> (TRequest request, ISender sender, CancellationToken ct) =>
        {
            TResponse response = await sender.Send(request, ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        });
    }

    /// <summary>
    /// Maps an HTTP PATCH endpoint that deserializes <typeparamref name="TRequest"/> from the JSON body,
    /// dispatches it via <see cref="ISender"/>, and returns <c>200 OK</c> with the handler response.
    /// </summary>
    /// <typeparam name="TRequest">Request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    /// <remarks>
    /// Body binding uses ASP.NET Core Minimal API's built-in complex-type body binding
    /// (equivalent to an implicit <c>[FromBody]</c> for a complex reference-type parameter).
    /// PlaxionMediator does not perform reflection-based model binding. Note that <typeparamref name="TRequest"/>
    /// carries the full desired shape of the resource (like <c>PUT</c>); PlaxionMediator does not implement
    /// JSON Merge Patch/JSON Patch semantics — partial-field semantics are the request/handler's responsibility.
    /// </remarks>
    [RequiresUnreferencedCode(MinimalApiAotMessage)]
    [RequiresDynamicCode(MinimalApiAotMessage)]
    public static RouteHandlerBuilder MapPlaxionMediatorPatch<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        return endpoints.MapPatch(pattern, async Task<IResult> (TRequest request, ISender sender, CancellationToken ct) =>
        {
            TResponse response = await sender.Send(request, ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        });
    }

    /// <summary>
    /// Maps an HTTP GET endpoint that binds <typeparamref name="TRequest"/> from route values and query string,
    /// dispatches it via <see cref="ISender"/>, and returns <c>200 OK</c> with the handler response.
    /// </summary>
    /// <typeparam name="TRequest">Request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    /// <remarks>
    /// Binding is performed entirely by ASP.NET Core's built-in complex-object route/query binder
    /// via <c>[AsParameters]</c>. Prefer a <c>sealed record</c> whose primary-constructor parameter names
    /// match route template parameters and/or query string keys. PlaxionMediator does not perform
    /// reflection-based binding.
    /// </remarks>
    [RequiresUnreferencedCode(MinimalApiAotMessage)]
    [RequiresDynamicCode(MinimalApiAotMessage)]
    public static RouteHandlerBuilder MapPlaxionMediatorGet<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        // [AsParameters] instructs ASP.NET Core to bind public ctor parameters/properties from route/query
        // (framework binding — not reflection performed by PlaxionMediator).
        return endpoints.MapGet(pattern, async Task<IResult> ([AsParameters] TRequest request, ISender sender, CancellationToken ct) =>
        {
            TResponse response = await sender.Send(request, ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        });
    }

    /// <summary>
    /// Maps an HTTP DELETE endpoint that binds <typeparamref name="TRequest"/> from route values and query string,
    /// dispatches it via <see cref="ISender"/>, and returns <c>200 OK</c> with the handler response.
    /// </summary>
    /// <typeparam name="TRequest">Request type implementing <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">Response type produced by the handler.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> for further endpoint configuration.</returns>
    /// <remarks>
    /// Binding is performed entirely by ASP.NET Core's built-in complex-object route/query binder
    /// via <c>[AsParameters]</c>. Prefer a <c>sealed record</c> whose primary-constructor parameter names
    /// match route template parameters and/or query string keys. PlaxionMediator does not perform
    /// reflection-based binding.
    /// </remarks>
    [RequiresUnreferencedCode(MinimalApiAotMessage)]
    [RequiresDynamicCode(MinimalApiAotMessage)]
    public static RouteHandlerBuilder MapPlaxionMediatorDelete<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        // [AsParameters] instructs ASP.NET Core to bind public ctor parameters/properties from route/query
        // (framework binding — not reflection performed by PlaxionMediator).
        return endpoints.MapDelete(pattern, async Task<IResult> ([AsParameters] TRequest request, ISender sender, CancellationToken ct) =>
        {
            TResponse response = await sender.Send(request, ct).ConfigureAwait(false);
            return TypedResults.Ok(response);
        });
    }
}
