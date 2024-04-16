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

        string filePath = $"results_list_graph.csv";
        string csvHeader = "Graph Size,Density,Average Memory Used (bytes),Average Time Taken (ms)";

        using StreamWriter writer = new(filePath);
        writer.WriteLine(csvHeader);

        Task[] listTasks = new Task[sizes.Length];

        for (int j = 0; j < sizes.Length; j++)
        {
            int size = sizes[j];
            listTasks[j] = Task.Run(() =>
            {
                foreach (double density in densities)
                {
                    long totalMemoryUsed = 0;
                    double totalTimeTaken = 0;

                    for (int i = 0; i < experimentCount; i++)
                    {
                        var graphGenerator = new Generator(size, density);
                        graphGenerator.GenerateGraph();
                        var graphList = graphGenerator.graphList;

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        long memoryBefore = GC.GetTotalMemory(true);

                        AntColony antColony = new(size, 60, graphList, 1, 5, 0.5, 0.1);
                        antColony.RunOptimization(10);

                        long memoryAfter = GC.GetTotalMemory(false);
                        stopwatch.Stop();

                        totalMemoryUsed += memoryAfter - memoryBefore;
                        totalTimeTaken += stopwatch.Elapsed.TotalMilliseconds;
                    }

                    writer.WriteLine($"{size},{density},{totalMemoryUsed / experimentCount},{totalTimeTaken / experimentCount:F3}");
                }
            });
        }

        Task.WaitAll(listTasks);

        Task[] matrixTasks = new Task[sizes.Length];

        string matrixFilePath = $"results_matrix_graph.csv";
        using StreamWriter matrixWriter = new(matrixFilePath);
        matrixWriter.WriteLine(csvHeader);

        for (int j = 0; j < sizes.Length; j++)
        {
            int size = sizes[j];
            matrixTasks[j] = Task.Run(() =>
            {
                foreach (double density in densities)
                {
                    long totalMemoryUsed = 0;
                    double totalTimeTaken = 0;

                    for (int i = 0; i < experimentCount; i++)
                    {
                        var graphGenerator = new Generator(size, density);
                        graphGenerator.GenerateGraph();
                        var graphMatrix = graphGenerator.graphMatrix;

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        long memoryBefore = GC.GetTotalMemory(true);

                        AntColony antColony = new(size, 60, graphMatrix, 1, 5, 0.5, 0.1);
                        antColony.RunOptimization(10);

                        long memoryAfter = GC.GetTotalMemory(false);
                        stopwatch.Stop();

                        totalMemoryUsed += memoryAfter - memoryBefore;
                        totalTimeTaken += stopwatch.Elapsed.TotalMilliseconds;
                    }

                    matrixWriter.WriteLine($"{size},{density},{totalMemoryUsed / experimentCount},{totalTimeTaken / experimentCount:F3}");
                }
            });
        }

        Task.WaitAll(matrixTasks);
    }
}

