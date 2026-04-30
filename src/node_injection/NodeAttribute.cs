using System;

namespace Utilities;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NodeRefAttribute(string path = null) : Attribute
{
    public string Path { get; } = path;
}