#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Helper to run the training scripts from the Unity editor.
/// </summary>
public static class TrainingRunner
{
    /// <summary>
    /// Run behavioural cloning.
    /// </summary>
    [MenuItem("ML-Dungeon/Behavioral Cloning", false, 0)]
    public static void BehavioralCloning()
    {
        RunScript("Pretrain.bat");
    }
    
    /// <summary>
    /// Run transfer learning.
    /// </summary>
    [MenuItem("ML-Dungeon/Transfer Learning", false, 1)]
    public static void TransferLearning()
    {
        RunScript("Tune.bat");
    }
    
    /// <summary>
    /// Run curriculum learning.
    /// </summary>
    [MenuItem("ML-Dungeon/Curriculum Learning", false, 2)]
    public static void CurriculumLearning()
    {
        RunScript("Curriculum.bat");
    }
    
    /// <summary>
    /// Run transfer and curriculum learning.
    /// </summary>
    [MenuItem("ML-Dungeon/Transfer and Curriculum Learning", false, 3)]
    public static void TransferCurriculumLearning()
    {
        RunScript("Full.bat");
    }
    
    /// <summary>
    /// Monitor the learning in Tensorboard.
    /// </summary>
    [MenuItem("ML-Dungeon/Tensorboard", false, 14)]
    public static void Tensorboard()
    {
        RunScript("Monitor.bat");
    }
    
    /// <summary>
    /// Install a Python environment.
    /// </summary>
    [MenuItem("ML-Dungeon/Install", false, 25)]
    public static void Install()
    {
        RunScript("Install.bat");
    }
    
    /// <summary>
    /// Activate the Python environment.
    /// </summary>
    [MenuItem("ML-Dungeon/Activate", false, 26)]
    public static void Activate()
    {
        RunScript("Activate.bat");
    }
    
    /// <summary>
    /// Run a script.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    private static void RunScript([NotNull] string name)
    {
        // Get the directory.
        string directory = Path.GetDirectoryName(Application.dataPath);
        if (directory == null)
        {
            Debug.LogError($"Parent of \"{Application.dataPath}\" does not exist.");
            return;
        }
        
        if (!name.EndsWith(".bat"))
        {
            name = $"{name}.bat";
        }
        
        // Get the file.
        string file = Path.Combine(directory, name);
        if (!File.Exists(file))
        {
            Debug.LogError($"\"{file}\" does not exist.");
            return;
        }
        
        // Start the file in its own process in the correct working directory.
        ProcessStartInfo processInfo = new()
        {
            FileName = file,
            WorkingDirectory = directory,
            UseShellExecute = true
        };
        
        // Try to run it.
        try
        {
            Process.Start(processInfo);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to execute \"{file}\": {e.Message}");
        }
    }
}
#endif