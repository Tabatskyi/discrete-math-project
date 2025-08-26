using ManagedCuda;
using ManagedCuda.VectorTypes;

namespace DiscreteMathProject;

public class AntColony
{
    private const int BlockSize = 256;

    private readonly double[,] distances;
    private readonly double[,] pheromones;

    private readonly int numberOfVerts;
    private readonly int numberOfAnts;

    private readonly double initialPheromone;

    private readonly CudaContext context;

    private readonly int[] toursBuffer; // flattened: numberOfAnts * numberOfVerts

    public int[] bestTour; // single best tour of length numberOfVerts

    public AntColony(int verts, int ants, double[,] dists, double Q, CudaContext context)
    {
        ArgumentNullException.ThrowIfNull(dists);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(verts);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ants);
        if (dists.GetLength(0) != verts || dists.GetLength(1) != verts)
            throw new ArgumentException("Distance matrix must be square with size equal to verts.", nameof(dists));

        numberOfVerts = verts;
        numberOfAnts = ants;
        distances = dists;
        initialPheromone = Q;
        this.context = context;

        pheromones = new double[numberOfVerts, numberOfVerts];
        toursBuffer = new int[numberOfAnts * numberOfVerts];
        bestTour = new int[numberOfVerts];

        InitializePheromones();
    }

    private void InitializePheromones()
    {
        for (int i = 0; i < numberOfVerts; i++)
            for (int j = 0; j < numberOfVerts; j++)
                pheromones[i, j] = initialPheromone;
    }

    public void RunOptimization(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

        string modulePath = ResolveBestModule();
        var module = context.LoadModule(modulePath);

        CudaDeviceVariable<double> d_graph = new(distances.Length);
        CudaDeviceVariable<int> d_tours = new(numberOfAnts * numberOfVerts);
        CudaDeviceVariable<double> d_pheromones = new(pheromones.Length);

        try
        {
            d_graph.CopyToDevice(distances);
            d_pheromones.CopyToDevice(pheromones);

            var constructToursKernel = new CudaKernel("ConstructTours", module)
            {
                BlockDimensions = new dim3(BlockSize, 1, 1),
                GridDimensions = new dim3((numberOfAnts + BlockSize - 1) / BlockSize, 1, 1)
            };

            var updatePheromonesKernel = new CudaKernel("UpdatePheromones", module)
            {
                BlockDimensions = new dim3(BlockSize, 1, 1),
                GridDimensions = new dim3((numberOfVerts + BlockSize - 1) / BlockSize, 1, 1)
            };

            for (int i = 0; i < iterations; i++)
            {
                constructToursKernel.Run(
                    d_graph.DevicePointer,
                    d_tours.DevicePointer,
                    d_pheromones.DevicePointer,
                    numberOfAnts,
                    numberOfVerts
                );

                updatePheromonesKernel.Run(
                    d_pheromones.DevicePointer,
                    d_tours.DevicePointer,
                    numberOfAnts,
                    numberOfVerts
                );
            }

            d_pheromones.CopyToHost(pheromones);
            d_tours.CopyToHost(toursBuffer);

            SelectBestTourFromBuffer();
        }
        finally
        {
            d_graph.Dispose();
            d_tours.Dispose();
            d_pheromones.Dispose();
        }
    }

    private string ResolveBestModule()
    {
        try
        {
            var info = context.GetDeviceInfo();
            int sm = info.ComputeCapability.Major * 10 + info.ComputeCapability.Minor;
            string cubin = $"CudaKernel.sm{sm}.cubin";
            if (File.Exists(cubin))
                return cubin;
        }
        catch
        {
            const string ptx = "CudaKernel.ptx";
            if (File.Exists(ptx))
                return ptx;
        }

        throw new FileNotFoundException("No suitable CUDA module found. Expected CudaKernel.smXY.cubin or CudaKernel.ptx next to the executable.");
    }

    private void SelectBestTourFromBuffer()
    {
        double bestLen = double.PositiveInfinity;
        int bestIdx = -1;

        for (int ant = 0; ant < numberOfAnts; ant++)
        {
            int offset = ant * numberOfVerts;
            double len = CalculateTourLength(toursBuffer, offset, numberOfVerts);
            if (len < bestLen)
            {
                bestLen = len;
                bestIdx = ant;
            }
        }

        if (bestIdx >= 0)
        {
            Array.Copy(toursBuffer, bestIdx * numberOfVerts, bestTour, 0, numberOfVerts);
        }
    }

    public double CalculateTourLength(int[] tour)
    {
        if (tour == null || tour.Length != numberOfVerts)
            throw new ArgumentException("Tour must have the same length as number of vertices.", nameof(tour));

        return CalculateTourLength(tour, 0, tour.Length);
    }

    private double CalculateTourLength(int[] tour, int offset, int count)
    {
        double length = 0;

        for (int i = 0; i < count - 1; i++)
        {
            int v1 = tour[offset + i];
            int v2 = tour[offset + i + 1];

            if ((uint)v1 >= (uint)numberOfVerts || (uint)v2 >= (uint)numberOfVerts)
                return double.PositiveInfinity;

            double d = distances[v1, v2];
            if (d <= 0)
                return double.PositiveInfinity; // invalid or missing edge

            length += d;
        }

        return length;
    }
}