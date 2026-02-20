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
    /// Efficient <see cref="animator"/> cache for the walk state.
    /// </summary>
    private static readonly int Walk = Animator.StringToHash("Walk");
    
    /// <summary>
    /// Efficient <see cref="animator"/> cache for the eliminated state.
    /// </summary>
    private static readonly int Final = Animator.StringToHash("Eliminate");
    
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
    [field: Tooltip("The NavMeshAgent for controlling the movement of this enemy.")]
    [field: HideInInspector]
    [field: SerializeField]
    public NavMeshAgent Agent { get; private set; }
    
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
    /// Track if this enemy is eliminated.
    /// </summary>
    private bool _eliminated;
    
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
        col.enabled = true;
        _eliminated = false;
        Agent.ResetPath();
        animator.ResetControllerState();
        animator.SetFloat(Speed, 0);
        animator.Play(Walk);
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
    /// Get the <see cref="Agent"/>.
    /// </summary>
    private void GetNavMeshAgent()
    {
        if (Agent == null || Agent.gameObject != gameObject)
        {
            Agent = GetComponent<NavMeshAgent>();
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
        
        if (!animator)
        {
            return;
        }
        
        animator.applyRootMotion = false;
        animator.writeDefaultValuesOnDisable = true;
    }
    
    /// <summary>
    /// Eliminate this enemy.
    /// </summary>
    public void Eliminate()
    {
        if (_eliminated)
        {
            return;
        }
        
        col.enabled = false;
        _eliminated = true;
        Agent.ResetPath();
        animator.Play(Final);
    }
    
    /// <summary>
    /// Frame-rate independent MonoBehaviour.FixedUpdate message for physics calculations.
    /// </summary>
    private void FixedUpdate()
    {
        if (_eliminated)
        {
            return;
        }
        
        Vector3 p = transform.position;
        Transform target = Instance.Agent.transform;
        Vector3 t = target.position;
        Vector2 p2 = new(p.x, p.z);
        Vector2 t2 = new(t.x, t.z);
        
        // If in range of the player and have line-of-sight, navigation to them.
        if (Vector2.Distance(p2, t2) <= range && (!Physics.Linecast(new(p.x, p.y + offset, p.z), new(t.x, t.y + offset, t.z), out RaycastHit hit, mask) || hit.transform == target))
        {
            Agent.destination = t;
            return;
        }
        
        // Otherwise, if we don't have a path, choose a random position.
        if (!Agent.hasPath)
        {
            Agent.destination = Instance.RandomWalkable();
        }
    }
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        if (!_eliminated)
        {
            animator.SetFloat(Speed, new Vector2(Agent.velocity.x, Agent.velocity.z).magnitude / Agent.speed);
        }
    }
}