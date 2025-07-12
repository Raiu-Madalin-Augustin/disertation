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
        var basePath = Path.Combine(AppContext.BaseDirectory.Split("bin")[0], "MiniShop.RegKit", "Data");

        var testsYaml = File.ReadAllText(Path.Combine(basePath, "tests.yaml"));
        var schemaDiffYaml = File.ReadAllText(Path.Combine(basePath, "schema_diff.yaml"));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

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

        var impactPath = Path.Combine(basePath, "impact.yaml");
        File.WriteAllText(impactPath, serializer.Serialize(output), Encoding.UTF8);
        Console.WriteLine("✔ impact.yaml generat cu succes.");

        ExperimentRunner.RunRegressionComparison(basePath);
    }
}
