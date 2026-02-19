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
    [Tooltip("The level instance object.")]
    [SerializeField]
    private Level levelPrefab;
    
    /// <summary>
    /// The number of <see cref="Level"/> instances to create.
    /// </summary>
    [Tooltip("The number of level instances to create.")]
    [Min(1)]
    [SerializeField]
    private int levels = 16;
    
    /// <summary>
    /// The maximum size that <see cref="Level"/> instances can be up to.
    /// </summary>
    [Tooltip("The maximum size that level instances can be up to.")]
    [Min(2)]
    [SerializeField]
    private int maxSize = 20;
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
    /// </summary>
    private void Start()
    {
        // Calculate grid dimensions for a roughly square layout.
        int columns = Mathf.CeilToInt(Mathf.Sqrt(levels));
        int rows = Mathf.CeilToInt((float)levels / columns);
        
        // Account for the size of dungeon tiles.
        float shift = (maxSize + 2) * levelPrefab.PieceSpacing;
        
        // Calculate starting offsets along the X and Z axes to keep the grid centered.
        float startX = -((columns - 1) / 2f) * shift;
        float startZ = -((rows - 1) / 2f) * shift;
        
        // Place all dungeon instances.
        Transform t = transform;
        Vector3 p = transform.position;
        for (int i = 0; i < levels; i++)
        {
            // Determine the 2D column and row positions for the current index.
            int col = i % columns;
            int row = i / columns;
            
            // Apply the offsets to the X and Z axes.
            Level level = Instantiate(levelPrefab, p + new Vector3(startX + col * shift, 0, startZ + row * shift), Quaternion.identity, t);
            level.name = levelPrefab.name;
        }
    }
}