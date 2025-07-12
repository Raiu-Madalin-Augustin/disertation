using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ImpactData
{
    public List<string> Impacted_Tests { get; set; } = new();
}

public class APFDResult
{
    public List<string> PrioritizedTests { get; set; } = new();
    public double APFD { get; set; }
}

public static class APFDCalculator
{
    public static APFDResult ComputeFromYaml(string yamlPath, Dictionary<string, List<string>> faultsPerTest)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var impactYaml = File.ReadAllText(yamlPath);
        var data = deserializer.Deserialize<ImpactData>(impactYaml);
        var impactedTests = data.Impacted_Tests;

        var prioritized = impactedTests
            .OrderByDescending(t => faultsPerTest.ContainsKey(t) ? faultsPerTest[t].Count : 0)
            .ToList();

        // T = număr de teste, F = fault-uri unice
        var allFaults = faultsPerTest.Values.SelectMany(x => x).Distinct().ToList();
        int T = prioritized.Count;
        int F = allFaults.Count;

        double sum = 0;
        foreach (var fault in allFaults)
        {
            for (int i = 0; i < prioritized.Count; i++)
            {
                var test = prioritized[i];
                if (faultsPerTest.TryGetValue(test, out var faults) && faults.Contains(fault))
                {
                    sum += (i + 1); // 1-based index
                    break;
                }
            }
        }

        double apfd = F > 0 ? 1 - (sum / (T * F)) + (1.0 / (2 * T)) : 1.0;

        return new APFDResult
        {
            PrioritizedTests = prioritized,
            APFD = Math.Round(apfd, 4)
        };
    }

    public static void ExportPrioritizationYaml(string outputPath, List<string> orderedTests, Dictionary<string, List<string>> faultsPerTest)
    {
        var prioritized = orderedTests.Select((testId, index) => new
        {
            id = testId,
            position = index + 1,
            faults = faultsPerTest.ContainsKey(testId) ? faultsPerTest[testId] : new List<string>()
        }).ToList();

        var output = new { prioritized_tests = prioritized };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(output);
        File.WriteAllText(outputPath, yaml, Encoding.UTF8);
        Console.WriteLine($"📄 prioritization.yaml scris la: {outputPath}");
    }

    public static void ExportPrioritizationHtml(string outputPath, List<string> orderedTests, Dictionary<string, List<string>> faultsPerTest)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'><head><meta charset='UTF-8'>");
        sb.AppendLine("<title>Prioritization Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 2em; }");
        sb.AppendLine("table { border-collapse: collapse; width: 80%; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h2>Prioritized Regression Tests</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>#</th><th>Test Case</th><th>Detected Faults</th></tr>");

        for (int i = 0; i < orderedTests.Count; i++)
        {
            var testId = orderedTests[i];
            var faults = faultsPerTest.ContainsKey(testId) ? string.Join(", ", faultsPerTest[testId]) : "-";
            sb.AppendLine($"<tr><td>{i + 1}</td><td>{testId}</td><td>{faults}</td></tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"📄 prioritization.html scris la: {outputPath}");
    }

    public static APFDResult ComputeFromYamlFromList(List<string> orderedTests, Dictionary<string, List<string>> faultsPerTest)
    {
        var allFaults = faultsPerTest.Values.SelectMany(x => x).Distinct().ToList();
        int T = orderedTests.Count;
        int F = allFaults.Count;

        double sum = 0;
        foreach (var fault in allFaults)
        {
            for (int i = 0; i < orderedTests.Count; i++)
            {
                var test = orderedTests[i];
                if (faultsPerTest.TryGetValue(test, out var faults) && faults.Contains(fault))
                {
                    sum += (i + 1); 
                    break;
                }
            }
        }

        double apfd = F > 0 ? 1 - (sum / (T * F)) + (1.0 / (2 * T)) : 1.0;

        return new APFDResult
        {
            PrioritizedTests = orderedTests,
            APFD = Math.Round(apfd, 4)
        };
    }
}
