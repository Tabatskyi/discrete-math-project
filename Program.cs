namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        {
            int vertices = 20; 
            double probability = 1; 

            var graphGenerator = new Generator(vertices, probability);
            graphGenerator.GenerateGraph();
            var graphList = graphGenerator.graphList;
            var graphMatrix = graphGenerator.graphMatrix;
            Utility.PrintListGraph(graphList);

            AntColony antColony = new(vertices, 60, graphList, 1, 5, 0.5, 0.1);
            antColony.RunOptimization(100);
            var bestTour = antColony.bestTour;
            foreach (var vert in bestTour) 
            {
                Console.WriteLine($"-> {vert} ");
            }
            Console.WriteLine();

            AntColony antColony2 = new(vertices, 60, graphList, 1, 5, 0.5, 0.1);
            antColony2.RunOptimization(100);
            var bestTour2 = antColony2.bestTour;
            foreach (var vert in bestTour2)
            {
                Console.WriteLine($"-> {vert} ");
            }
        }
    }
}