using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// The player agent itself.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
public class Player : Agent
{
    /// <summary>
    /// How fast this agent can move.
    /// </summary>
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
    /// The <see cref="Rigidbody"/> for controlling the movement of this agent.
    /// </summary>
    [Tooltip("The rigidbody for controlling the movement of this agent.")]
    [HideInInspector]
    [SerializeField]
    private Rigidbody body;
    
    /// <summary>
    /// The <see cref="BehaviorParameters"/> for the decision-making of this agent.
    /// </summary>
    [Tooltip("The parameters for the decision-making of this agent.")]
    [HideInInspector]
    [SerializeField]
    private BehaviorParameters parameters;
    
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
        GetParameters();
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
    /// Get the <see cref="parameters"/>.
    /// </summary>
    private void GetParameters()
    {
        if (parameters == null || parameters.gameObject != gameObject)
        {
            parameters = GetComponent<BehaviorParameters>();
        }
    
        if (!parameters)
        {
            return;
        }
        
        ActionSpec spec = parameters.BrainParameters.ActionSpec;
        if (spec.NumContinuousActions != 2)
        {
            spec.NumContinuousActions = 2;
        }
        if (spec.NumDiscreteActions != 0)
        {
            spec.BranchSizes = Array.Empty<int>();
        }
        
        parameters.BrainParameters.ActionSpec = spec;
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
        actionsOut.ContinuousActions.Array[0] = Keyboard.current.dKey.isPressed ? Keyboard.current.aKey.isPressed ? 0f : 1f : Keyboard.current.aKey.isPressed ? -1f : 0f;
        actionsOut.ContinuousActions.Array[1] = Keyboard.current.wKey.isPressed ? Keyboard.current.sKey.isPressed ? 0f : 1f : Keyboard.current.sKey.isPressed ? -1f : 0f;
    }
}