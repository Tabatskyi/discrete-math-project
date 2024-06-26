﻿using System.Diagnostics;
using System.Globalization;

namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

        int[] sizes = [20, 35, 50, 75, 100, 111, 125, 150, 175, 200];
        double[] densities = [0.5, 0.6, 0.7, 0.9, 1.0];
        int experimentCount = 2;
   
        string csvHeader = "Graph Size,Density,Average Tour Lenght,Average Memory Used (bytes),Average Time Taken (ms)";

        Dictionary<(int, double), (long memorySum, double timeSum, double lenghtSum, int count)> listResults = [];
        Dictionary<(int, double), (long memorySum, double timeSum, double lenghtSum, int count)> matrixResults = [];

        foreach (double density in densities)
        {
            List<Task> tasks = [];
            foreach (int size in sizes)
            {
                for (int expNum = 0; expNum < experimentCount; expNum++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        string result = PerformExperiment(size, density, true);
                        var parts = result.Split(',');
                        long memoryUsed = long.Parse(parts[2]);
                        double timeTaken = double.Parse(parts[3]);
                        double lenght = double.Parse(parts[4]);

                        lock (listResults)
                        {
                            if (!listResults.ContainsKey((size, density)))
                                listResults[(size, density)] = (0, 0, 0, 0);

                            var (sumMem, sumTime, sumLenght, count) = listResults[(size, density)];
                            listResults[(size, density)] = (sumMem + memoryUsed, sumTime + timeTaken, sumLenght + lenght, count + 1);
                        }

                        Console.WriteLine($"List graph: Size {size}, Density {density}.");

                    }));

                    tasks.Add(Task.Run(() =>
                    {
                        string result = PerformExperiment(size, density, false);
                        var parts = result.Split(',');
                        long memoryUsed = long.Parse(parts[2]);
                        double timeTaken = double.Parse(parts[3]);
                        double lenght = double.Parse(parts[4]);

                        lock (matrixResults)
                        {
                            if (!matrixResults.ContainsKey((size, density)))
                                matrixResults[(size, density)] = (0, 0, 0, 0);

                            var (sumMem, sumTime,sumLenght, count) = matrixResults[(size, density)];
                            matrixResults[(size, density)] = (sumMem + memoryUsed, sumTime + timeTaken, sumLenght + lenght, count + 1);
                        }

                    Console.WriteLine($"Matrix graph: Size {size}, Density {density}.");

                    }));
                }
            }

            Task.WaitAll([.. tasks]);

            using var listWriter = new StreamWriter($"results_list_graph_{density}.csv");
            listWriter.WriteLine(csvHeader);

            foreach (var ((size, d), (memSum, timeSum, lenghtSum, count)) in listResults)
            {
                if (d == density)  
                    listWriter.WriteLine($"{size},{density},{lenghtSum / count:F4},{memSum / count},{timeSum / count:F3}");
            }
            

            using var matrixWriter = new StreamWriter($"results_matrix_graph_{density}.csv") ;
            matrixWriter.WriteLine(csvHeader);

            foreach (var ((size, d), (memSum, timeSum, lenghtSum, count)) in matrixResults)
            {
                if (d == density)
                    matrixWriter.WriteLine($"{size},{density},{lenghtSum / count},{memSum / count},{timeSum / count:F3}");
            }
            
            listResults.Clear();
            matrixResults.Clear();
        }

        stopwatch.Stop();

        Console.WriteLine($"Total elapsed time: {stopwatch.Elapsed}.\nPress any key to exit.");
        Console.ReadKey();

    }

    private static string PerformExperiment(int size, double density, bool isList)
    {
        int antsCount = 60;
        double alphaValue = 1;
        double betaValue = 5;
        double evaporationValue = 0.5;
        double QValue = 0.1;

        var graphGenerator = new Generator(size, density);
        graphGenerator.GenerateGraph();

        Stopwatch stopwatch = Stopwatch.StartNew();
        long memoryBefore = GC.GetTotalMemory(true);

        AntColony antColony;
        if (isList)
        {
            var graphList = graphGenerator.graphList;
            antColony = new AntColony(size, antsCount, graphList, alphaValue, betaValue, evaporationValue, QValue);
        }
        else
        {
            var graphMatrix = graphGenerator.graphMatrix;
            antColony = new AntColony(size, antsCount, graphMatrix, alphaValue, betaValue, evaporationValue, QValue);
        }

        antColony.RunOptimization(10);

        long memoryAfter = GC.GetTotalMemory(false);
        stopwatch.Stop();

        long memoryUsed = Math.Abs(memoryAfter - memoryBefore);
        double timeTaken = stopwatch.Elapsed.TotalMilliseconds;

        return $"{size},{density},{memoryUsed},{timeTaken:F3},{antColony.CalculateTourLength(antColony.bestTour):F4}";
    }

}

