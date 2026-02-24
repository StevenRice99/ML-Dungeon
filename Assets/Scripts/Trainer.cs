using UnityEngine;

/// <summary>
/// Managing training of agents.
/// </summary>
[AddComponentMenu("ML-Dungeon/Trainer")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
public class Trainer : MonoBehaviour
{
    /// <summary>
    /// The <see cref="Level"/> instance object.
    /// </summary>
    [field: Tooltip("The level instance object.")]
    [field: SerializeField]
    public Level LevelPrefab { get; private set; }
    
    /// <summary>
    /// The number of <see cref="Level"/> instances to create.
    /// </summary>
    [field: Tooltip("The number of level instances to create.")]
    [field: Min(1)]
    [field: SerializeField]
    public int Levels { get; private set; } = 16;
    
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
    /// The maximum wall percentage that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The maximum wall percentage that can be spawned in any scenario.")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float MaxWalls { get; private set; } = 0.2f;
    
    /// <summary>
    /// The maximum amount of enemies that can be spawned in any scenario.
    /// </summary>
    [field: Tooltip("The maximum amount of enemies that can be spawned in any scenario.")]
    [field: Min(0)]
    [field: SerializeField]
    public int MaxEnemies { get; private set; } = 5;
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        if (MinSize > MaxSize)
        {
            (MaxSize, MinSize) = (MinSize, MaxSize);
        }
    }
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
    /// </summary>
    private void Start()
    {
        OnValidate();
        
        // Calculate grid dimensions for a roughly square layout.
        int columns = Mathf.CeilToInt(Mathf.Sqrt(Levels));
        int rows = Mathf.CeilToInt((float)Levels / columns);
        
        // Account for the size of dungeon tiles.
        float shift = (MaxSize + 2) * LevelPrefab.PieceSpacing;
        
        // Calculate starting offsets along the X and Z axes to keep the grid centered.
        float startX = -((columns - 1) / 2f) * shift;
        float startZ = -((rows - 1) / 2f) * shift;
        
        // Place all dungeon instances.
        Transform t = transform;
        Vector3 p = transform.position;
        for (int i = 0; i < Levels; i++)
        {
            // Determine the 2D column and row positions for the current index.
            int col = i % columns;
            int row = i / columns;
            
            // Apply the offsets to the X and Z axes.
            Level level = Instantiate(LevelPrefab, p + new Vector3(startX + col * shift, 0, startZ + row * shift), Quaternion.identity, t);
            level.name = LevelPrefab.name;
        }
    }
}