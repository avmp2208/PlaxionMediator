using System.Collections.Immutable;
using System.Reflection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Authorization;
using PlaxionMediator.Core;
using PlaxionMediator;
using PlaxionMediator.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator.SourceGenerators.Tests;

internal static class GeneratorTestHelper
{
    public static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult) Run(string source)
    {
        return Run(source, includeAuthorization: false);
    }

    public static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult RunResult) Run(
        string source,
        bool includeAuthorization)
    {
        (GeneratorDriver driver, CSharpCompilation compilation) = CreateDriver(source, includeAuthorization);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return (outputCompilation, diagnostics.AddRange(runResult.Diagnostics), runResult);
    }

    public static (GeneratorDriver Driver, CSharpCompilation Compilation) CreateDriver(string source)
    {
        return CreateDriver(source, includeAuthorization: false);
    }

    public static (GeneratorDriver Driver, CSharpCompilation Compilation) CreateDriver(string source, bool includeAuthorization)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        PortableExecutableReference[] references = CreateReferences(includeAuthorization);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        PlaxionMediatorGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        return (driver, compilation);
    }

    private static PortableExecutableReference[] CreateReferences(bool includeAuthorization)
    {
        List<PortableExecutableReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IRequest<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISender).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PipelineComposer).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PlaxionMediatorOptions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
        ];

        if (includeAuthorization)
        {
            references.Add(MetadataReference.CreateFromFile(typeof(IRequestAuthorization<>).Assembly.Location));
        }

        // Add common BCL references from the runtime directory
        string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is not null)
        {
            foreach (string path in tpa.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name is "System.Collections" or "System.Linq" or "System.Threading" or "System.Threading.Tasks"
                    or "System.Runtime" or "System.Private.CoreLib" or "System.ComponentModel"
                    or "Microsoft.Extensions.DependencyInjection.Abstractions")
                {
                    if (references.All(r => r.Display != path))
                    {
                        references.Add(MetadataReference.CreateFromFile(path));
                    }
                }
            }
        }

        return references.ToArray();
    }
}
