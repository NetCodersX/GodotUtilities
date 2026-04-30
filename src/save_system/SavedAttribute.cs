using System;

namespace Utilities.Persistence;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SaveAttribute(string key = null) : Attribute
{
    public string Key { get; } = key;
}