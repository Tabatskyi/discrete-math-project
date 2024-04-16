namespace DiscreteMathProject;

public class Generator(int vertices, double probability)
{
    private readonly Random rand = new();
    private readonly int n = vertices;
    private readonly double p = probability;
    public Dictionary<int, List<Tuple<int, double>>> graphList;
    public double[,] graphMatrix;

    public void GenerateGraph()
    {
        graphList = [];

        for (int i = 0; i < n; i++)
        {
            graphList[i] = []; 
            for (int j = 0; j < n; j++)
            {
                if (i != j && rand.NextDouble() <= p)
                {
                    double weight = (double)Math.Ceiling((decimal)((i + j).GetHashCode() << 6)) / 33;
                    graphList[i].Add(new Tuple<int, double>(j, weight));
                }
            }
        }

        graphMatrix = new double[n, n];
        foreach (var kvp in graphList)
            foreach (var edge in kvp.Value)
                graphMatrix[kvp.Key, edge.Item1] = edge.Item2;
    }
}
