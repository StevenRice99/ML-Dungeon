using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handle enemy logic.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    /// <summary>
    /// The range that this can detect the <see cref="Level.Agent"/>.
    /// </summary>
    [Tooltip("The range that this can detect the player.")]
    [Min(float.Epsilon)]
    [SerializeField]
    private float range = 5f;
    
    /// <summary>
    /// The <see cref="LayerMask"/> to use for checking line-of-sight.
    /// </summary>
    [Tooltip("The mask to use for checking line-of-sight.")]
    [SerializeField]
    private LayerMask mask;
    
    /// <summary>
    /// The vertical offset to use for checking line-of-sight.
    /// </summary>
    [Tooltip("The vertical offset to use for checking line-of-sight.")]
    [SerializeField]
    private float offset = 0.5f;
    
    /// <summary>
    /// The <see cref="NavMeshAgent"/> for controlling the movement of this enemy.
    /// </summary>
    [Tooltip("The NavMeshAgent for controlling the movement of this enemy.")]
    [HideInInspector]
    [SerializeField]
    private NavMeshAgent agent;
    
    /// <summary>
    /// The <see cref="Collider"/> hitting the <see cref="Level.Agent"/>.
    /// </summary>
    [Tooltip("The collider for hitting the player.")]
    [HideInInspector]
    [SerializeField]
    private Collider col;
    
    /// <summary>
    /// The <see cref="Level"/> this is a part of.
    /// </summary>
    [NonSerialized]
    public Level Instance;
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        GetComponents();
    }
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void Start()
    {
        GetComponents();
    }
    
    /// <summary>
    /// Get all needed components.
    /// </summary>
    private void GetComponents()
    {
        GetNavMeshAgent();
        GetCollider();
    }
    
    /// <summary>
    /// Get the <see cref="agent"/>.
    /// </summary>
    private void GetNavMeshAgent()
    {
        if (agent == null || agent.gameObject != gameObject)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }
    
    /// <summary>
    /// Get the <see cref="col"/>.
    /// </summary>
    private void GetCollider()
    {
        if (col == null || col.gameObject != gameObject)
        {
            col = GetComponent<Collider>();
        }
        
        if (col)
        {
            col.isTrigger = true;
        }
    }
    
    /// <summary>
    /// Frame-rate independent MonoBehaviour.FixedUpdate message for physics calculations.
    /// </summary>
    private void FixedUpdate()
    {
        Vector3 p = transform.position;
        Transform target = Instance.Agent.transform;
        Vector3 t = target.position;
        Vector2 p2 = new(p.x, p.z);
        Vector2 t2 = new(t.x, t.z);
        
        // If in range of the player and have line-of-sight, navigation to them.
        if (Vector2.Distance(p2, t2) <= range && (!Physics.Linecast(new(p.x, p.y + offset, p.z), new(t.x, t.y + offset, t.z), out RaycastHit hit, mask) || hit.transform == target))
        {
            agent.destination = t;
            return;
        }
        
        // Otherwise, if we don't have a path, choose a random position.
        if (!agent.hasPath)
        {
            agent.destination = Instance.RandomWalkable();
        }
    }
}