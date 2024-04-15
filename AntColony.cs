namespace DiscreteMathProject;


public class AntColony
{
    private readonly Random rand = new();
    private readonly int numberOfVerts;
    private readonly int numberOfAnts;
    private readonly Dictionary<int, List<Tuple<int, double>>> distances;
    private readonly Dictionary<int, List<Tuple<int, double>>> pheromones;
    private readonly double pherMultiplier;
    private readonly double distMultiplier;
    private readonly double evaporationRate;
    private readonly double initialPheromone;
    private double bestTourLength = double.MaxValue;
    public int[] bestTour;

    public AntColony(int cities, int ants, Dictionary<int, List<Tuple<int, double>>> dists, double alpha, double beta, double evaporation, double initialPher)
    {
        numberOfVerts = cities;
        numberOfAnts = ants;
        distances = dists;
        pheromones = [];

        pherMultiplier = alpha;
        distMultiplier = beta;
        evaporationRate = evaporation;
        initialPheromone = initialPher;

        bestTour = [];

        InitializePheromones();
    }

    private void InitializePheromones()
    {
        foreach (var vert in distances.Keys)
        {
            pheromones[vert] = distances[vert].Select(edge => new Tuple<int, double>(edge.Item1, initialPheromone)).ToList();
        }
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
            int nextCity = ChooseNextVert(currentVert, visited);
            if (nextCity == -1) 
                break;
            tour.Add(nextCity);
            visited.Add(nextCity);
            currentVert = nextCity;
        }

        /*if (tour.Count == numberOfVerts && distances[currentVert].Any(d => d.Item1 == startVert))
            tour.Add(startVert);*/

        return [.. tour];
    }

    private int ChooseNextVert(int currentVert, HashSet<int> visited)
    {
        if (!pheromones.TryGetValue(currentVert, out List<Tuple<int, double>>? value) || value == null)
            return -1;

        var accessibleVerts = value.Where(x => !visited.Contains(x.Item1) && x != null).ToList();

        if (accessibleVerts.Count == 0)
            return -1;

        double sum = 0;
        foreach (var vert in accessibleVerts)
        {
            var distanceEntry = distances[currentVert].First(d => d.Item1 == vert.Item1);
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
            var distanceEntry = distances[currentVert].First(d => d.Item1 == vert.Item1);
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
        foreach (var vert in pheromones.Keys)
        {
            for (int i = 0; i < pheromones[vert].Count; i++)
            {
                var edge = pheromones[vert][i];
                pheromones[vert][i] = new Tuple<int, double>(edge.Item1, edge.Item2 * (1 - evaporationRate));
            }
        }
    }

    public void RunOptimization(int iterations)
    {
        for (int iter = 0; iter < iterations; iter++)
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
                for (int i = 0; i < solution.Length - 1; i++)
                {
                    UpdatePheromones(solution[i], solution[i + 1], depositAmount);
                }
            }
        }
    }
    

    private void UpdatePheromones(int vert1, int vert2, double depositAmount)
    {
        var edgeIndex = pheromones[vert1].FindIndex(x => x.Item1 == vert2);
        if (edgeIndex != -1) 
        {
            var edge = pheromones[vert1][edgeIndex];
            pheromones[vert1][edgeIndex] = new Tuple<int, double>(edge.Item1, edge.Item2 + depositAmount);
        }
        else
            pheromones[vert1].Add(new Tuple<int, double>(vert2, depositAmount));
        

        edgeIndex = pheromones[vert2].FindIndex(x => x.Item1 == vert1);
        if (edgeIndex != -1)
        {
            var edge = pheromones[vert2][edgeIndex];
            pheromones[vert2][edgeIndex] = new Tuple<int, double>(edge.Item1, edge.Item2 + depositAmount);
        }
        else
            pheromones[vert2].Add(new Tuple<int, double>(vert1, depositAmount));
        
    }


    private double CalculateTourLength(int[] tour)
    {
        if (tour == null || tour.Length < 2)
            throw new ArgumentException("Tour is null or too short to calculate length.");

        double length = 0;
        for (int i = 0; i < tour.Length - 1; i++)
        {
            int vert1 = tour[i];
            int vert2 = tour[i + 1];

            if (distances.TryGetValue(vert1, out List<Tuple<int, double>>? value) && value != null)
            {
                var edge = value.First(e => e.Item1 == vert2);
                if (!edge.Equals(default(Tuple<int, double>)))
                {
                    length += edge.Item2;
                }
                else
                    throw new InvalidOperationException($"No direct link between vertice {vert1} and vertice {vert2}.");
                
            }
            else
                throw new InvalidOperationException($"No entries found for vertice {vert1} in distances.");
            
        }

        if (tour.Length > 1 && distances.ContainsKey(tour.Last()) && distances[tour.Last()].Any(d => d.Item1 == tour[0]))
        {
            var returnEdge = distances[tour.Last()].First(e => e.Item1 == tour[0]);
            if (!returnEdge.Equals(default(Tuple<int, double>)))
            {
                length += returnEdge.Item2;
            }
            else
            {
                throw new InvalidOperationException($"No return link from vertice {tour.Last()} to vertice {tour[0]}.");
            }
        }

        return length;
    }



}

