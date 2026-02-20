using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Manage automatic sample recordings for a <see cref="Level.Agent"/>.
/// </summary>
[AddComponentMenu("ML-Dungeon/Recording")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
public class Recording : MonoBehaviour
{
    /// <summary>
    /// The number of attempts per <see cref="settings"/> to record.
    /// </summary>
    [Tooltip("The number of attempts per settings to record.")]
    [Min(1)]
    [SerializeField]
    private int attempts = 10;
    
    /// <summary>
    /// The <see cref="DungeonSettings"/> to record.
    /// </summary>
    [Tooltip("The settings to record.")]
    [SerializeField]
    private DungeonSettings[] settings = Array.Empty<DungeonSettings>();
    
    /// <summary>
    /// The current <see cref="settings"/> index.
    /// </summary>
    private int _setting;
    
    /// <summary>
    /// The current <see cref="_attempt"/> of the current <see cref="settings"/>.
    /// </summary>
    private int _attempt;
    
    /// <summary>
    /// Awake is called when an enabled script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        Level level = FindAnyObjectByType<Level>(FindObjectsInactive.Include);
        if (!level)
        {
            return;
        }
        
        GetCurrentSettings(out int size, out float walls, out int enemies);
        level.Size = size;
        level.WallPercent = walls;
        level.DesiredEnemies = enemies;
    }
    
    /// <summary>
    /// Get the next recording settings.
    /// </summary>
    /// <param name="size">The size of the level to generate.</param>
    /// <param name="walls">What percentage of the floors should attempt to be randomly made into walls.</param>
    /// <param name="enemies">How many enemies to try and spawn.</param>
    /// <returns>The name to use for the recording.</returns>
    public string GetNextSettings(out int size, out float walls, out int enemies)
    {
        // See if we should advance to the next setting.
        if (_attempt >= attempts)
        {
            _setting++;
            _attempt = 0;
        }
        
        // If we are done, stop.
        if (_setting >= settings.Length)
        {
            size = 2;
            walls = 0f;
            enemies = 0;
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return "0-0-0-0";
        }
        
        // Otherwise, get the information.
        size = settings[_setting].Size;
        walls = settings[_setting].Walls;
        enemies = settings[_setting].Enemies;
        return $"{size}-{walls}-{enemies}-{_attempt++}";
    }
    
    
    /// <summary>
    /// Get the current recording settings.
    /// </summary>
    /// <param name="size">The size of the level to generate.</param>
    /// <param name="walls">What percentage of the floors should attempt to be randomly made into walls.</param>
    /// <param name="enemies">How many enemies to try and spawn.</param>
    /// <returns>The name to use for the recording.</returns>
    public string GetCurrentSettings(out int size, out float walls, out int enemies)
    {
        if (_attempt >= attempts || _setting >= settings.Length)
        {
            size = 2;
            walls = 0f;
            enemies = 0;
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return "0-0-0-0";
        }
        
        size = settings[_setting].Size;
        walls = settings[_setting].Walls;
        enemies = settings[_setting].Enemies;
        return $"{size}-{walls}-{enemies}-{_attempt}";
    }
    
    /// <summary>
    /// Settings for how to generate the dungeon.
    /// </summary>
    [Serializable]
    public class DungeonSettings
    {
        /// <summary>
        /// The size of the level to generate.
        /// </summary>
        [field: Tooltip("The size of the level to generate.")]
        [field: Min(2)]
        [field: SerializeField]
        public int Size { get; private set; } = 2;
        
        /// <summary>
        /// What percentage of the floors should attempt to be randomly made into walls.
        /// </summary>
        [field: Tooltip("What percentage of the floors should attempt to be randomly made into walls.")]
        [field: Range(0f, 1f)]
        [field: SerializeField]
        public float Walls { get; private set; }
        
        /// <summary>
        /// How many enemies to try and spawn.
        /// </summary>
        [field: Tooltip("How many enemies to try and spawn.")]
        [field: Min(0)]
        [field: SerializeField]
        public int Enemies { get; private set; }
    }
}