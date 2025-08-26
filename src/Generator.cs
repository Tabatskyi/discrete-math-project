namespace DiscreteMathProject;

public class Generator
{
    private readonly Random rand = new();
    private readonly int n;
    private readonly double p;
    public double[,] graphMatrix;

    public Generator(int vertices, double probability)
    {
        n = vertices;
        p = probability;
        graphMatrix = new double[n, n];
    }

    public void GenerateGraph()
    {
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i != j && rand.NextDouble() <= p)
                {
                    double weight = Math.Round(1.0 + rand.NextDouble() * 9.0, 2);
                    graphMatrix[i, j] = weight;
                }
                else
                {
                    graphMatrix[i, j] = 0;
                }
            }
        }
    }
}