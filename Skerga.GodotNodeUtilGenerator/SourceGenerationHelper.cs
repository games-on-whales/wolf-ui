using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

/*
using NodeName = string;
using TscnId = string;
using TypeName = string;
using SourceFileName = string;

public class Tscn
{
    public TscnId Uid { get; set; } = "";
    public readonly List<(NodeName, TypeName)> ResolvedNodes = [];
    public List<(NodeName, TscnId)> UnresolvedNodes = []; 
    public SourceFileName SourceFileName { get; set; }
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public bool SkipCreate = false;
    
    private string NodeTemplate = """
                                    private {1} _{0};
                                    public {1}{0}
                                    {
                                        get
                                        {
                                            _{0} ??= GetNode<{1}>("%{0}");
                                            return _{0};
                                        }
                                    }
                            """;

    public string GetSource()
    {
        var builder = new StringBuilder();
        builder.Append($$"""
                          using Godot;

                          namespace {{Namespace}}
                          {
                              public partial class {{ClassName}}
                              {
                              
                          """);
        
        foreach (var tuple in ResolvedNodes)
        {
            builder.Append(string.Format(NodeTemplate, tuple.Item1, tuple.Item2));
        }

        if (!SkipCreate)
        {
            builder.Append($$"""
                                     private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("{{Uid}}");
                                     public static {{ClassName}} Create()
                                     {
                                         {{ClassName}} obj = SelfRef.Instantiate<{{ClassName}}>();
                                         return obj;
                                     }
                             """);
        }
        
        builder.Append("""
                           }
                       }
                       """);
        
        return builder.ToString();
    }
}
*/
public partial class SceneFile
{
    private enum State { Block, Values }
    public string Uid = "";

    public readonly List<(string, string)> UniqueNodes = [];

    private void ParseBlock(string block)
    {
        var hasKwd = block.Split('\n').Any(l => l == "unique_name_in_owner = true");

        if (!hasKwd) return;
        
        var first = block.Split('\n').First();
        var match = Regex.Matches(first, """\[node name="(.*?)"(?:.*?type="(.+?)".*?|.*?)\]""");

        if (match.Count <= 0) return;
        var name = match[0].Groups[1].Value;
        var type = match[0].Groups[2].Value == "" ? "Node" : match[0].Groups[2].Value;
        UniqueNodes.Add((name, type));
    }

    public SceneFile(string content)
    {
        var lines = content.Split('\n');
        var builder = new StringBuilder();
        var state = State.Block;
        var lastState = State.Block;
        foreach (var line in lines)
        {
            if (line == "\n")
                continue;

            var uidMatches = Regex.Matches(line, """\[gd_scene .*uid="(uid://.*?)"]""");
            var block = Regex.Matches(line, @"\[.+\]");
            if (uidMatches.Count > 0)
            {
                Uid = uidMatches[0].Groups[1].Value;
            }

            if (state == State.Values || lastState == state)
            {
                //if (builder.Length > 0) builder.Length--;
                ParseBlock(builder.ToString());
                builder.Clear();
            }

            builder.AppendLine(line);

            lastState = state;
            state = block.Count > 0 ? State.Block : State.Values;
        }
    }
}

public class SourceGenerationHelper
{
    public const string Attribute = """
    namespace Skerga.GodotNodeUtilGenerator
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public class SceneAutoConfigureAttribute : System.Attribute
        {

            public bool GenerateNewMethod
            {
                get;
                set;
            } = true;
        }
    }
    """;

    public static readonly List<(string, string)> TscnFiles = [];


    public static string ParseSceneFileOfScriptFile((string, string) tscnFiles, INamedTypeSymbol symbol)
    {
        var attributes = symbol.GetAttributes();
        var skipNewGeneration = attributes
            .Where(a => a.AttributeClass != null && a.AttributeClass.ToDisplayString() ==
                "Skerga.GodotNodeUtilGenerator.SceneAutoConfigureAttribute")
            .Select(a => a.NamedArguments.IsEmpty ? default : a.NamedArguments.First(kv => kv.Key == "GenerateNewMethod"))
            .Where(kv => kv.Key is not null)
            .Any(kv => kv.Value.Value is bool);
        
        SceneFile scene = new(tscnFiles.Item2);

        var builder = new StringBuilder();
        foreach (var elem in scene.UniqueNodes)
        {
            builder.AppendLine($$"""
                    private {{elem.Item2}} _{{elem.Item1}};
                    public {{elem.Item2}} {{elem.Item1}}
                    {
                        get
                        {
                            _{{elem.Item1}} ??= GetNode<{{elem.Item2}}>("%{{elem.Item1}}");
                            return _{{elem.Item1}};
                        }
                    }
            """);
        }

        var newMethodSourceCode = $$"""
                private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("{{scene.Uid}}");
                public static {{symbol.Name}} Create()
                {
                    {{symbol.Name}} obj = SelfRef.Instantiate<{{symbol.Name}}>();
                    return obj;
                }
        """;

        var namespaceText = symbol.ToDisplayString().Length > (symbol.Name.Length + 1)
            ? $"namespace {symbol.ToDisplayString().Substring(0, symbol.ToDisplayString().Length - symbol.Name.Length - 1)}"
            : "";
        
        return $$"""
        using Godot;
        
        {{namespaceText}}
        {
            public partial class {{symbol.Name}}
            {
        {{builder}}

        {{(skipNewGeneration ? "" : newMethodSourceCode)}}

                public static void Yell()
                {
                    GD.Print("Injected");
                }
            }
        }
        """;
    }
}
