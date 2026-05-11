using System.Collections.Generic;
using Godot;

namespace Utilities;

public static class ResourceLoaderUtil
{
    /// <summary>
    /// Loads all resources of type <typeparamref name="T"/> from a folder (non-recursive).
    /// Silently skips files that don't match the type or can't be loaded.
    /// </summary>
    public static IEnumerable<T> LoadResourcesFrom<T>(string folderPath) where T : Resource
        => LoadResources<T>(folderPath, recursive: false);

    /// <summary>
    /// Loads all resources of type <typeparamref name="T"/> from a folder and all subfolders.
    /// Silently skips files that don't match the type or can't be loaded.
    /// </summary>
    public static IEnumerable<T> LoadResourcesFromRecursive<T>(string folderPath) where T : Resource
        => LoadResources<T>(folderPath, recursive: true);

    /// <summary>
    /// Returns a cached resource by id, loading it on first access.
    /// Tries each extension in order until one resolves.
    /// Returns null and logs a warning if no file is found.
    /// </summary>
    public static T LoadById<T>(
        Dictionary<StringName, T> cache,
        string folderPath,
        StringName id,
        params string[] extensions) where T : Resource
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;

        foreach (var ext in extensions)
        {
            var resource = TryLoad<T>($"{folderPath}/{id}.{ext}");
            if (resource is null) continue;

            cache[id] = resource;
            return resource;
        }

        Log.Warn("ResourceLoaderUtil", $"Resource not found for id '{id}' in '{folderPath}'");
        return null;
    }

    private static IEnumerable<T> LoadResources<T>(string folderPath, bool recursive) where T : Resource
    {
        if (!DirAccess.DirExistsAbsolute(folderPath))
        {
            Log.Warn("ResourceLoaderUtil", $"Folder not found: '{folderPath}'");
            yield break;
        }

        foreach (var resource in Walk<T>(folderPath, recursive))
            yield return resource;
    }

    private static IEnumerable<T> Walk<T>(string folderPath, bool recursive) where T : Resource
    {
        var dir = DirAccess.Open(folderPath);
        if (dir == null)
        {
            Log.Warn("ResourceLoaderUtil", $"Could not open folder: '{folderPath}' (Error: {DirAccess.GetOpenError()})");
            yield break;
        }

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (fileName.StartsWith('.'))
                continue;

            string fullPath = $"{folderPath}/{fileName}";

            if (dir.CurrentIsDir())
            {
                if (recursive)
                    foreach (var res in Walk<T>(fullPath, recursive: true))
                        yield return res;
                continue;
            }

            var loaded = TryLoad<T>(fullPath);
            if (loaded != null)
                yield return loaded;
        }

        dir.ListDirEnd();
    }

    public static T GetOrLoad<T>(StringName id, Dictionary<StringName, T> cache, 
        Godot.Collections.Dictionary<StringName, string> pathDict) where T : Resource
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;
        
        if (pathDict.TryGetValue(id, out string path))
        {
            var stream = GD.Load<T>(path);
            if (stream is not null)
            {
                cache[id] = stream;
                return stream;
            }
            GD.PushError($"AudioManager: failed to load stream for '{id}' at path '{path}'");
        }
        return null;
    }

    /// <summary>
    /// Safely loads a resource, returning null if the file doesn't exist,
    /// isn't the expected type, or fails to load.
    /// </summary>
    private static T TryLoad<T>(string fullPath) where T : Resource
    {
        if (!ResourceLoader.Exists(fullPath))
            return null;

        try
        {
            return ResourceLoader.Load<T>(fullPath);
        }
        catch (System.InvalidCastException)
        {
            return null;
        }
    }
}