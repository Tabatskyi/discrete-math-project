namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        {
            int vertices = 20; 
            double probability = 1; 

            var graphGenerator = new Generator(vertices, probability);
            var graph = graphGenerator.GenerateGraph();
            Utility.PrintGraph(graph);

            AntColony antColony = new(vertices, 60, graph, 1, 5, 0.5, 0.1);
            antColony.RunOptimization(100);
            var bestTour = antColony.bestTour;
            foreach (var city in bestTour) 
            {
                Console.WriteLine($"-> {city} ");
            }
        }
    }
}