namespace DiscreteMathProject;

public class Utility
{
    public static void PrintGraph(Dictionary<int, List<Tuple<int, double>>> graph)
    {
        Console.WriteLine("Graph Adjacency List:");
        foreach (var vertex in graph.Keys)
        {
            Console.Write($"Vertex {vertex} -> ");
            foreach (var edge in graph[vertex])
            {
                Console.Write($"(to: {edge.Item1}, weight: {edge.Item2:F2}) ");
            }
            Console.WriteLine();
        }
    }
}
