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
    /// The timescale to use during the automatic heuristic phases.
    /// </summary>
    public float AutoScale => Mathf.Max(autoScale, ManualScale);
    
    /// <summary>
    /// The timescale to use during the automatic heuristic phases. If you plan on doing a fully-automatic training, simply set this to be the same as <see cref="AutoScale"/>.
    /// </summary>
    [Header("Time")]
    [Tooltip("The timescale to use during the automatic heuristic phases.")]
    [Min(1f)]
    [SerializeField]
    private float autoScale = 20f;
    
    /// <summary>
    /// The timescale to use during the heuristic phases which you can manually override. If you plan on doing a fully-automatic training, simply set this to be the same as <see cref="AutoScale"/>.
    /// </summary>
    [field: Tooltip("The timescale to use during the manual heuristic phases which you can manually override. If you plan on doing a fully-automatic training, simply set this to be the same as the auto scale.")]
    [field: Min(1f)]
    [field: SerializeField]
    public float ManualScale { get; private set; } = 1f;
    
    /// <summary>
    /// The number of recordings to make.
    /// </summary>
    [Header("Configuration")]
    [Tooltip("The number of attempts per settings to record.")]
    [Min(1)]
    [SerializeField]
    private int number = 100000;
    
    /// <summary>
    /// The minimum size that <see cref="Level"/> instances can be down to.
    /// </summary>
    [field: Tooltip("The minimum size that level instances can be down to.")]
    [field: Min(2)]
    [field: SerializeField]
    public int MinSize { get; private set; } = 10;
    
    /// <summary>
    /// The maximum size that <see cref="Level"/> instances can be up to.
    /// </summary>
    [field: Tooltip("The maximum size that level instances can be up to.")]
    [field: Min(2)]
    [field: SerializeField]
    public int MaxSize { get; private set; } = 30;
    
    /// <summary>
    /// The minimum wall percentage that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The minimum wall percentage that can be spawned in any scenario.")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float MinWalls { get; private set; } = 0.1f;
    
    /// <summary>
    /// The maximum wall percentage that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The maximum wall percentage that can be spawned in any scenario.")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float MaxWalls { get; private set; } = 0.2f;
    
    /// <summary>
    /// The minimum amount of enemies that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The minimum amount of enemies that can be spawned in any scenario.")]
    [field: Min(0)]
    [field: SerializeField]
    public int MinEnemies { get; private set; } = 1;
    
    /// <summary>
    /// The maximum amount of enemies that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The maximum amount of enemies that can be spawned in any scenario.")]
    [field: Min(0)]
    [field: SerializeField]
    public int MaxEnemies { get; private set; } = 5;
    
    /// <summary>
    /// The current <see cref="number"/> recordings being made.
    /// </summary>
    private int _number;
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        if (MinSize > MaxSize)
        {
            (MaxSize, MinSize) = (MinSize, MaxSize);
        }
        
        if (MinWalls > MaxWalls)
        {
            (MaxWalls, MinWalls) = (MinWalls, MaxWalls);
        }
        
        if (MinEnemies > MaxEnemies)
        {
            (MaxEnemies, MinEnemies) = (MinEnemies, MaxEnemies);
        }
    }
    
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
        
        OnValidate();
        
        GetRecordingSettings(out int size, out float walls, out int enemies);
        level.Size = size;
        level.WallPercent = walls;
        level.DesiredEnemies = enemies;
    }
    
    /// <summary>
    /// Advance to the next index information.
    /// </summary>
    public void AdvanceSettings()
    {
        // If we are done, stop.
        if (++_number >= number)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    
    /// <summary>
    /// Get new recording settings.
    /// </summary>
    /// <param name="size">The size of the level to generate.</param>
    /// <param name="walls">What percentage of the floors should attempt to be randomly made into walls.</param>
    /// <param name="enemies">How many enemies to try and spawn.</param>
    /// <returns>The name to use for the recording.</returns>
    public string GetRecordingSettings(out int size, out float walls, out int enemies)
    {
        if (_number >= number)
        {
            size = 2;
            walls = 0f;
            enemies = 0;
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return null;
        }
        
        size = Random.Range(MinSize, MaxSize + 1);
        walls = Random.Range(MinWalls, MaxWalls);
        enemies = Random.Range(MinEnemies, MaxEnemies + 1);
        return _number.ToString();
    }
}