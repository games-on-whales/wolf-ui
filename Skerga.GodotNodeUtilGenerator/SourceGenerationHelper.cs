using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

public partial class SceneFile
{
    enum State { Block, Values }
    public string uid = "";

    public List<(string, string)> UniqueNodes = [];

    private void ParseBlock(string block)
    {
        bool hasKwd = block.Split('\n').Where(l => l == "unique_name_in_owner = true").Any();

        if (hasKwd)
        {
            var first = block.Split('\n').First();
            var match = Regex.Matches(first, @"\[node name=""(.*?)""(?:.*?type=""(.+?)"".*?|.*?)\]");

            if (match.Count > 0)
            {
                string name = match[0].Groups[1].Value;
                string type = match[0].Groups[2].Value == "" ? "Node" : match[0].Groups[2].Value;
                UniqueNodes.Add((name, type));
            }
        }
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

            var uidmatches = Regex.Matches(line, @"\[gd_scene .*uid=""(uid://.*?)""]");
            var block = Regex.Matches(line, @"\[.+\]");
            if (uidmatches.Count > 0)
            {
                uid = uidmatches[0].Groups[1].Value;
            }

            if (state == State.Values || (state == State.Block && lastState == state))
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
    public const string Attribute = @"
namespace Skerga.GodotNodeUtilGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SceneAutoConfigureAttribute : System.Attribute
    {
    }
}";

    public static readonly List<(string, string)> TscnFiles = [];


    public static string ParseSceneFileOfScriptFile((string, string) TscnFiles, INamedTypeSymbol symbol)
    {
        SceneFile scene = new(TscnFiles.Item2);

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



        return $$"""
        using Godot;

        namespace {{symbol.ToDisplayString().Substring(0, symbol.ToDisplayString().Length - symbol.Name.Length - 1)}}
        {
            public partial class {{symbol.Name}}
            {
        {{builder.ToString()}}

                private static readonly PackedScene SelfRef = ResourceLoader.Load<PackedScene>("{{scene.uid}}");
                public static {{symbol.Name}} New()
                {
                    {{symbol.Name}} obj = SelfRef.Instantiate<{{symbol.Name}}>();
                    return obj;
                }

                public static void Yell()
                {
                    GD.Print("Injected");
                }
            }
        }
        """;
    }
}
