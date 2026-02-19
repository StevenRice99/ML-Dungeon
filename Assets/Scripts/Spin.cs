using UnityEngine;

/// <summary>
/// Allows for spinning an object around its Y-axis at a given <see cref="speed"/>.
/// </summary>
[AddComponentMenu("ML-Dungeon/Spin")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
public class Spin : MonoBehaviour
{
    /// <summary>
    /// The rate to spin this at.
    /// </summary>
    [Tooltip("The rate to spin this at.")]
    [SerializeField]
    private float speed = 360f;
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        Transform t = transform;
        t.localEulerAngles = new(0, t.localEulerAngles.y + speed * Time.deltaTime, 0);
    }
}