using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

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
    /// The penalty for movement cost.
    /// </summary>
    [Tooltip("The penalty for movement cost.")]
    [SerializeField]
    private float penalty = -0.0001f;
    
    /// <summary>
    /// The maximum number of steps allowed to be performed between goals before being considered a failure.
    /// </summary>
    [Tooltip("The maximum number of steps allowed to be performed between goals before being considered a failure.")]
    [SerializeField]
    private int maxSteps = 500;
    
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
    /// How long in seconds to time out the agent if they get stuck in the same spot.
    /// </summary>
    [Header("Timeout")]
    [Tooltip("How long in seconds to time out the agent if they get stuck in the same spot.")]
    [Min(float.Epsilon)]
    [SerializeField]
    private float seconds = 10f;
    
    /// <summary>
    /// The minimum distance to move to reset the timeout.
    /// </summary>
    [Tooltip("The minimum distance to move to reset the timeout.")]
    [Min(float.Epsilon)]
    [SerializeField]
    private float distance = 0.25f;
    
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
    /// Our previous relative position in the level.
    /// </summary>
    public Vector2 Previous { get; private set; }
    
    /// <summary>
    /// The previous relative position of the nearest enemy in the level.
    /// </summary>
    public Vector2 PreviousEnemy { get; private set; }
    
    /// <summary>
    /// A <see cref="DemonstrationRecorder"/> attached to this.
    /// </summary>
    private DemonstrationRecorder _recorder;
    
    /// <summary>
    /// The <see cref="Recording"/> instance for settings.
    /// </summary>
    private Recording _recording;
    
    /// <summary>
    /// A <see cref="Trainer"/> instance for settings.
    /// </summary>
    private Trainer _trainer;
    
    /// <summary>
    /// The last position the player was at.
    /// </summary>
    private Vector2 _lastPosition;
    
    /// <summary>
    /// The elapsed timeout time.
    /// </summary>
    private float _elapsed;
    
    /// <summary>
    /// The current step since accomplishing something.
    /// </summary>
    private int _step;
    
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
        
        // See if there is a trainer in the scene.
        _trainer = FindAnyObjectByType<Trainer>();
        if (_trainer)
        {
            return;
        }
        
        // See if we are recording in this scene.
        _recording = FindAnyObjectByType<Recording>(FindObjectsInactive.Include);
        if (!_recording)
        {
            return;
        }
        
        // Set to heuristic for recording.
        Parameters.BehaviorType = BehaviorType.HeuristicOnly;
        
        if (!TryGetComponent(out _recorder))
        {
            _recorder = gameObject.AddComponent<DemonstrationRecorder>();
        }
        
        _recorder.DemonstrationDirectory = RecorderPath;
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
        spec.NumContinuousActions = 2;
        if (spec.NumDiscreteActions != 0)
        {
            spec.BranchSizes = Array.Empty<int>();
        }
        
        Parameters.BrainParameters.ActionSpec = spec;
        Parameters.BrainParameters.VectorObservationSize = 10;
        Parameters.BrainParameters.NumStackedVectorObservations = 1;
        Parameters.UseChildSensors = true;
        Parameters.UseChildActuators = false;
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
        AddReward(penalty);
        RequestDecision();
        _velocity = _movement.normalized * speed;
        _velocity3 = new(_velocity.x, 0, _velocity.y);
        body.linearVelocity = _velocity3;
        
        // If this is not in inference mode, add timeouts to prevent any weird cases of getting stuck during demonstration generation or training.
        if (!Parameters.IsInHeuristicMode() && (!Academy.IsInitialized || !Academy.Instance.IsCommunicatorOn))
        {
            return;
        }
        
        // See if we have hit the maximum number of steps.
        if (++_step >= maxSteps)
        {
            CustomEndEpisode(true);
            return;
        }
        
        // Check our tile.
        Vector3 p = transform.position;
        Vector2 p2 = new(p.x, p.z);
        
        // If the positions are the same, step the time, stopping if we have been in the same spot for too long.
        if (Vector2.Distance(p2, _lastPosition) >= distance)
        {
            _lastPosition = p2;
            _elapsed = 0;
            return;
        }
        
        _elapsed += Time.fixedDeltaTime;
        if (_elapsed >= seconds)
        {
            CustomEndEpisode(true);
        }
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
    /// OnTriggerStay is called once per physics update for every Collider other that is touching the trigger. This function can be a coroutine.
    /// </summary>
    /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
    private void OnTriggerStay([NotNull] Collider other)
    {
        // The weapon pickup uses the "Respawn" tag.
        if (other.CompareTag("Respawn"))
        {
            // Get a reward for reaching the pickup.
            if (!_hasWeapon)
            {
                _hasWeapon = true;
                weapon?.SetActive(true);
                SetReward(1f);
                _step = 0;
            }
            
            // If there are no enemies in the level, end it.
            if (Instance.EnemiesCount < 1)
            {
                CustomEndEpisode(false);
            }
            
            return;
        }
        
        // The only other targets for us are enemies.
        if (!other.TryGetComponent(out Enemy enemy) || !Instance.EnemiesActive.Contains(enemy))
        {
            return;
        }
        
        // If we don't have the weapon, the agent lost, so end the episode.
        if (!_hasWeapon)
        {
            SetReward(-1f);
            CustomEndEpisode(true);
            return;
        }
        
        // Otherwise, eliminate the enemy.
        animator.Play(Attack);
        Instance.EliminateEnemy(enemy);
        SetReward(1f);
        _step = 0;
        
        // If this was the last enemy, end the episode.
        if (Instance.EnemiesCount < 1)
        {
            CustomEndEpisode(false);
        }
    }
    
    /// <summary>
    /// End an episode while accounting for any failure.
    /// </summary>
    /// <param name="failure"></param>
    public void CustomEndEpisode(bool failure)
    {
        if (failure)
        {
            // When we failed, get the distance to the next goal to give a partial reward based on how close we got.
            Vector3 self3 = transform.position;
            Vector2 self = new(self3.x, self3.z);
            Vector2 objective;
            if (_hasWeapon)
            {
                Enemy nearest = Instance.EnemiesActive.OrderBy(x =>
                {
                    Vector3 p = x.transform.position;
                    return Vector2.Distance(self, new(p.x, p.z));
                }).FirstOrDefault();
                if (nearest)
                {
                    Vector3 objective3 = nearest.transform.position;
                    objective = new(objective3.x, objective3.z);
                }
                else
                {
                    objective = self;
                }
            }
            else
            {
                Vector3 objective3 = Instance.Weapon.transform.position;
                objective = new(objective3.x, objective3.z);
            }
            
            // Get the points as relative coordinates to the level, with each axis being from [0, 1].
            self = Instance.PositionToPercentage(self);
            objective = Instance.PositionToPercentage(objective);
            
            // Add a relative reward based on how close the agent got to their next objective.
            // If in opposite extremes, such as the player being at [0, 0] and the next objective being at [1, 1], this is zero reward.
            // Otherwise, if somehow, they were right on top of each other, which should never happen as otherwise it wouldn't be a failure to begin with, but this situation would give a reward of one.
            // The maximum possible distance in a 1x1 grid is the diagonal length, being the square root of two.
            AddReward(Mathf.Clamp01(1f - Vector2.Distance(self, objective) / 1.4142135623730950488f));
            
            // Handle if recording.
            if (_recording)
            {
                // Stop the current recording.
                _recorder.Close();
                _recorder.Record = false;
                
                // If this was a failure, discard the recording.
                _recorder.enabled = false;
                string path = Path.Combine(_recorder.DemonstrationDirectory ?? RecorderPath, $"{_recorder.DemonstrationName}.demo");
                if (File.Exists(path))
                {
                    File.Delete(path);
                    path = $"{path}.meta";
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                
                _recorder.enabled = true;
            }
        }
        
        EndEpisode();
    }
    
    /// <summary>
    /// The default path for demonstration recording.
    /// </summary>
    private static string RecorderPath
    {
        get
        {
            string path = Path.GetDirectoryName(Application.dataPath);
            if (path == null)
            {
                return Application.dataPath;
            }
            
            path = Path.Combine(path, "Demonstrations");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }
    }
    
    /// <summary>
    /// Read sensor observations.
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations([NotNull] VectorSensor sensor)
    {
        // Get our relative position. We pass both the previous and current positions so the agent can tell which way it was moving.
        sensor.AddObservation(Previous);
        Previous = Instance.PositionToPercentage(transform.position);
        sensor.AddObservation(Previous);
        
        // To reduce the number of observations, use the weapon indication in two ways.
        // When we don't have the weapon, give the relative coordinates of the weapon pickup.
        // Otherwise, pass [-1, -1] when we do have the weapon.
        sensor.AddObservation(_hasWeapon ? new(-1f, -1f) : Instance.PositionToPercentage(Instance.Weapon.transform.position));
        
        // If we have eliminated all enemies, pass [-1, -1] to indicate this.
        // Since we keep track of previous enemy positions, do the same as with our position.
        if (Instance.EnemiesCount < 1)
        {
            sensor.AddObservation(new Vector2(-1f, -1f));
            sensor.AddObservation(new Vector2(-1f, -1f));
            return;
        }
        
        // Otherwise, pass the position of the nearest enemy and the previous position.
        sensor.AddObservation(PreviousEnemy);
        PreviousEnemy = NearestEnemy();
        sensor.AddObservation(PreviousEnemy);
    }
    
    /// <summary>
    /// Get the relative position of the nearest enemy.
    /// </summary>
    /// <returns>The relative position of the nearest enemy.</returns>
    private Vector2 NearestEnemy()
    {
        Vector3 p = transform.position;
        Vector2 p2 = new(p.x, p.z);
        return Instance.PositionToPercentage(Instance.EnemiesActive.Select(x =>
        {
            Vector3 t = x.transform.position;
            return new Vector2(t.x, t.z);
        }).OrderBy(x => Vector2.Distance(new(x.x, x.y), p2)).First());
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
        
        // If there are enemies, so if we have a weapon, navigate to the nearest one to eliminate them.
        if (_hasWeapon)
        {
            if (_recording)
            {
                Time.timeScale = _recording.AutoScale;
            }
            
            NavMesh.CalculatePath(p, Instance.EnemiesActive.Select(x => x.transform.position).OrderBy(x =>
            {
                Vector2 t2 = new(x.x, x.z);
                return Vector2.Distance(p2, t2);
            }).First(), NavMesh.AllAreas, path);
        }
        // Otherwise, we don't yet have the weapon, so navigate to the weapon pickup.
        else
        {
            if (_recording)
            {
                // Space can also be held to fast-forward.
                Time.timeScale = Keyboard.current.spaceKey.isPressed ? _recording.AutoScale : _recording.ManualScale;
            }
            
            // The keyboard can be used to manually override and avoid enemies that the automatic pathfinding may fail.
            bool up = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
            bool down = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
            bool right = Keyboard.current.dKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            bool left = Keyboard.current.aKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            if (up || down || right || left)
            {
                actionsOut.ContinuousActions.Array[0] = right ? left ? 0f : 1f : left ? -1f : 0f;
                actionsOut.ContinuousActions.Array[1] = up ? down ? 0f : 1f : down ? -1f : 0f;
                return;
            }
            
            // Or, the mouse can also be used to navigate to that point.
            bool mouse = false;
            if (Mouse.current.rightButton.isPressed)
            {
                Camera c = Camera.main;
                if (c)
                {
                    Ray ray = c.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (new Plane(Vector3.up, Instance.transform.position).Raycast(ray, out float d))
                    {
                        mouse = true;
                        NavMesh.CalculatePath(p, ray.GetPoint(d), NavMesh.AllAreas, path);
                    }
                }
            }
            
            // Lastly, automatically move to the weapons pickup if no manual overrides were added.
            if (!mouse)
            {
                NavMesh.CalculatePath(p, Instance.Weapon.transform.position, NavMesh.AllAreas, path);
            }
        }
        
        // The first position is just our current position, so get the second.
        int points = path.GetCornersNonAlloc(_pathHelper);
        Vector2 direction = points > 1 ? (new Vector2(_pathHelper[1].x, _pathHelper[1].z) - p2).normalized : Vector2.zero;
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
        
        // If recording, get the next parameters we should use.
        if (_recording)
        {
            _recorder.DemonstrationName = _recording.GetRecordingSettings(out int size, out float walls, out int enemies);
            
            // Skip ones we are already doing.
            while (File.Exists(Path.Combine(_recorder.DemonstrationDirectory ?? Path.Combine(Application.dataPath, "Demonstrations"), $"{_recorder.DemonstrationName}.demo")))
            {
                _recording.AdvanceSettings();
                _recorder.DemonstrationName = _recording.GetRecordingSettings(out size, out walls, out enemies);
            }
            
            Instance.Size = size;
            Instance.WallPercent = walls;
            Instance.DesiredEnemies = enemies;
            _recorder.Record = _recorder.DemonstrationName != null;
        }
        // Otherwise, create the level using any variable defined in the training.
        else
        {
            // See if we should bound values to a trainer.
            bool trainer = _trainer;
            int maxSize;
            float maxWalls;
            int maxEnemies;
            if (trainer)
            {
                maxSize = _trainer.MaxSize;
                maxWalls = _trainer.MaxWalls;
                maxEnemies = _trainer.MaxEnemies;
            }
            else
            {
                maxSize = Instance.Size;
                maxWalls = Instance.WallPercent;
                maxEnemies = Instance.DesiredEnemies;
            }
            
            int size = (int)_environment.GetWithDefault("size", maxSize);
            float walls = _environment.GetWithDefault("walls", maxWalls);
            int enemies = (int)_environment.GetWithDefault("enemies", maxEnemies);
            
            // If there is a trainer, ensure we keep randomizing level sizes, even down to smaller ones, to help the model generate.
            if (trainer)
            {
                Instance.Size = size <= _trainer.MinSize ? _trainer.MinSize : Mathf.Min(Random.Range(_trainer.MinSize, size + 1), maxSize);
                Instance.WallPercent = Mathf.Min(Random.Range(0f, walls), maxWalls);
                Instance.DesiredEnemies = Mathf.Min(Random.Range(0, enemies + 1), maxEnemies);
            }
            else
            {
                Instance.Size = size;
                Instance.WallPercent = walls;
                Instance.DesiredEnemies = enemies;
            }
        }
        
        Instance.CreateLevel();
        
        // Now that the player is spawned, cache the relative position and enemy position.
        Vector3 p = transform.position;
        Previous = Instance.PositionToPercentage(p);
        PreviousEnemy = Instance.EnemiesCount < 1 ? new(-1f, -1f) : NearestEnemy();
        
        // Reset timeout values.
        _elapsed = 0;
        _lastPosition = new(p.x, p.z);
        _step = 0;
        
        // Set the collider back to regular.
        col.isTrigger = false;
    }
}