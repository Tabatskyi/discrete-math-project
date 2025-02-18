extern "C" __device__ double atomicAddDouble(double* address, double val) {
#if __CUDA_ARCH__ >= 600
	return atomicAdd(address, val);
#else
	unsigned long long int* address_as_ull = (unsigned long long int*)address;
	unsigned long long int old = *address_as_ull, assumed;
	do {
		assumed = old;
		old = atomicCAS(address_as_ull, assumed,
			__double_as_longlong(val + __longlong_as_double(assumed)));
	} while (assumed != old);
	return __longlong_as_double(old);
#endif
}

extern "C" __global__ void ConstructTours(double* graph, int* tours, double* pheromones, int numAnts, int numCities)
{
    int antIndex = blockIdx.x * blockDim.x + threadIdx.x;
    if (antIndex < numAnts)
    {
		int cityIndex = 0;
		tours[antIndex * numCities + cityIndex] = cityIndex;
		for (cityIndex = 1; cityIndex < numCities; cityIndex++)
		{
			int currentCity = tours[antIndex * numCities + cityIndex - 1];
			int nextCity = -1;
			double maxPheromone = 0.0;
			for (int i = 0; i < numCities; i++)
			{
				if (graph[currentCity * numCities + i] > 0)
				{
					int visited = 0;
					for (int j = 0; j < cityIndex; j++)
					{
						if (tours[antIndex * numCities + j] == i)
						{
							visited = 1;
							break;
						}
					}
					if (!visited)
					{
						double pheromone = pheromones[currentCity * numCities + i];
						if (pheromone > maxPheromone)
						{
							maxPheromone = pheromone;
							nextCity = i;
						}
					}
				}
			}

			if (nextCity == -1)
			{
				for (int i = 0; i < numCities; i++)
				{
					int visited = 0;
					for (int j = 0; j < cityIndex; j++)
					{
						if (tours[antIndex * numCities + j] == i)
						{
							visited = 1;
							break;
						}
					}
					if (!visited)
					{
						nextCity = i;
						break;
					}
				}
			}

			tours[antIndex * numCities + cityIndex] = nextCity;
		}
    }
}

extern "C" __global__ void UpdatePheromones(double* pheromones, int* tours, int numAnts, int numCities)
{
    int cityIndex = blockIdx.x * blockDim.x + threadIdx.x;
    if (cityIndex < numCities)
    {
		for (int i = 0; i < numCities; i++)
		{
			for (int j = 0; j < numAnts; j++)
			{
				int city1 = tours[j * numCities + cityIndex];
				int city2 = tours[j * numCities + (cityIndex + 1) % numCities];
				if (city1 != -1 && city2 != -1)
				{
					atomicAddDouble(&pheromones[city1 * numCities + city2], 1.0);
					atomicAddDouble(&pheromones[city2 * numCities + city1], 1.0);
				}
			}
		}
    }
}
