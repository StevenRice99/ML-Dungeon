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
    /// Run a script.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="delay">The seconds to wait before starting to play. Set to zero or less for no launching of Unity.</param>
    /// <param name="scene">The scene to load.</param>
    private static async void RunScript([NotNull] string name, float delay = 5f, [NotNull] string scene = "Training")
    {
        if (delay > 0)
        {
            // Exit play mode to then re-establish it after.
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
            
            // Ensure the right scene is loaded
            LoadScene(scene);
        }
        
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
            return;
        }
        
        // Try to play the game.
        if (delay <= 0)
        {
            return;
        }
        
        await Awaitable.WaitForSecondsAsync(delay);
        EditorApplication.isPlaying = true;
    }
    
    /// <summary>
    /// Load a scene.
    /// </summary>
    /// <param name="name">The scene to load.</param>
    private static void LoadScene([NotNull] string name)
    {
        // Find all scene assets in the project.
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            // Convert the GUID to an actual file path.
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Verify it's an exact name match.
            if (Path.GetFileNameWithoutExtension(path) != name)
            {
                continue;
            }
            
            // Prompt the user to save any unsaved work in the current scene.
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            
            return;
        }
    }
}
#endif