using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Skerga.GodotNodeUtilGenerator;

[Generator]
public class SceneNodeCtorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "SceneAutoConfigureAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)
        ));

        var files = context.AdditionalTextsProvider
            .Where(m => m is not null)
            //.Where(a => a.Path.EndsWith(".tres"))
            .Select((a, c) => (a.Path, a.GetText(c)!.ToString()));

        var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());

        var provider = context.SyntaxProvider.ForAttributeWithMetadataName("Skerga.GodotNodeUtilGenerator.SceneAutoConfigureAttribute",
            predicate: (c, _) => c is ClassDeclarationSyntax,
            transform: (n, _) => n.TargetNode)
        .Where(m => m is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilationAndFiles,
            (productionContext, sourceContext) => Generate(productionContext, sourceContext.Left, sourceContext.Right));

        context.RegisterSourceOutput(compilation,
            (spc, source) => Execute(spc, source.Left, source.Right));

    }

    void Generate(SourceProductionContext context, Compilation compilation, ImmutableArray<(string, string)> files)
    {
        if (files.Count() <= 0)
            return;

        foreach (var n in files)
        {
            SourceGenerationHelper.TscnFiles.Add(n);
        }

    }
    
    private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<SyntaxNode> typeList)
    {
        //if (!Debugger.IsAttached) Debugger.Launch();
        var builder = new StringBuilder();

        foreach (var syntax in typeList)
        {
            var symbol = compilation
                .GetSemanticModel(syntax.SyntaxTree)
                .GetDeclaredSymbol(syntax) as INamedTypeSymbol;

            if (symbol is not null)
            {
                var tscn = SourceGenerationHelper.TscnFiles
                    .Where(e => Path.GetFileNameWithoutExtension(e.Item1) == Path.GetFileNameWithoutExtension(syntax.SyntaxTree.FilePath))
                    .First();


                var sourceCode = SourceGenerationHelper.ParseSceneFileOfScriptFile(tscn, symbol);

                context.AddSource($"{symbol.Name}Extension.g.cs", sourceCode);

                builder.AppendLine();
                builder.Append($"\"\"\"");
                builder.AppendLine();
                builder.Append($"{symbol.Name}Extension.g.cs ->");
                builder.AppendLine();
                builder.Append($"{sourceCode}");
                builder.AppendLine();
                builder.Append($"\"\"\",");
                //SourceGenerationHelper.ParseSceneFileOfScriptFile(syntax.SyntaxTree.FilePath);
            }
        }

        if (builder.Length > 0) builder.Length--;

        var code = $$"""
            using System.Collections.Generic;

            namespace Skerga.GodotNodeUtilGenerator
            {
                public static class MyGeneratedClass
                {
                    public static List<string> Names = [{{builder.ToString()}}];
                }
            }
            """;

        context.AddSource("MyGeneratedClass.g.cs", code);
    }
}