using System.Collections.Immutable;
using System.Reflection;
using Conduit.Abstractions;
using Conduit.Core;
using Conduit.DependencyInjection;
using Conduit.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.SourceGenerators.Tests;

internal static class GeneratorTestHelper
{
    public static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult) Run(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISender).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PipelineComposer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ConduitOptions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
        ];

        // Add common BCL references from the runtime directory
        string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is not null)
        {
            List<PortableExecutableReference> all = [.. references];
            foreach (string path in tpa.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name is "System.Collections" or "System.Linq" or "System.Threading" or "System.Threading.Tasks"
                    or "System.Runtime" or "System.Private.CoreLib" or "System.ComponentModel"
                    or "Microsoft.Extensions.DependencyInjection.Abstractions")
                {
                    if (all.All(r => r.Display != path))
                    {
                        all.Add(MetadataReference.CreateFromFile(path));
                    }
                }
            }

            references = all.ToArray();
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ConduitGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return (outputCompilation, diagnostics.AddRange(runResult.Diagnostics), runResult);
    }
}
