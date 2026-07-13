using System;

public class RandomHelper
{
    private int _seed;
    private int _index;

    public RandomHelper(int seed)
    {
        _seed = seed;
        _index = 0;
    }

    public RandomHelper(string seed)
    {
        _seed = 0;
        for (int i = 0; i < seed.Length; i++)
        {
            _seed += seed[i];
        }
        _index = 0;
    }

    public int GetRandom()
    {
        _index++;
        long n = _seed ^ (_index * 747796405L + 2891336453L);
        n = (n ^ (n >> 15)) * 2246822519L;
        n = (n ^ (n >> 13)) * 3266489917L;
        n = n ^ (n >> 16);
        return (int)n;
    }

    public int GetRandom(int min, int max)
    {
        long val = GetRandom();
        return min + (int)((val % (max - min) + (max - min)) % (max - min));
    }
}