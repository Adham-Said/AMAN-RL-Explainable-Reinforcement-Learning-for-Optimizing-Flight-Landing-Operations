using UnityEngine;
using System;

public class RandomGenerator
{
    private System.Random random;

    public RandomGenerator()
    {
        random = new System.Random();
    }

    public float Next()
    {
        return (float)random.NextDouble();
    }

    public float Range(float min, float max)
    {
        return min + Next() * (max - min);
    }

    public float Exponential(float mean)
    {
        float u = (float)random.NextDouble();
        // Avoid taking log of 0
        while (u == 0) u = (float)random.NextDouble();
        return -mean * Mathf.Log(u);
    }
} 