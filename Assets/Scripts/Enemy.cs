using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handle enemy logic.
/// </summary>
[AddComponentMenu("ML-Dungeon/Enemy")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour
{
    /// <summary>
    /// Efficient <see cref="animator"/> cache for the speed variable.
    /// </summary>
    private static readonly int Speed = Animator.StringToHash("Speed");
    
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
    private float offset = 0.25f;
    
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
    /// The <see cref="Animator"/> for the agent.
    /// </summary>
    [Tooltip("The animator for the agent.")]
    [HideInInspector]
    [SerializeField]
    private Animator animator;
    
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
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    private void OnEnable()
    {
        animator.SetFloat(Speed, 0);
    }
    
    /// <summary>
    /// Get all needed components.
    /// </summary>
    private void GetComponents()
    {
        GetNavMeshAgent();
        GetCollider();
        GetAnimator();
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
    /// Get the <see cref="animator"/>.
    /// </summary>
    private void GetAnimator()
    {
        if (animator == null || animator.gameObject != gameObject)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator)
        {
            animator.applyRootMotion = false;
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
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        animator.SetFloat(Speed, new Vector2(agent.velocity.x, agent.velocity.z).magnitude / agent.speed);
    }
}