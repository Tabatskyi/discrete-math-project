namespace DiscreteMathProject;

class Program
{
    public static void Main()
    {
        {
            int vertices = 20; 
            double probability = 0.3; 

            var graphGenerator = new Generator(vertices, probability);
            var graph = graphGenerator.GenerateGraph();
            Utility.PrintGraph(graph);
        }
    }
}