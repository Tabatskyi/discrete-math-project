using ManagedCuda;
using ManagedCuda.VectorTypes;

namespace DiscreteMathProject;

public class AntColony
{
    private readonly double[,] distances;
    private readonly double[,] pheromones;

    private readonly Random rand = new();

    private readonly int numberOfVerts;
    private readonly int numberOfAnts;

    private readonly double pherMultiplier;
    private readonly double distMultiplier;
    private readonly double initialPheromone;

    private readonly CudaContext context;

    public int[] bestTour;

    public AntColony(int verts, int ants, double[,] dists, double alpha, double beta, double evaporation, double Q, CudaContext context)
    {
        numberOfVerts = verts;
        numberOfAnts = ants;

        distances = dists;

        pherMultiplier = alpha;
        distMultiplier = beta;
        initialPheromone = Q;

        this.context = context;

        pheromones = new double[numberOfVerts, numberOfVerts];
        bestTour = new int[numberOfAnts * numberOfVerts];

        InitializePheromones();
        GenerateSolution();
    }

    private void InitializePheromones()
    {
        for (int i = 0; i < numberOfVerts; i++)
            for (int j = 0; j < numberOfVerts; j++)
                pheromones[i, j] = initialPheromone;
       
    }

    private int[] GenerateSolution()
    {
        List<int> tour = [];
        int startVert = rand.Next(numberOfVerts);
        tour.Add(startVert);
        HashSet<int> visited = [startVert];
        int currentVert = startVert;

        while (tour.Count < numberOfVerts)
        {
            int nextVert = ChooseNextVert(currentVert, visited);
            if (nextVert == -1)
                break;

            tour.Add(nextVert);
            visited.Add(nextVert);
            currentVert = nextVert;
        }

        return [.. tour];
    }

    private int ChooseNextVert(int currentVert, HashSet<int> visited)
    {
        List<int> accessibleVerts = [];
        double sum = 0;

        for (int i = 0; i < numberOfVerts; i++)
        {
            if (!visited.Contains(i) && distances[currentVert, i] > 0)
            {
                double pheromone = pheromones[currentVert, i];
                double distance = distances[currentVert, i];
                sum += Math.Pow(pheromone, pherMultiplier) * Math.Pow(1.0 / distance, distMultiplier);
                accessibleVerts.Add(i);
            }
        }

        if (sum <= 0)
            return -1;

        double randomPoint = rand.NextDouble() * sum;
        double cumulativeProbability = 0;

        foreach (int vert in accessibleVerts)
        {
            double pheromone = pheromones[currentVert, vert];
            double distance = distances[currentVert, vert];
            cumulativeProbability += Math.Pow(pheromone, pherMultiplier) * Math.Pow(1.0 / distance, distMultiplier);
            if (cumulativeProbability >= randomPoint)
                return vert;
        }

        return -1;
    }

    public void RunOptimization(int iterations)
    {
        var module = context.LoadModule("CudaKernel.ptx");

        CudaDeviceVariable<double> d_graph = new(distances.Length);
        d_graph.CopyToDevice(distances);

        CudaDeviceVariable<int> d_tours = new(numberOfAnts * numberOfVerts);
        CudaDeviceVariable<double> d_pheromones = new(pheromones.Length);
        d_pheromones.CopyToDevice(pheromones);

        var constructToursKernel = new CudaKernel("ConstructTours", module);
        var updatePheromonesKernel = new CudaKernel("UpdatePheromones", module);

        int blockSize = 256;
        int gridSizeAnts = (numberOfAnts + blockSize - 1) / blockSize;
        int gridSizeCities = (numberOfVerts + blockSize - 1) / blockSize;

        for (int i = 0; i < iterations; i++)
        {
            constructToursKernel.BlockDimensions = new dim3(blockSize, 1, 1);
            constructToursKernel.GridDimensions = new dim3(gridSizeAnts, 1, 1);
            constructToursKernel.Run(
                d_graph.DevicePointer,
                d_tours.DevicePointer,
                d_pheromones.DevicePointer,
                numberOfAnts,
                numberOfVerts
            );

            updatePheromonesKernel.BlockDimensions = new dim3(blockSize, 1, 1);
            updatePheromonesKernel.GridDimensions = new dim3(gridSizeCities, 1, 1);
            updatePheromonesKernel.Run(
                d_pheromones.DevicePointer,
                d_tours.DevicePointer,
                numberOfAnts,
                numberOfVerts
            );
        }

        d_pheromones.CopyToHost(pheromones);
        d_tours.CopyToHost(bestTour);

        d_graph.Dispose();
        d_tours.Dispose();
        d_pheromones.Dispose();
    }

    public double CalculateTourLength(int[] tour)
    {
        if (tour == null || tour.Length < 2)
            throw new Exception("Tour is null or too short to calculate length.");

        double length = 0;

        for (int i = 0; i < tour.Length - 1; i++)
        {
            int vert1 = tour[i];
            int vert2 = tour[i + 1];

            length += distances[vert1, vert2];
        }

        return length;
    }
}