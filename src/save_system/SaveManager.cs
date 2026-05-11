using Godot;
using Godot.Collections;

namespace Utilities.Persistence;

public partial class SaveManager : Node
{
    public const string DEFAULT_FOLDER_PATH = "user://saves";
    public const string DEFAULT_SLOT = "slot1";

    public const string PARENT_PATH_ID = "__parent_path__";
    public const string SCENE_PATH_ID = "__scene_path__";

    private const string GAME_SAVED_TEXT = "[color=green]Save Manager: Game Saved ![/color]";
    private const string GAME_LOADED_TEXT = "[color=yellow]Save Manager: Game Loaded ![/color]";

    private readonly System.Collections.Generic.HashSet<ISaveable> saveables = [];

    public static SaveManager Instance { get; private set; }

    private Dictionary saveCache;

    public override void _EnterTree() => Instance = this;
    private string GetSlotPath(string slotId) => $"{DEFAULT_FOLDER_PATH}/{slotId}.dat";

    public void Register(ISaveable saveable) => saveables.Add(saveable);
    public void Unregister(ISaveable saveable) => saveables.Remove(saveable);

    public void Save() => Save(DEFAULT_SLOT);
    public bool Load() => Load(DEFAULT_SLOT);

    #region Save

    public void Save(string slotId, string folderPath = DEFAULT_FOLDER_PATH)
    {
        GetOrCreateFolder(folderPath);

        var root = CollectData();
        using var file = FileAccess.Open(GetSlotPath(slotId), FileAccess.ModeFlags.Write);

        if (file is null)
        {
            GD.PrintErr($"Failed to open save file. Error: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreVar(root);
        GD.PrintRich(GAME_SAVED_TEXT);
    }

    private void GetOrCreateFolder(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
        {
            var err = DirAccess.MakeDirRecursiveAbsolute(path);
            if (err != Error.Ok)
                GD.PrintErr($"Failed to create directory: {path}, Error: {err}");
        }
    }

    private Dictionary CollectData()
    {
        var dict = new Dictionary();

        foreach (ISaveable saveable in saveables)
        {
            var key = saveable.SaveKey;
            var data = saveable.Serialize();

            if (dict.ContainsKey(key))
            {
                var existing = dict[key].AsGodotDictionary();
                foreach (var kvp in data)
                    existing[kvp.Key] = kvp.Value;
            }
            else { dict[key] = data; }
        }

        return dict;
    }

    #endregion

    #region Load

    public bool Load(string slotId)
    {
        string path = GetSlotPath(slotId);
        if (!FileAccess.FileExists(path)) return false;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null) return false;

        var root = file.GetVar().AsGodotDictionary();
        saveCache = root;

        var sceneNodes = new Dictionary<string, Node>();
        foreach (ISaveable saveable in saveables)
            sceneNodes[saveable.SaveKey] = saveable as Node;

        DynamicallySpawn(root, sceneNodes);
        DeserializeAll(root, sceneNodes);

        GD.PrintRich(GAME_LOADED_TEXT);
        return true;
    }

    private void DynamicallySpawn(Dictionary root, Dictionary<string, Node> nodes)
    {
        foreach (var kvp in root)
        {
            string savedKey = kvp.Key.AsString();
            var nodeData = kvp.Value.AsGodotDictionary();

            if (nodes.ContainsKey(savedKey)) continue;
            if (!nodeData.TryGetValue(SCENE_PATH_ID, out var pathVar)) continue;

            string scenePath = pathVar.AsString();
            if (string.IsNullOrEmpty(scenePath)) continue;

            string parentPath = nodeData.TryGetValue(PARENT_PATH_ID, out var pVar) 
                ? pVar.AsString() : "";

            var prefab = GD.Load<PackedScene>(scenePath);
            var newNode = prefab.Instantiate();
            
            var parent = GetNodeOrNull(parentPath) ?? GetTree().CurrentScene;
            parent.AddChild(newNode);
            
            nodes[savedKey] = newNode;
        }
    }

    private void DeserializeAll(Dictionary root, Dictionary<string, Node> nodes)
    {
        foreach (var kvp in root)
        {
            string savedKey = kvp.Key.AsString();
            if (nodes.TryGetValue(savedKey, out Node node) && IsInstanceValid(node))
            {
                var data = kvp.Value.AsGodotDictionary();

                if (node is ISaveable saveable)
                    saveable.Deserialize(data);
            
                var adapter = node.GetNodeOrNull<PropertySaveAdapter>("PropertySaveAdapter");
                if (adapter is ISaveable asSaveable && node != adapter)
                    asSaveable.Deserialize(data);
            }
        }
    }

    #endregion

    #region Utilities

    public bool SaveExists() => FileAccess.FileExists(GetSlotPath(DEFAULT_SLOT));
    public bool SaveExists(string slotId) => FileAccess.FileExists(GetSlotPath(slotId));

    public void DeleteSave() => DirAccess.RemoveAbsolute(GetSlotPath(DEFAULT_SLOT));
    public void DeleteSave(string slotId) => DirAccess.RemoveAbsolute(GetSlotPath(slotId));

    public bool TryGetData(string key, out Dictionary result)
    {
        if (saveCache is null) { result = null; return false; }
        result = saveCache.TryGetValue(key, out Variant value) ? value.AsGodotDictionary() : null;
        return result != null;
    }

    public System.Collections.Generic.List<string> GetAvailableSlots()
    {
        var slots = new System.Collections.Generic.List<string>();
        using var dir = DirAccess.Open(DEFAULT_FOLDER_PATH);
        if (dir == null) return slots;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".dat"))
                slots.Add(fileName.GetBaseName());
            fileName = dir.GetNext();
        }
        return slots;
    }

    #endregion
}

