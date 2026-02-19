using UnityEngine;

/// <summary>
/// Managing training of agents.
/// </summary>
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
    private int levels = 1;
    
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
        float shift = (maxSize + 2) * levelPrefab.PieceSpacing;
        float starting = -((levels - 1) / 2f) * shift;
        Debug.Log(starting);
        Transform t = transform;
        for (int i = 0; i < levels; i++)
        {
            Level level = Instantiate(levelPrefab, new(starting, 0, 0), Quaternion.identity, t);
            level.name = levelPrefab.name;
            starting += shift;
        }
    }
}