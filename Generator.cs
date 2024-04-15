namespace DiscreteMathProject;

public class Generator(int vertices, double probability)
{
    private readonly Random rand = new();
    private readonly int n = vertices;
    private readonly double p = probability;

    public Dictionary<int, List<Tuple<int, double>>> GenerateGraph()
    {
        var graph = new Dictionary<int, List<Tuple<int, double>>>();

        for (int i = 0; i < n; i++)
        {
            graph[i] = new List<Tuple<int, double>>(); 
            for (int j = 0; j < n; j++)
            {
                if (i != j && rand.NextDouble() <= p)
                {
                    double weight = rand.NextDouble() * 10;
                    graph[i].Add(new Tuple<int, double>(j, weight));
                }
            }
        }

        return graph;
    }
}
