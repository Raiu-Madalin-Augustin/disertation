using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using MiniShop.RegKit.Utils;

public class TestDefinition
{
    public Dictionary<string, TestEntry> Tests { get; set; } = new();
}

public class TestEntry
{
    public List<string> Tables { get; set; } = new();
}

public class SchemaDiff
{
    public List<DiffEntry> Diff { get; set; } = new();
}

public class DiffEntry
{
    public string Table { get; set; } = "";
    public string? ColumnRemoved { get; set; }
}

class Program
{
    static void Main()
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var testsYaml = File.ReadAllText("Data/tests.yaml");
        var schemaDiffYaml = File.ReadAllText("Data/schema_diff.yaml");

        var tests = deserializer.Deserialize<TestDefinition>(testsYaml);
        var diffs = deserializer.Deserialize<SchemaDiff>(schemaDiffYaml);

        var impacted = new HashSet<string>();

        var modifiedTables = diffs.Diff
            .Where(d => !string.IsNullOrWhiteSpace(d.ColumnRemoved))
            .Select(d => d.Table)
            .ToHashSet();

        foreach (var test in tests.Tests)
        {
            if (test.Value.Tables.Any(t => modifiedTables.Contains(t)))
            {
                impacted.Add(test.Key);
            }
        }

        var output = new { impacted_tests = impacted.ToList() };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var impactYaml = serializer.Serialize(output);
        File.WriteAllText("Data/impact.yaml", impactYaml, Encoding.UTF8);

        Console.WriteLine("✔ impact.yaml generat cu succes.");

        var faultsPerTest = new Dictionary<string, List<string>>
        {
            { "T4", new List<string> { "F1" } },
            { "T5", new List<string> { "F2" } },
            { "T14", new List<string> { "F1", "F3" } },
            { "T6", new List<string> { "F4" } },
            { "T9", new List<string> { "F3" } }
        };

        var result = APFDCalculator.ComputeFromYaml("Data/impact.yaml", faultsPerTest);

        APFDCalculator.ExportPrioritizationYaml("Data/prioritization.yaml", result.PrioritizedTests, faultsPerTest);
        APFDCalculator.ExportPrioritizationHtml("Data/prioritization.html", result.PrioritizedTests, faultsPerTest);

        ExperimentRunner.RunRegressionComparison();
    }
}
