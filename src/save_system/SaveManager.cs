using System;
using Godot;

namespace Utilities.Persistence;

public partial class SaveManager : Node
{
    public enum Slot { Slot1, Slot2, Slot3, Slot4 }

    public const string PARENT_PATH_ID = "__parent_path__";
    public const string SCENE_PATH_ID = "__scene_path__";
    public const string SAVE_FOLDER_PATH = "user://saves";

    private const Slot DEFAULT_SLOT = Slot.Slot1;

    private const string GAME_SAVED_TEXT = "[color=green]Save Manager: Game Saved ![/color]";
    private const string GAME_LOADED_TEXT = "[color=yellow]Save Manager: Game Loaded ![/color]";

    public static readonly StringName SaveGroup = "Saveable";

    public static SaveManager Instance { get; private set; }

    private string[] slots;    

    public override void _EnterTree()
    {
        Instance = this;

        var names = Enum.GetNames(typeof(Slot));
        slots = new string[names.Length];

        for (int i = 0; i < names.Length; i++)
            slots[i] = $"{SAVE_FOLDER_PATH}/{names[i]}.dat";
    }

    private string GetSlotPath(Slot slot) => slots[(int)slot];

    public void Save() => Save(DEFAULT_SLOT);
    public bool Load() => Load(DEFAULT_SLOT);

    public void Save(Slot slot)
    {
        var root = new Godot.Collections.Dictionary();

        foreach (var node in GetTree().GetNodesInGroup(SaveGroup))
        {
            if (node is not ISaveable saveable) continue;

            string key = saveable.SaveKey;
            var data = saveable.Serialize();

            if (root.ContainsKey(key))
            {
                var existing = root[key].AsGodotDictionary();
                foreach (var kvp in data)
                    existing[kvp.Key] = kvp.Value;
            }
            else
            {
                root[key] = data;
            }
        }

        if (!DirAccess.DirExistsAbsolute(SAVE_FOLDER_PATH))
        {
            var err = DirAccess.MakeDirRecursiveAbsolute(SAVE_FOLDER_PATH);
            if (err != Error.Ok)
                GD.PrintErr($"Failed to create directory: {SAVE_FOLDER_PATH}, Error: {err}");
        }

        using var file = FileAccess.Open(GetSlotPath(slot), FileAccess.ModeFlags.Write);

        if (file == null)
        {
            GD.PrintErr($"Failed to open save file. Error: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreVar(root);

        GD.PrintRich(GAME_SAVED_TEXT);
    }

    public bool Load(Slot slot)
    {
        string path = GetSlotPath(slot);
        if (!FileAccess.FileExists(path)) return false;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null) return false;

        var root = file.GetVar().AsGodotDictionary();

        var sceneNodes = new System.Collections.Generic.Dictionary<string, Node>();
        foreach (var node in GetTree().GetNodesInGroup(SaveGroup))
        {
            if (IsInstanceValid(node) && node is ISaveable s)
                sceneNodes[s.SaveKey] = node;
        }

        foreach (var kvp in root)
        {
            string savedKey = kvp.Key.AsString();
            var nodeData = kvp.Value.AsGodotDictionary();

            if (!sceneNodes.ContainsKey(savedKey))
            {
                if (nodeData.TryGetValue(SCENE_PATH_ID, out var pathVar))
                {
                    string scenePath = pathVar.AsString();
                    if (string.IsNullOrEmpty(scenePath)) continue;

                    string parentPath = nodeData.TryGetValue(PARENT_PATH_ID, out var pVar) 
                        ? pVar.AsString() : "";

                    var prefab = GD.Load<PackedScene>(scenePath);
                    var newNode = prefab.Instantiate();
                    
                    var parent = GetNodeOrNull(parentPath) ?? GetTree().CurrentScene;
                    parent.AddChild(newNode);
                    
                    sceneNodes[savedKey] = newNode;
                }
            }
        }

        foreach (var kvp in root)
        {
            string savedKey = kvp.Key.AsString();
            if (sceneNodes.TryGetValue(savedKey, out Node node) && IsInstanceValid(node))
            {
                var data = kvp.Value.AsGodotDictionary();

                if (node is ISaveable saveable)
                    saveable.Deserialize(data);
            
                var adapter = node.GetNodeOrNull<PropertySaveAdapter>("PropertySaveAdapter");
                if (adapter is ISaveable asSaveable && node != adapter)
                    asSaveable.Deserialize(data);
            }
        }

        GD.PrintRich(GAME_LOADED_TEXT);
        return true;
    }

    public bool SaveExists() => FileAccess.FileExists(GetSlotPath(DEFAULT_SLOT));
    public bool SaveExists(Slot slot) => FileAccess.FileExists(GetSlotPath(slot));

    public void DeleteSave() => DirAccess.RemoveAbsolute(GetSlotPath(DEFAULT_SLOT));
    public void DeleteSave(Slot slot) => DirAccess.RemoveAbsolute(GetSlotPath(slot));
}

