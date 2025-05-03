using UnityEngine;
using System;
using System.Collections.Generic;

public class SimAttribute
{
    public string Name { get; private set; }
    public object Value { get; private set; }

    public SimAttribute(string name, object value)
    {
        Name = name;
        Value = value;
    }
}

public class SimEvent : IComparable<SimEvent>
{
    private float time;
    private Dictionary<string, object> attributes;

    public float Time { get { return time; } }

    public SimEvent(float time)
    {
        this.time = time;
        this.attributes = new Dictionary<string, object>();
    }

    public SimEvent(string type, float time)
    {
        this.time = time;
        this.attributes = new Dictionary<string, object>();
        AddAttribute("Type", type);
    }

    public void AddAttribute(string key, object value)
    {
        attributes[key] = value;
    }

    public T GetAttributeValue<T>(string key)
    {
        if (attributes.ContainsKey(key))
        {
            return (T)attributes[key];
        }
        return default(T);
    }

    public int CompareTo(SimEvent other)
    {
        return Time.CompareTo(other.Time);
    }
} 