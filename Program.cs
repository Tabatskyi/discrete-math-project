using System.Diagnostics;

namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        int[] sizes = [20, 35, 50, 75, 100, 111, 125, 150, 175, 200];
        double[] densities = [1.0];
        int experimentCount = 20;

        Task[] tasks = new Task[sizes.Length];

        for (int j = 0; j < sizes.Length; j++)
        {
            int size = sizes[j];
            tasks[j] = Task.Run(() =>
            {
                foreach (double density in densities)
                {
                    var graphGenerator = new Generator(size, density);
                    graphGenerator.GenerateGraph();
                    var graphList = graphGenerator.graphList;

                    long totalMemoryUsed = 0;
                    double totalTimeTaken = 0;

                    for (int i = 0; i < experimentCount; i++)
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        long memoryBefore = GC.GetTotalMemory(true);

                        AntColony antColony = new AntColony(size, 60, graphList, 1, 5, 0.5, 0.1);
                        antColony.RunOptimization(100);

                        long memoryAfter = GC.GetTotalMemory(false);
                        stopwatch.Stop();

                        totalMemoryUsed += memoryAfter - memoryBefore;
                        totalTimeTaken += stopwatch.Elapsed.TotalMilliseconds;
                    }

                    Console.WriteLine($"Graph size: {size}, Density: {density}");
                    Console.WriteLine($"Average Memory Used: {totalMemoryUsed / experimentCount} bytes");
                    Console.WriteLine($"Average Time Taken: {totalTimeTaken / experimentCount} ms");
                    Console.WriteLine();
                }
            });
        }

        Task.WaitAll(tasks);
    }
}

