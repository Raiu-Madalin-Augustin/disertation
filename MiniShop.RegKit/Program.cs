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
    // suportă formatul vechi și cel nou
    public string? Column { get; set; }           // ex: "stock_status" când change=column_added
    public string? Change { get; set; }           // ex: "column_added" | "column_removed" | "modified"
    public string? ColumnRemoved { get; set; }    // compat vechi
    public string? ColumnAdded { get; set; }      // compat vechi
}

class Program
{
    static void Main()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory.Split("bin")[0], "Data");

        var testsYaml = File.ReadAllText(Path.Combine(basePath, "tests.yaml"));
        var schemaDiffYaml = File.ReadAllText(Path.Combine(basePath, "schema_diff.yaml"));

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var tests = deserializer.Deserialize<TestDefinition>(testsYaml);
        var diffs = deserializer.Deserialize<SchemaDiff>(schemaDiffYaml);

        var impacted = new HashSet<string>();

        var modifiedTables = diffs.Diff
            .Where(d =>
             !string.IsNullOrWhiteSpace(d.Change) ||          // scenariu nou (ex: column_added)
             !string.IsNullOrWhiteSpace(d.ColumnRemoved) ||   // compat vechi
             !string.IsNullOrWhiteSpace(d.ColumnAdded) ||     // compat vechi
             !string.IsNullOrWhiteSpace(d.Column))            // dacă e doar column setat
            .Select(d => d.Table)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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
