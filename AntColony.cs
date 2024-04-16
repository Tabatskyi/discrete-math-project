namespace DiscreteMathProject;

public class AntColony
{
    private readonly Dictionary<int, List<Tuple<int, double>>>? listDistances;
    private readonly Dictionary<int, List<Tuple<int, double>>>? listPheromones;
    private readonly double[,]? matrixDistances;
    private readonly double[,]? matrixPheromones;

    private readonly Random rand = new();

    private readonly int numberOfVerts;
    private readonly int numberOfAnts;

    private readonly double pherMultiplier;
    private readonly double distMultiplier;
    private readonly double evaporationRate;
    private readonly bool matrix;
    private readonly double initialPheromone;
    private double bestTourLength = double.PositiveInfinity;

    public int[] bestTour;

    public AntColony(int verts, int ants, Dictionary<int, List<Tuple<int, double>>> dists, double alpha, double beta, double evaporation, double Q)
    {
        matrix = false;
        numberOfVerts = verts;
        numberOfAnts = ants;
        
        listDistances = dists;

        pherMultiplier = alpha;
        distMultiplier = beta;
        evaporationRate = evaporation;
        initialPheromone = Q;

        listPheromones = [];
        bestTour = [];

        InitializePheromones();
    }

    public AntColony(int verts, int ants, double[,] dists, double alpha, double beta, double evaporation, double Q)
    {
        matrix = true;
        numberOfVerts = verts;
        numberOfAnts = ants;

        matrixDistances = dists;

        pherMultiplier = alpha;
        distMultiplier = beta;
        evaporationRate = evaporation;
        initialPheromone = Q;

        matrixPheromones = new double[numberOfVerts, numberOfVerts];
        bestTour = [];

        InitializePheromones();
    }


    private void InitializePheromones()
    {
        if (matrix)
            for (int i = 0; i < numberOfVerts; i++)
                for (int j = 0; j < numberOfVerts; j++)
                    matrixPheromones[i, j] = initialPheromone;
        else
            foreach (var vert in listDistances.Keys)
                listPheromones[vert] = listDistances[vert].Select(edge => new Tuple<int, double>(edge.Item1, initialPheromone)).ToList();
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
        if (matrix)
            return ChooseNextVertMatrix(currentVert, visited);
        else
            return ChooseNextVertList(currentVert, visited);
    }


    private int ChooseNextVertMatrix(int currentVert, HashSet<int> visited)
    {
        List<int> accessibleVerts = [];
        double sum = 0;

        for (int i = 0; i < numberOfVerts; i++)
        {
            if (!visited.Contains(i) && matrixDistances[currentVert, i] > 0)
            {
                double pheromone = matrixPheromones[currentVert, i];
                double distance = matrixDistances[currentVert, i];
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
            double pheromone = matrixPheromones[currentVert, vert];
            double distance = matrixDistances[currentVert, vert];
            cumulativeProbability += Math.Pow(pheromone, pherMultiplier) * Math.Pow(1.0 / distance, distMultiplier);
            if (cumulativeProbability >= randomPoint)
                return vert;
        }

        return -1;
    }


    private int ChooseNextVertList(int currentVert, HashSet<int> visited)
    {
        if (!listPheromones.TryGetValue(currentVert, out List<Tuple<int, double>>? value) || value == null)
            return -1;

        var accessibleVerts = value.Where(x => !visited.Contains(x.Item1) && x != null).ToList();

        if (accessibleVerts.Count == 0)
            return -1;

        double sum = 0;
        foreach (var vert in accessibleVerts)
        {
            var distanceEntry = listDistances[currentVert].FirstOrDefault(d => d.Item1 == vert.Item1);

            if (distanceEntry != null && distanceEntry.Item2 > 0)
            {
                double pheromone = vert.Item2;
                double distance = distanceEntry.Item2;
                sum += Math.Pow(pheromone, pherMultiplier) * Math.Pow(1.0 / distance, distMultiplier);
            }
        }

        if (sum <= 0)
            return -1;

        double randomPoint = rand.NextDouble() * sum;
        double cumulativeProbability = 0;

        foreach (var vert in accessibleVerts)
        {
            var distanceEntry = listDistances[currentVert].First(d => d.Item1 == vert.Item1);
            if (distanceEntry != null && distanceEntry.Item2 > 0) 
            {
                double pheromone = vert.Item2;
                double distance = distanceEntry.Item2;
                cumulativeProbability += Math.Pow(pheromone, pherMultiplier) * Math.Pow(1.0 / distance, distMultiplier);
                if (cumulativeProbability >= randomPoint)
                    return vert.Item1;
            }
        }

        return -1; 
    }


    private void EvaporatePheromones()
    {
        if (matrix)
            for (int i = 0; i < numberOfVerts; i++)
                for (int j = 0; j < numberOfVerts; j++)
                    matrixPheromones[i, j] *= 1 - evaporationRate;
        
        else
            foreach (var vert in listPheromones.Keys)
                for (int i = 0; i < listPheromones[vert].Count; i++)
                {
                    var edge = listPheromones[vert][i];
                    listPheromones[vert][i] = new Tuple<int, double>(edge.Item1, edge.Item2 * (1 - evaporationRate));
                }
        
    }



    public void RunOptimization(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            List<int[]> solutions = [];

            for (int ant = 0; ant < numberOfAnts; ant++)
            {
                var tour = GenerateSolution();
                double currentTourLength = CalculateTourLength(tour);
                if (currentTourLength < bestTourLength)
                {
                    bestTourLength = currentTourLength;
                    bestTour = tour;
                }
                solutions.Add(tour);
            }

            EvaporatePheromones();

            foreach (var solution in solutions)
            {
                double tourLength = CalculateTourLength(solution);
                double depositAmount = 1000.0 / tourLength;

                for (int j = 0; j < solution.Length - 1; j++)
                    UpdatePheromones(solution[j], solution[j + 1], depositAmount);            
            }
        }
    }


    private void UpdatePheromones(int vert1, int vert2, double depositAmount)
    {
        if (matrix)
        {
            matrixPheromones[vert1, vert2] += depositAmount;
            matrixPheromones[vert2, vert1] += depositAmount; 
        }
        else
        {
            UpdatePheromonesList(listPheromones, vert1, vert2, depositAmount);
            UpdatePheromonesList(listPheromones, vert2, vert1, depositAmount); 
        }
    }

    private void UpdatePheromonesList(Dictionary<int, List<Tuple<int, double>>> pheromones, int vert1, int vert2, double depositAmount)
    {
        var edgeIndex = pheromones[vert1].FindIndex(x => x.Item1 == vert2);

        if (edgeIndex != -1)
        {
            var edge = pheromones[vert1][edgeIndex];
            pheromones[vert1][edgeIndex] = new Tuple<int, double>(edge.Item1, edge.Item2 + depositAmount);
        }
        else
            pheromones[vert1].Add(new Tuple<int, double>(vert2, depositAmount));
    }



    private double CalculateTourLength(int[] tour)
    {
        if (tour == null || tour.Length < 2)
            throw new Exception("Tour is null or too short to calculate length.");

        double length = 0;

        for (int i = 0; i < tour.Length - 1; i++)
        {
            int vert1 = tour[i];
            int vert2 = tour[i + 1];

            if (matrix)
            {
                length += matrixDistances[vert1, vert2];
            }
            else
            {
                if (listDistances.TryGetValue(vert1, out List<Tuple<int, double>>? value) && value != null)
                {
                    var edge = value.FirstOrDefault(e => e.Item1 == vert2);
                    if (!edge.Equals(default(Tuple<int, double>)))
                        length += edge.Item2;
                    else
                        throw new Exception($"No direct link between vertice {vert1} and vertice {vert2}.");
                }
                else
                    throw new Exception($"No entries found for vertice {vert1} in distances.");
            }
        }

        return length;
    }


}

