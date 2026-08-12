using System.Collections.Immutable;
using System.Reflection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Pipeline;
using PlaxionMediator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(DiagnosticAnalyzer analyzer, string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISender).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PipelineBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PlaxionMediator.Transactions.ITransactionalRequest).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PlaxionMediator.Retry.IRetryableRequest).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PlaxionMediatorOptions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        ];

        // Optional: SQLite extension methods for provider-specific analyzer tests.
        try
        {
            references.Add(MetadataReference.CreateFromFile(
                typeof(Microsoft.EntityFrameworkCore.SqliteDbContextOptionsBuilderExtensions).Assembly.Location));
        }
        catch
        {
            // SQLite package may be unavailable in some environments; analyzer 045 tests rely on method name syntax.
        }

        string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is not null)
        {
            foreach (string path in tpa.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name is "System.Collections" or "System.Linq" or "System.Threading" or "System.Threading.Tasks"
                    or "System.Runtime" or "System.Private.CoreLib" or "netstandard"
                    or "System.Collections.Concurrent" or "Microsoft.Extensions.DependencyInjection.Abstractions"
                    or "Microsoft.Extensions.DependencyInjection")
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
