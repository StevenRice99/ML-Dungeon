using UnityEngine;

/// <summary>
/// Configure a <see cref="Camera"/> for a <see cref="Trainer"/>.
/// </summary>
[AddComponentMenu("ML-Dungeon/Camera - Trainer")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraTrainer : CameraHandler
{
    /// <summary>
    /// Configure the <see cref="CameraHandler.Cam"/>.
    /// </summary>
    protected override void SetupCamera()
    {
        Trainer trainer = FindAnyObjectByType<Trainer>();
        if (!trainer)
        {
            return;
        }
        
        transform.position = trainer.transform.position + new Vector3(0f, Height, 0f);
        Cam.orthographicSize = (trainer.MaxSize + 2) * trainer.LevelPrefab.PieceSpacing * Mathf.CeilToInt(Mathf.Sqrt(trainer.Levels)) / 2f;
    }
}