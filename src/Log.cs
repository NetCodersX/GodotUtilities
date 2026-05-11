using Godot;
using System;
using System.Collections.Generic;

namespace Utilities;

public static class Log
{
    public enum Level { Info, Warn, Error, Debug }

    public static bool Enabled { get; set; } = true;

    private static readonly HashSet<string> disabledCategories = [];

    public static void Disable(string category) => disabledCategories.Add(category);
    public static void Enable(string category) => disabledCategories.Remove(category);

    public static void Warn(string category, string message) => Write(Level.Warn, category, message);
    public static void Error(string category, string message) => Write(Level.Error, category, message);

    public static void Info(string category, string message) => Write(Level.Info, category, message);
    public static void Debug(string category, string message) => Write(Level.Debug, category, message);
    
    private static void Write(Level level, string category, string message)
    {
        if (!Enabled) return;
        if (disabledCategories.Contains(category)) return;

        string prefix = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{category}][{level}]";
        string line = prefix + " " + message;

        switch (level)
        {
            case Level.Debug: 
                GD.PrintRich("[color=gray]" + line + "[/color]"); 
                break;

            case Level.Info: GD.Print(line); break;
            case Level.Error: GD.PushError(line); break;
            case Level.Warn: GD.PushWarning(line); break;
        }
    }
}

