using System.Collections.Immutable;
using System.Reflection;
using PlaxionMediator.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        ];

        string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is not null)
        {
            foreach (string path in tpa.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name is "System.Collections" or "System.Linq" or "System.Threading" or "System.Threading.Tasks"
                    or "System.Runtime" or "System.Private.CoreLib" or "netstandard")
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
