using UnityEngine;
using System;

public class RandomGenerator : MonoBehaviour
{
    private System.Random random;

    public void Initialize()
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
        return -mean * (float)Math.Log(1 - random.NextDouble());
    }
} 