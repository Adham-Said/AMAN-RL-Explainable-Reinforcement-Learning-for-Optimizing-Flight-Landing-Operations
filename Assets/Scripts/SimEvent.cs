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
    private Dictionary<string, SimAttribute> attributes = new Dictionary<string, SimAttribute>();
    public float Time { get; private set; }

    public SimEvent(string type, float time)
    {
        Time = time;
        AddAttribute("Type", type);
    }

    public void AddAttribute(string name, object value)
    {
        attributes[name] = new SimAttribute(name, value);
    }

    public T GetAttributeValue<T>(string name)
    {
        if (attributes.TryGetValue(name, out SimAttribute attr))
        {
            return (T)attr.Value;
        }
        throw new KeyNotFoundException($"Attribute {name} not found");
    }

    public int CompareTo(SimEvent other)
    {
        return Time.CompareTo(other.Time);
    }
} 