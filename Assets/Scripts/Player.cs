using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// The player agent itself.
/// </summary>
[AddComponentMenu("ML-Dungeon/Player")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
[RequireComponent(typeof(Animator))]
public class Player : Agent
{
    /// <summary>
    /// Efficient <see cref="animator"/> cache for the speed variable.
    /// </summary>
    private static readonly int Speed = Animator.StringToHash("Speed");
    
    /// <summary>
    /// Efficient <see cref="animator"/> cache for the attack state.
    /// </summary>
    private static readonly int Attack = Animator.StringToHash("Attack");
    
    /// <summary>
    /// How fast this agent can move.
    /// </summary>
    [Header("Configuration")]
    [Tooltip("How fast this agent can move.")]
    [Min(float.Epsilon)]
    [SerializeField]
    private float speed = 1f;
    
    /// <summary>
    /// The degrees-per-second which the agent visually rotates.
    /// </summary>
    [Tooltip("The degrees-per-second which the agent visually rotates.")]
    [Min(float.Epsilon)]
    [SerializeField]
    private float rotation = 360f;
    
    /// <summary>
    /// The weapon visual.
    /// </summary>
    [Header("Components")]
    [Tooltip("The weapon visual.")]
    [SerializeField]
    private GameObject weapon;
    
    /// <summary>
    /// The <see cref="Rigidbody"/> for controlling the movement of this agent.
    /// </summary>
    [Tooltip("The rigidbody for controlling the movement of this agent.")]
    [HideInInspector]
    [SerializeField]
    public Rigidbody body;
    
    /// <summary>
    /// The <see cref="Collider"/> hitting objects.
    /// </summary>
    [Tooltip("The collider for hitting objects.")]
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
    /// The <see cref="BehaviorParameters"/> for the decision-making of this agent.
    /// </summary>
    [field: Tooltip("The parameters for the decision-making of this agent.")]
    [field: HideInInspector]
    [field: SerializeField]
    public BehaviorParameters Parameters { get; private set; }
    
    /// <summary>
    /// The reward for winning the level.
    /// </summary>
    [Header("Rewards")]
    [Tooltip("The reward for winning the level.")]
    [SerializeField]
    private float win = 1f;
    
    /// <summary>
    /// The reward for eliminating an enemy.
    /// </summary>
    [Tooltip("The reward for eliminating an enemy.")]
    [SerializeField]
    private float eliminate = 1f;
    
    /// <summary>
    /// The penalty given every tick.
    /// </summary>
    [Tooltip("The penalty given every tick.")]
    [SerializeField]
    private float penalty = -0.01f;
    
    /// <summary>
    /// The penalty to lose when we try to fight an enemy without the weapon.
    /// </summary>
    [Tooltip("The penalty to lose when we try to fight an enemy without the weapon.")]
    [SerializeField]
    private float lose = -1;
    
    /// <summary>
    /// The <see cref="Level"/> this is a part of.
    /// </summary>
    [NonSerialized]
    public Level Instance;
    
    /// <summary>
    /// The movement input.
    /// </summary>
    private Vector2 _movement = Vector2.zero;
    
    /// <summary>
    /// The current velocity.
    /// </summary>
    private Vector2 _velocity;
    
    /// <summary>
    /// The current velocity.
    /// </summary>
    private Vector3 _velocity3;
    
    /// <summary>
    /// If we currently have the weapon.
    /// </summary>
    private bool _hasWeapon;
    
    /// <summary>
    /// The <see cref="Academy"/> learning environment parameters.
    /// </summary>
    private EnvironmentParameters _environment;
    
    /// <summary>
    /// Help reduce garbage with getting where to move.
    /// </summary>
    private readonly Vector3[] _pathHelper = new Vector3[2];
    
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
        GetRigidbody();
        GetCollider();
        GetParameters();
        GetAnimator();
    }
    
    /// <summary>
    /// Get the <see cref="body"/>.
    /// </summary>
    private void GetRigidbody()
    {
        if (body == null || body.gameObject != gameObject)
        {
            body = GetComponent<Rigidbody>();
        }
        
        if (!body || !Application.isPlaying)
        {
            return;
        }
        
        // Configure the parameters.
        body.isKinematic = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.centerOfMass = Vector3.zero;
        body.maxAngularVelocity = 0;
        body.maxLinearVelocity = speed;
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
        
        if (!Application.isPlaying && col)
        {
            col.isTrigger = true;
        }
    }
    
    /// <summary>
    /// Get the <see cref="Parameters"/>.
    /// </summary>
    private void GetParameters()
    {
        if (Parameters == null || Parameters.gameObject != gameObject)
        {
            Parameters = GetComponent<BehaviorParameters>();
        }
    
        if (!Parameters)
        {
            return;
        }
        
        ActionSpec spec = Parameters.BrainParameters.ActionSpec;
        if (spec.NumContinuousActions != 2)
        {
            spec.NumContinuousActions = 2;
        }
        if (spec.NumDiscreteActions != 0)
        {
            spec.BranchSizes = Array.Empty<int>();
        }
        
        Parameters.BrainParameters.ActionSpec = spec;
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
        RequestDecision();
        _velocity = _movement.normalized * speed;
        _velocity3 = new(_velocity.x, 0, _velocity.y);
        body.linearVelocity = _velocity3;
        
        // Always tick down penalties to learn to complete levels quickly.
        AddReward(penalty);
    }
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        // Gradually rotate from the current rotation to the target rotation.
        if (_velocity != Vector2.zero)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_velocity3), rotation * Time.deltaTime);
        }
        
        animator.SetFloat(Speed, _velocity.magnitude / speed);
    }
    
    /// <summary>
    /// When a GameObject collides with another GameObject, Unity calls OnTriggerEnter. This function can be a coroutine.
    /// </summary>
    /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        HandleTriggers(other);
    }
    
    /// <summary>
    /// OnTriggerStay is called once per physics update for every Collider other that is touching the trigger. This function can be a coroutine.
    /// </summary>
    /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
    private void OnTriggerStay(Collider other)
    {
        HandleTriggers(other);
    }
    
    /// <summary>
    /// Handle active triggers.
    /// </summary>
    /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
    private void HandleTriggers(Collider other)
    {
        // The weapon pickup uses the "Respawn" tag.
        if (other.CompareTag("Respawn"))
        {
            _hasWeapon = true;
            weapon?.SetActive(true);
            return;
        }
        
        // The end-level coin uses the "Finish" tag. There must be no enemies left to finish the level.
        if (Instance.EnemiesCount < 1 && other.CompareTag("Finish"))
        {
            AddReward(win);
            EndEpisode();
            return;
        }
        
        // The only other targets for us are enemies.
        if (!other.TryGetComponent(out Enemy enemy))
        {
            return;
        }
        
        // If we have the weapon, eliminate enemies.
        if (_hasWeapon)
        {
            animator.Play(Attack);
            Instance.EliminateEnemy(enemy);
            AddReward(eliminate);
            return;
        }
        
        // Otherwise, give the losing penalty and end the episode.
        AddReward(lose);
        EndEpisode();
    }
    
    /// <summary>
    /// Read actions to control the player.
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        _movement.x = actions.ContinuousActions.Array[0];
        _movement.y = actions.ContinuousActions.Array[1];
    }
    
    /// <summary>
    /// Manual keyboard controls.
    /// </summary>
    /// <param name="actionsOut">The keyboard actions we are performing.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Otherwise, automatically find a path.
        Vector3 p = transform.position;
        Vector2 p2 = new(p.x, p.z);
        NavMeshPath path = new();
        if (Instance.EnemiesCount < 1)
        {
            NavMesh.CalculatePath(p, Instance.End.transform.position, NavMesh.AllAreas, path);
        }
        else if (_hasWeapon)
        {
            NavMesh.CalculatePath(p, Instance.EnemiesActive.Select(x => x.transform.position).OrderBy(x =>
            {
                Vector2 t2 = new(x.x, x.z);
                return Vector2.Distance(p2, t2);
            }).First(), NavMesh.AllAreas, path);
        }
        else
        {
            // The keyboard can be used to manually override and avoid enemies. Space can also be held to act as a manual override but not have any impact on calculations.
            bool up = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
            bool down = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
            bool right = Keyboard.current.dKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            bool left = Keyboard.current.aKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            if (up || down || right || left || Keyboard.current.spaceKey.isPressed)
            {
                actionsOut.ContinuousActions.Array[0] = right ? left ? 0f : 1f : left ? -1f : 0f;
                actionsOut.ContinuousActions.Array[1] = up ? down ? 0f : 1f : down ? -1f : 0f;
                return;
            }
            
            // Or, the mouse can be used to navigate to that point.
            bool mouse = false;
            if (Mouse.current.rightButton.isPressed)
            {
                Camera c = Camera.main;
                if (c)
                {
                    Ray ray = c.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (new Plane(Vector3.up, Instance.transform.position).Raycast(ray, out float distance))
                    {
                        mouse = true;
                        NavMesh.CalculatePath(p, ray.GetPoint(distance), NavMesh.AllAreas, path);
                    }
                }
            }
            
            // Lastly, automatically move to the weapons pickup.
            if (!mouse)
            {
                NavMesh.CalculatePath(p, Instance.Weapon.transform.position, NavMesh.AllAreas, path);
            }
        }
        
        // The first position is just our current position, so get the second.
        Vector2 direction = path.GetCornersNonAlloc(_pathHelper) > 1 ? (new Vector2(_pathHelper[1].x, _pathHelper[1].z) - p2).normalized : Vector2.zero;
        actionsOut.ContinuousActions.Array[0] = direction.x;
        actionsOut.ContinuousActions.Array[1] = direction.y;
    }
    
    /// <summary>
    /// Called the first time the agent is created.
    /// </summary>
    public override void Initialize()
    {
        _environment = Academy.Instance.EnvironmentParameters;
    }

    /// <summary>
    /// Called to reset the world.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // To start, ensure the collider is a trigger to not cause any detections as the level is being generated.
        col.isTrigger = true;
        _hasWeapon = false;
        weapon?.SetActive(false);
        
        // Reset animations.
        animator.SetFloat(Speed, 0);
        
        // Create the level using any variable defined in the training.
        Instance.Size = (int)_environment.GetWithDefault("size", Instance.Size);
        Instance.WallPercent = _environment.GetWithDefault("walls", Instance.WallPercent);
        Instance.DesiredEnemies = (int)_environment.GetWithDefault("enemies", Instance.DesiredEnemies);
        Instance.CreateLevel();
        
        // Now that the player is spawned, set back to a regular collider.
        col.isTrigger = false;
    }
}