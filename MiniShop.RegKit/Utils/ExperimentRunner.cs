using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MiniShop.RegKit.Utils;

public static class ExperimentRunner
{
    public static void RunRegressionComparison(string basePath)
    {
        var impactPath = Path.Combine(basePath, "impact.yaml");

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

        var result = APFDCalculator.ComputeFromYaml(impactPath, faultsPerTest);

        Console.WriteLine("✅ Prioritizare automată:");
        foreach (var test in result.PrioritizedTests)
        {
            Console.WriteLine($" - {test}");
        }
        Console.WriteLine($"📈 APFD (prioritizat): {result.APFD}");

        var rnd = new Random();
        var shuffled = result.PrioritizedTests.OrderBy(_ => rnd.Next()).ToList();

        var randomResult = APFDCalculator.ComputeFromYamlFromList(shuffled, faultsPerTest);
        Console.WriteLine($"\n🎲 APFD (random): {randomResult.APFD}");

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

        File.WriteAllText(Path.Combine(basePath, "results.yaml"), serializer.Serialize(report), Encoding.UTF8);
        Console.WriteLine("📄 Salvat raport în results.yaml");

        ExportComparisonHtml(Path.Combine(basePath, "apfd_comparison.html"), result.PrioritizedTests, shuffled, result.APFD, randomResult.APFD);
        ExportComparisonChartHtml(Path.Combine(basePath, "apfd_chart.html"), result.APFD, randomResult.APFD);
    }

    private static void ExportComparisonHtml(string outputPath, List<string> ordered, List<string> randomized, double apfdOrdered, double apfdRandom)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'><title>APFD Comparison</title></head><body>");
        sb.AppendLine($"<h2>APFD Prioritizat: {apfdOrdered}</h2>");
        sb.AppendLine($"<h2>APFD Random: {apfdRandom}</h2>");
        sb.AppendLine("<h3>Ordine prioritizată:</h3><ul>");
        ordered.ForEach(t => sb.AppendLine($"<li>{t}</li>"));
        sb.AppendLine("</ul><h3>Ordine random:</h3><ul>");
        randomized.ForEach(t => sb.AppendLine($"<li>{t}</li>"));
        sb.AppendLine("</ul></body></html>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
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
    }
}
