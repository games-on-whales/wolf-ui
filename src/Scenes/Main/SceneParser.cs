using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WolfUI.Scenes.Main;

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
            var match = UniqueNodesReg().Matches(first);
            //GD.Print(match);
            if (match.Count > 0)
            {
                string name = match[0].Groups[1].Value;
                string type = match[0].Groups[2].Value == "" ? "Node" : match[0].Groups[2].Value;
                //GD.Print(name, " : ", type);
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

            var uidmatches = UidExtraction().Matches(line);
            var block = BlockStart().Matches(line);
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

    [GeneratedRegex(@"\[gd_scene .*uid=""(uid://.*?)""]")]
    private static partial Regex UidExtraction();

    [GeneratedRegex(@"\[.+\]")]
    private static partial Regex BlockStart();
    
    [GeneratedRegex(@"\[node name=""(.*?)""(?:.*?type=""(.+?)"".*?|.*?)\]")]
    private static partial Regex UniqueNodesReg();
}
