using Godot;
using Godot.Collections;

namespace Utilities.Persistence;

/// <summary>
/// Adapts a node to the save system by allowing selected properties
/// to be serialized and restored without modifying the node itself.
///
/// This is especially useful for nodes that:
/// - Do not have a script
/// - Use only built-in properties
/// - Should avoid custom Serialize/Deserialize implementations
///
/// The adapter inspects the parent node and provides a filtered list
/// of valid properties that can be saved via the editor.
/// </summary>
[Tool, GlobalClass]
public partial class PropertySaveAdapter : Node, ISaveable
{
    [Export] private string uniqueId = "";

    [Export] public bool DynamicSpawn { get; private set; } = false;
    [Export] private Array<string> PropertiesToSave { get; set; } = [];

    private static readonly System.Collections.Generic.HashSet<string> SkippedProps =
        ["", "script", "__meta__"];

    private Array<string> suggestedProperties = [];

    private Node cachedParent;
    private string cachedKey;
    private string cachedScenePath;

    private Node ParentNode => cachedParent ??= GetParent();

    public string SaveKey 
    {
        get 
        {
            if (!string.IsNullOrEmpty(uniqueId)) 
                return $"prop_{uniqueId}";

            if (GetParent() is ISaveable parentSaveable)
                return parentSaveable.SaveKey;

            return $"prop_{GetParent().GetPath()}";
        }
    }

    public string ScenePath
    {
        get
        {
            if (!DynamicSpawn) return null;

            if (!string.IsNullOrEmpty(cachedScenePath)) return cachedScenePath;
            cachedScenePath = ParentNode.SceneFilePath;
            return cachedScenePath;
        }
    }

    public override void _Notification(int what)
    {
        if (!Engine.IsEditorHint()) return;

        if (what == NotificationParented)
            UpdatePropertyList();

        if (what == NotificationPostEnterTree)
            UpdatePropertyList();
    }

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint()) return;
        SaveManager.Instance.Register(this);
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;
        SaveManager.Instance.Unregister(this);
    }

    private void UpdatePropertyList()
    {
        if (ParentNode == null) return;

        suggestedProperties.Clear();

        var props = ParentNode.GetPropertyList();

        foreach (var prop in props)
        {
            if (!prop.TryGetValue("name", out var nameVar)) continue;
            
            string name = nameVar.AsString();

            if (SkippedProps.Contains(name)) continue;

            if (name.StartsWith('/')) continue;
            if (!prop.TryGetValue("type", out var typeVar)) continue;

            var type = (Variant.Type)typeVar.AsInt32();
            if (type is Variant.Type.Nil or Variant.Type.Object) continue;

            if (prop.TryGetValue("usage", out var usageVar))
            {
                var usage = (PropertyUsageFlags)usageVar.AsInt32();
                if (usage.HasFlag(PropertyUsageFlags.Editor))
                    continue;
            }

            suggestedProperties.Add(name);
        }

        NotifyPropertyListChanged();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsString() == nameof(PropertiesToSave))
        {
            string options = string.Join(",", suggestedProperties);

            property["hint"] = (int)PropertyHint.TypeString;
            property["hint_string"] =
                $"{(int)Variant.Type.String}/{(int)PropertyHint.Enum}:{options}";
        }
    }

    public void OnSerialize(Dictionary data)
    {
        foreach (string prop in PropertiesToSave)
        {
            var value = ParentNode.Get(prop);
            if (value.VariantType != Variant.Type.Nil)
                data[prop] = value;
        }
    }

    public void OnDeserialize(Dictionary data)
    {
        foreach (string prop in PropertiesToSave)
        {
            if (data.TryGetValue(prop, out Variant value))
                ParentNode.Set(prop, value);
        }
    }
}
