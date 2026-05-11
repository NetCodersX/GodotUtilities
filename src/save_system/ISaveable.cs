using Godot.Collections;
using Utilities.Persistence.Reflection;

namespace Utilities.Persistence;

public interface ISaveable
{
    string SaveKey { get; }

    virtual string ScenePath => "";
    virtual string ParentPath => "";

    virtual void OnSerialize(Dictionary data) { }
    virtual void OnDeserialize(Dictionary data) { }

    Dictionary Serialize()
    {
        var dict = SaveReflector.Serialize(this);

        dict[SaveManager.SCENE_PATH_ID] = ScenePath;
        dict[SaveManager.PARENT_PATH_ID] = ParentPath;

        OnSerialize(dict);
        return dict;
    }

    void Deserialize(Dictionary data)
    {
        SaveReflector.Deserialize(this, data);
        OnDeserialize(data);
    }
}

public interface ISerializableResource
{
    Dictionary Serialize();
    void Deserialize(Dictionary data);   
}