using UnityEngine;
using System.Collections.Generic;

public abstract class DES : MonoBehaviour
{
    protected float clock = 0f;  // Simulation clock
    protected float timeScale = 1f;  // Time scale for simulation speed
    protected bool isRunning = false;
    protected RandomGenerator random;

    private PriorityQueue<SimEvent> eventQueue;

    protected virtual void Awake()
    {
        eventQueue = new PriorityQueue<SimEvent>();
        random = new RandomGenerator();
        isRunning = true;
    }

    protected void Schedule(SimEvent e)
    {
        eventQueue.Enqueue(e);
    }

    protected void AddEvent(SimEvent e)
    {
        Schedule(e);
    }

    protected abstract void HandleEvent(SimEvent e);
    public abstract void Report();

    private void Update()
    {
        if (!isRunning) return;

        while (eventQueue.Count > 0)
        {
            SimEvent nextEvent = eventQueue.Peek();
            if (nextEvent.Time <= clock)
            {
                eventQueue.Dequeue();
                HandleEvent(nextEvent);
            }
            else
            {
                break;
            }
        }

        clock += Time.deltaTime * timeScale;
    }
} 