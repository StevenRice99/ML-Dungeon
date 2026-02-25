using System.Linq;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

/// <summary>
/// Configure a <see cref="Camera"/> for an individual <see cref="Level"/>.
/// </summary>
[AddComponentMenu("ML-Dungeon/Camera - Level")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraLevel : CameraHandler
{
    /// <summary>
    /// The models we can switch between.
    /// </summary>
    [Tooltip("The models we can switch between.")]
    [SerializeField]
    private ModelAsset[] models;
    
    /// <summary>
    /// The <see cref="Level"/>.
    /// </summary>
    private Level _level;
    
    /// <summary>
    /// Allow for checking if we are recording.
    /// </summary>
    private Recording _recording;
    
    /// <summary>
    /// Configure the <see cref="CameraHandler.Cam"/>.
    /// </summary>
    protected override void SetupCamera()
    {
        _level = FindAnyObjectByType<Level>();
        if (_level)
        {
            transform.position = _level.transform.position + new Vector3(0f, Height, 0f);
        }
        
        _recording = FindAnyObjectByType<Recording>(FindObjectsInactive.Include);
        
        // Ensure no NULL models.
        models = models.Where(x => x != null).ToArray();
    }
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        // Ensure we can view the entire level.
        if (_level)
        {
            Cam.orthographicSize = (_level.Size + 2) * _level.PieceSpacing / 2f;
        }
    }
    
    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// </summary>
    private void OnGUI()
    {
        const float x = 10;
        const float w = 175;
        const float h = 25;
        
        // If we are recording, add a discard button in case an error occurs.
        if (_recording)
        {
            if (GUI.Button(new(x, x, w, h), "Discard"))
            {
                _level.Agent.CustomEndEpisode(true);
            }
            
            return;
        }
        
        // If we are actively training, don't display the controls.
        if (Academy.Instance.IsCommunicatorOn)
        {
            return;
        }
        
        float y = x;
        if (GUI.Button(new(x, y, w, h), "Reset"))
        {
            _level.Agent.EndEpisode();
        }
        
        y += h + x;
        if (GUI.Button(new(x, y, w, h), "Increase Size"))
        {
            _level.Size++;
            _level.Agent.EndEpisode();
        }
        
        if (_level.Size > 2)
        {
            y += h + x;
            if (GUI.Button(new(x, y, w, h), "Decrease Size"))
            {
                _level.Size--;
                _level.Agent.EndEpisode();
            }
        }
        
        y += h + x;
        if (GUI.Button(new(x, y, w, h), "Increase Enemies"))
        {
            _level.DesiredEnemies++;
            _level.Agent.EndEpisode();
        }
        
        if (_level.DesiredEnemies > 0)
        {
            y += h + x;
            if (GUI.Button(new(x, y, w, h), "Decrease Enemies"))
            {
                _level.DesiredEnemies--;
                _level.Agent.EndEpisode();
            }
        }
        
        y += h + x;
        GUI.Label(new(x, y, w, h), "Wall Percentage");
        
        y += h + x;
        float original = _level.WallPercent;
        float updated = GUI.HorizontalSlider(new(x, y, w, h), original, 0f, 1f);
        if (!Mathf.Approximately(original, updated))
        {
            _level.WallPercent = updated;
            _level.Agent.EndEpisode();
        }
        
        // Reset the coordinates for the right side of the screen.
        float xr = Screen.width - x - w;
        y = x;
        
        // Allow us to choose the execution mode if any models.
        if (models.Length > 0 && (!Academy.IsInitialized || !Academy.Instance.IsCommunicatorOn))
        {
            // Handle based on if we are currently using a model or not.
            bool heuristic = _level.Agent.Parameters.IsInHeuristicMode();
            if (heuristic)
            {
                // Indicate we are in heuristic mode.
                GUI.Label(new(xr, y, w, h), "Heuristic");
                y += h + x;
                
                // Give options to switch to all the other models.
                foreach (ModelAsset model in models)
                {
                    if (GUI.Button(new(xr, y, w, h), model.name))
                    {
                        _level.Agent.Parameters.Model = model;
                        _level.Agent.Parameters.BehaviorType = BehaviorType.InferenceOnly;
                    }
                    
                    y += h + x;
                }
            }
            else
            {
                // Display the name of the current model.
                ModelAsset model = _level.Agent.Parameters.Model;
                if (model != null)
                {
                    GUI.Label(new(xr, y, w, h), model.name);
                    y += h + x;
                }
                
                // Display all other models which can be switched to.
                foreach (ModelAsset other in models)
                {
                    if (model == other)
                    {
                        continue;
                    }
                    
                    if (GUI.Button(new(xr, y, w, h), other.name))
                    {
                        _level.Agent.Parameters.Model = other;
                        _level.Agent.Parameters.BehaviorType = BehaviorType.InferenceOnly;
                    }
                    
                    y += h + x;
                }
                
                // Display the option to switch to heuristic mode.
                if (GUI.Button(new(xr, y, w, h), "Heuristic"))
                {
                    _level.Agent.Parameters.Model = null;
                    _level.Agent.Parameters.BehaviorType = BehaviorType.HeuristicOnly;
                }
                
                y += h + x;
            }
        }
        
        // Show stats
        GUI.Label(new(xr, y, w, h), $"Step: {_level.Agent.StepCount}");
        y += h + x;
        GUI.Label(new(xr, y, w, h), $"Score: {_level.Agent.GetCumulativeReward()}");
        y += h + x;
        GUI.Label(new(xr, y, w, h), $"Position: {_level.Agent.Previous}");
        y += h + x;
        GUI.Label(new(xr, y, w, h), $"Enemy: {_level.Agent.PreviousEnemy}");
    }
}