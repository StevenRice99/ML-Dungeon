using Unity.MLAgents;
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
        const float w = 150;
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
        
        // Show stats on the right.
        float xr = Screen.width - x - w;
        y = x;
        GUI.Label(new(xr, y, w, h), $"Step: {_level.Agent.StepCount}");
        y += h + x;
        GUI.Label(new(xr, y, w, h), $"Score: {_level.Agent.GetCumulativeReward()}");
    }
}