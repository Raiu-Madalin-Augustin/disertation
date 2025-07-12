using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MiniShop.RegKit.Utils;

public static class ExperimentRunner
{
    public static void RunRegressionComparison()
    {
        var baseDataPath = Path.Combine(AppContext.BaseDirectory, "Data");

        var faultsPerTest = new Dictionary<string, List<string>>
        {
            { "T4", new List<string> { "F1" } },
            { "T5", new List<string> { "F1" } },
            { "T14", new List<string> { "F2" } },
            { "T6", new List<string> { "F3" } },
            { "T9", new List<string> { "F3" } },
            { "T15", new List<string> { "F2" } },
            { "T16", new List<string> { "F4" } },
            { "T17", new List<string> { "F2" } },
            { "T20", new List<string> { "F3" } }
        };

        // 🧠 APFD pentru prioritizare automată
        var result = APFDCalculator.ComputeFromYaml(Path.Combine(baseDataPath, "impact.yaml"), faultsPerTest);

        Console.WriteLine("✅ Prioritizare automată:");
        foreach (var test in result.PrioritizedTests)
        {
            Console.WriteLine($" - {test}");
        }
        Console.WriteLine($"📈 APFD (prioritizat): {result.APFD}");

        // 🎲 APFD pentru o permutare random
        var rnd = new Random();
        var shuffled = result.PrioritizedTests.OrderBy(_ => rnd.Next()).ToList();

        var randomResult = APFDCalculator.ComputeFromYamlFromList(shuffled, faultsPerTest);
        Console.WriteLine($"\n🎲 APFD (random): {randomResult.APFD}");

        // 💾 Salvează un raport simplu
        var report = new
        {
            timestamp = DateTime.UtcNow.ToString("u"),
            tests = result.PrioritizedTests,
            apfd_prioritized = result.APFD,
            apfd_random = randomResult.APFD
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        File.WriteAllText(Path.Combine(baseDataPath, "results.yaml"), serializer.Serialize(report), Encoding.UTF8);

        Console.WriteLine("\n📄 Salvat raport în Data/results.yaml");

        ExportComparisonHtml(
            Path.Combine(baseDataPath, "apfd_comparison.html"),
            result.PrioritizedTests,
            shuffled,
            result.APFD,
            randomResult.APFD
        );
        ExportComparisonChartHtml(
            Path.Combine(baseDataPath, "apfd_chart.html"),
            result.APFD,
            randomResult.APFD
        );
    }

    private static void ExportComparisonHtml(string outputPath, List<string> ordered, List<string> randomized, double apfdOrdered, double apfdRandom)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'><head><meta charset='UTF-8'>");
        sb.AppendLine("<title>APFD Comparison Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 2em; }");
        sb.AppendLine("h2 { color: #333; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 2em; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 8px; text-align: center; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h2>Comparatie Prioritizare vs Random</h2>");

        sb.AppendLine("<p><strong>APFD Prioritizat:</strong> " + apfdOrdered + "</p>");
        sb.AppendLine("<p><strong>APFD Random:</strong> " + apfdRandom + "</p>");

        sb.AppendLine("<h3>Ordine Prioritizată</h3>");
        sb.AppendLine("<table><tr><th>#</th><th>Test</th></tr>");
        for (int i = 0; i < ordered.Count; i++)
        {
            sb.AppendLine($"<tr><td>{i + 1}</td><td>{ordered[i]}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<h3>Ordine Aleatorie</h3>");
        sb.AppendLine("<table><tr><th>#</th><th>Test</th></tr>");
        for (int i = 0; i < randomized.Count; i++)
        {
            sb.AppendLine($"<tr><td>{i + 1}</td><td>{randomized[i]}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("</body></html>");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"📄 Export HTML: {outputPath}");
    }

    private static void ExportComparisonChartHtml(string outputPath, double apfdOrdered, double apfdRandom)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <title>APFD Chart</title>
  <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
</head>
<body>
  <div style='width: 600px; margin: auto;'>
    <h2 style='text-align: center;'>Comparație APFD</h2>
    <canvas id='apfdChart'></canvas>
  </div>

  <script>
    const ctx = document.getElementById('apfdChart').getContext('2d');
    const chart = new Chart(ctx, {{
      type: 'bar',
      data: {{
        labels: ['Prioritizat', 'Random'],
        datasets: [{{
          label: 'APFD',
          data: [{apfdOrdered}, {apfdRandom}],
          backgroundColor: ['#4CAF50', '#FF9800']
        }}]
      }},
      options: {{
        scales: {{
          y: {{
            beginAtZero: true,
            max: 1.0
          }}
        }}
      }}
    }});
  </script>
</body>
</html>";
        File.WriteAllText(outputPath, html, Encoding.UTF8);
        Console.WriteLine($"📊 Grafic exportat în: {outputPath}");
    }
}
