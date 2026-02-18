using UnityEngine;

/// <summary>
/// Allow for easily spinning an object.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class Spin : MonoBehaviour
{
    /// <summary>
    /// The rate to spin this at.
    /// </summary>
    [Tooltip("The rate to spin this at.")]
    [SerializeField]
    private float rotation = 360f;
    
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        Transform t = transform;
        t.localEulerAngles = new(0, t.localEulerAngles.y + rotation * Time.deltaTime, 0);
    }
}