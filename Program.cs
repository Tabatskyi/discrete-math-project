using System.Diagnostics;
using System.Globalization;

namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

        int[] sizes = [20, 35, 50, 75, 100, 111, 125, 150, 175, 200];
        double[] densities = [0.5, 0.6, 0.7, 0.9, 1.0];
        int experimentCount = 20;
        string csvHeader = "Graph Size,Density,Average Memory Used (bytes),Average Time Taken (ms)";

        List<Task> tasks = [];

        foreach (double density in densities)
        {
            string listFilePath = $"results_list_graph_{density}.csv";
            string matrixFilePath = $"results_matrix_graph_{density}.csv";

            tasks.Add(Task.Run(() =>
            {
                using StreamWriter writer = new(listFilePath);
                writer.WriteLine(csvHeader);
                foreach (int size in sizes)
                {
                    PerformExperiment(writer, size, density, experimentCount, true);
                    Console.WriteLine($"list: {size}, {density} done");
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                using StreamWriter writer = new(matrixFilePath);
                writer.WriteLine(csvHeader);
                foreach (int size in sizes)
                {
                    PerformExperiment(writer, size, density, experimentCount, false);
                    Console.WriteLine($"matrix: {size}, {density} done");
                }
            }));
        }

        Task.WaitAll([.. tasks]);
    }

    private static void PerformExperiment(StreamWriter writer, int size, double density, int experimentCount, bool useList)
    {
        long totalMemoryUsed = 0;
        double totalTimeTaken = 0;

        for (int i = 0; i < experimentCount; i++)
        {
            var graphGenerator = new Generator(size, density);
            graphGenerator.GenerateGraph();

            Stopwatch stopwatch = Stopwatch.StartNew();
            long memoryBefore = GC.GetTotalMemory(true);

            AntColony antColony;
            if (useList)
            {
                var graphList = graphGenerator.graphList;
                antColony = new AntColony(size, 60, graphList, 1, 5, 0.5, 0.1);
            }
            else
            {
                var graphMatrix = graphGenerator.graphMatrix;
                antColony = new AntColony(size, 60, graphMatrix, 1, 5, 0.5, 0.1);
            }

            antColony.RunOptimization(10);

            long memoryAfter = GC.GetTotalMemory(false);
            stopwatch.Stop();

            totalMemoryUsed += memoryAfter - memoryBefore;
            totalTimeTaken += stopwatch.Elapsed.TotalMilliseconds;
        }

        writer.WriteLine($"{size},{density},{totalMemoryUsed / experimentCount},{totalTimeTaken / experimentCount:F3}");
    }
}

