using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IdentityBraces.Core;

// Dumps what the Visual Studio extension's Core makes of a sample file, so the TypeScript
// port can be diffed against it brace for brace.
//
// This is the generator behind test/fixtures/csharp-core.json, which parity.test.ts asserts
// against. It links the C# sources directly rather than copying them, so the fixture cannot
// be regenerated from a stale copy of the thing it is meant to be checking:
//
//     cd vscode/tools/parity && dotnet run -c Release
//
internal static class Program
{
    private const string DefaultInput = "../../test/fixtures/sample.txt";
    private const string DefaultOutput = "../../test/fixtures/csharp-core.json";

    private static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : DefaultInput;
        string text = File.ReadAllText(path);

        var sb = new StringBuilder();
        sb.Append("[\n");

        EmitScan(sb, text, Settings(true, false, 0), "default");
        sb.Append(",\n");
        EmitScan(sb, text, Settings(false, false, 0), "shared");
        sb.Append(",\n");
        EmitScan(sb, text, Settings(true, true, 0), "depth");
        sb.Append(",\n");
        EmitScan(sb, text, Settings(true, false, 3), "warn3");
        sb.Append(",\n");
        EmitScan(sb, text, Unusable(), "unusable");
        sb.Append("\n]\n");

        string output = args.Length > 1 ? args[1] : DefaultOutput;
        File.WriteAllText(output, sb.ToString());
        Console.WriteLine("wrote " + output);
    }

    private static ScanSettings Settings(bool independent, bool depth, int warn)
    {
        ScanSettings s = ScanSettings.Default;
        s.IndependentBraces = independent;
        s.ColorByDepth = depth;
        s.ComplexityWarningDepth = warn;
        return s;
    }

    private static ScanSettings Unusable()
    {
        ScanSettings s = ScanSettings.Default;
        TraitPreset preset = TraitPresets.Find("unusable");

        var weights = new List<TraitWeight>();
        for (int i = 0; i < TraitCatalog.All.Count; i++)
        {
            TraitInfo info = TraitCatalog.All[i];
            int percent;
            weights.Add(new TraitWeight
            {
                Id = info.Id,
                Layer = info.Layer,
                Percent = preset.Weights.TryGetValue(info.Id, out percent) ? percent : 0,
            });
        }

        s.TraitWeights = weights;
        return s;
    }

    private static void EmitScan(StringBuilder sb, string text, ScanSettings settings, string label)
    {
        BraceInfo[] braces = BraceScanner.Scan(text, settings);

        sb.Append("{\"label\":\"").Append(label).Append("\",\"braces\":[");

        for (int i = 0; i < braces.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            BraceInfo b = braces[i];
            sb.Append("{\"p\":").Append(b.Position);
            sb.Append(",\"c\":").Append(Quote(b.Character.ToString()));
            sb.Append(",\"k\":").Append((int)b.Kind);
            sb.Append(",\"o\":").Append(b.IsOpen ? 1 : 0);
            sb.Append(",\"m\":").Append(b.IsMatched ? 1 : 0);
            sb.Append(",\"d\":").Append(b.Depth);
            sb.Append(",\"pi\":").Append(b.PartnerIndex);
            sb.Append(",\"pa\":").Append(b.ParentIndex);
            sb.Append(",\"id\":\"").Append(b.Identity.ToString("x16")).Append('"');
            sb.Append(",\"ci\":").Append(b.ColorIndex);
            sb.Append(",\"body\":").Append(Quote(b.Traits.Body));
            sb.Append(",\"cr\":").Append(Quote(b.Traits.Creature));
            sb.Append(",\"co\":").Append(Quote(b.Traits.Costume));
            sb.Append(",\"mo\":").Append(Quote(b.Traits.Motion));
            sb.Append(",\"ef\":[");
            for (int e = 0; e < b.Traits.Effects.Length; e++)
            {
                if (e > 0)
                {
                    sb.Append(',');
                }

                sb.Append(Quote(b.Traits.Effects[e]));
            }

            sb.Append("]");
            sb.Append(",\"name\":").Append(Quote(BraceNames.Of(b.Identity)));
            sb.Append('}');
        }

        sb.Append("]}");
    }

    private static string Quote(string value)
    {
        if (value == null)
        {
            return "null";
        }

        var sb = new StringBuilder("\"");
        foreach (char c in value)
        {
            if (c == '"' || c == '\\')
            {
                sb.Append('\\').Append(c);
            }
            else if (c < 0x20)
            {
                sb.Append("\\u").Append(((int)c).ToString("x4"));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.Append('"').ToString();
    }
}
