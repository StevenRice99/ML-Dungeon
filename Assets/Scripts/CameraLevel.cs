using UnityEngine;

/// <summary>
/// Handle the <see cref="Camera"/> for a scene.
/// </summary>
[AddComponentMenu("ML-Dungeon/Camera Handler")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraHandler : MonoBehaviour
{
    /// <summary>
    /// The <see cref="Camera"/>.
    /// </summary>
    [HideInInspector]
    [Tooltip("The camera.")]
    [SerializeField]
    private Camera cam;
    
    /// <summary>
    /// The vertical height to place the <see cref="cam"/> at.
    /// </summary>
    [Tooltip("The vertical height to place the camera at.")]
    [SerializeField]
    private float height = 10f;
    
    /// <summary>
    /// Extra distance to extend the <see cref="cam"/>.
    /// </summary>
    [Tooltip("Extra distance to extend the camera.")]
    [SerializeField]
    private float extra = 10f;
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        GetCamera();
    }
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
    /// </summary>
    private void Start()
    {
        GetCamera();
    }
    
    /// <summary>
    /// Get the <see cref="cam"/>.
    /// </summary>
    private void GetCamera()
    {
        if (cam == null || cam.gameObject != gameObject)
        {
            cam = GetComponent<Camera>();
        }
        
        if (!cam)
        {
            return;
        }
        
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new(0.2784314f, 0.2784314f, 0.2784314f, 1f);
        cam.orthographic = true;
        cam.nearClipPlane = 0f;
        cam.farClipPlane = height + extra;
        cam.rect = new(0f, 0f, 1f, 1f);
        cam.depth = -1f;
        cam.renderingPath = RenderingPath.Forward;
        cam.targetTexture = null;
        cam.useOcclusionCulling = false;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.allowDynamicResolution = false;
        cam.stereoSeparation = 0f;
        cam.stereoConvergence = height;
        cam.targetDisplay = 0;
    }
}