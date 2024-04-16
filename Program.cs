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

        Task[] listTasks = new Task[densities.Length];

        for (int j = 0; j < densities.Length; j++)
        {
            double density = densities[j];
            string filePath = $"results_list_graph_{density}.csv";

            listTasks[j] = Task.Run(() =>
            {
                using StreamWriter writer = new(filePath);
                writer.WriteLine(csvHeader);
                foreach (int size in sizes)
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
                    Console.WriteLine($"list: {size}, {density} done");
                }
            });
        }

        Task.WaitAll(listTasks);

        Task[] matrixTasks = new Task[densities.Length];

        for (int j = 0; j < densities.Length; j++)
        {
            double density = densities[j];
            string matrixFilePath = $"results_matrix_graph_{density}.csv";
            
            matrixTasks[j] = Task.Run(() =>
            {
                using StreamWriter matrixWriter = new(matrixFilePath);
                matrixWriter.WriteLine(csvHeader);
                foreach (int size in sizes)
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
                    Console.WriteLine($"matrix: {size}, {density} done");
                }

            });
        }

        Task.WaitAll(matrixTasks);
    }
}

