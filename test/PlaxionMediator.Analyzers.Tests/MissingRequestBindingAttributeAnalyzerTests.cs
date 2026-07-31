using PlaxionMediator.Analyzers;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class MissingRequestBindingAttributeAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Get_Request_Has_No_Bindable_Members()
    {
        const string source = """
            using PlaxionMediator.Abstractions;

            public sealed record EmptyGetRequest : IRequest<int>;

            public static class Endpoints
            {
                public static void Map(object endpoints)
                {
                    MapPlaxionMediatorGet<EmptyGetRequest, int>(endpoints, "/empty");
                }

                public static object MapPlaxionMediatorGet<TRequest, TResponse>(object endpoints, string pattern)
                    where TRequest : IRequest<TResponse>
                    => endpoints;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new MissingRequestBindingAttributeAnalyzer(),
            source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator005");
    }

    [Fact]
    public async Task Reports_When_Delete_Request_Has_No_Bindable_Members()
    {
        const string source = """
            using PlaxionMediator.Abstractions;

            public sealed class EmptyDeleteRequest : IRequest<int>
            {
            }

            public static class Endpoints
            {
                public static void Map(object endpoints)
                {
                    MapPlaxionMediatorDelete<EmptyDeleteRequest, int>(endpoints, "/empty");
                }

                public static object MapPlaxionMediatorDelete<TRequest, TResponse>(object endpoints, string pattern)
                    where TRequest : IRequest<TResponse>
                    => endpoints;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new MissingRequestBindingAttributeAnalyzer(),
            source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator005");
    }

    [Fact]
    public async Task No_Diagnostic_When_Request_Has_Primary_Constructor_Parameter()
    {
        const string source = """
            using System;
            using PlaxionMediator.Abstractions;

            public sealed record GetByIdRequest(Guid Id) : IRequest<int>;

            public static class Endpoints
            {
                public static void Map(object endpoints)
                {
                    MapPlaxionMediatorGet<GetByIdRequest, int>(endpoints, "/items/{id}");
                }

                public static object MapPlaxionMediatorGet<TRequest, TResponse>(object endpoints, string pattern)
                    where TRequest : IRequest<TResponse>
                    => endpoints;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new MissingRequestBindingAttributeAnalyzer(),
            source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator005");
    }

    [Fact]
    public async Task No_Diagnostic_For_Post_Even_If_Request_Has_No_Members()
    {
        const string source = """
            using PlaxionMediator.Abstractions;

            public sealed record EmptyPostRequest : IRequest<int>;

            public static class Endpoints
            {
                public static void Map(object endpoints)
                {
                    MapPlaxionMediatorPost<EmptyPostRequest, int>(endpoints, "/empty");
                }

                public static object MapPlaxionMediatorPost<TRequest, TResponse>(object endpoints, string pattern)
                    where TRequest : IRequest<TResponse>
                    => endpoints;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new MissingRequestBindingAttributeAnalyzer(),
            source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator005");
    }
}
