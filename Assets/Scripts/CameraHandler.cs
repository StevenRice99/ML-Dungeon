using UnityEngine;

/// <summary>
/// Handle the <see cref="Camera"/> for a scene.
/// </summary>
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public abstract class CameraHandler : MonoBehaviour
{
    /// <summary>
    /// The <see cref="Camera"/>.
    /// </summary>
    [field: HideInInspector]
    [field: Tooltip("The camera.")]
    [field: SerializeField]
    protected Camera Cam { get; private set; }
    
    /// <summary>
    /// The vertical height to place the <see cref="Cam"/> at.
    /// </summary>
    [field: Tooltip("The vertical height to place the camera at.")]
    [field: SerializeField]
    protected float Height { get; private set; } = 10f;
    
    /// <summary>
    /// Extra distance to extend the <see cref="Cam"/>.
    /// </summary>
    [Tooltip("Extra distance to extend the camera.")]
    [SerializeField]
    private float extra = 1000f;
    
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
        SetupCamera();
    }
    
    /// <summary>
    /// Get the <see cref="Cam"/>.
    /// </summary>
    private void GetCamera()
    {
        if (Cam == null || Cam.gameObject != gameObject)
        {
            Cam = GetComponent<Camera>();
        }
        
        if (!Cam)
        {
            return;
        }
        
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = new(0.2784314f, 0.2784314f, 0.2784314f, 1f);
        Cam.orthographic = true;
        Cam.nearClipPlane = 0f;
        Cam.farClipPlane = Height + extra;
        Cam.rect = new(0f, 0f, 1f, 1f);
        Cam.depth = -1f;
        Cam.renderingPath = RenderingPath.Forward;
        Cam.targetTexture = null;
        Cam.useOcclusionCulling = false;
        Cam.allowHDR = false;
        Cam.allowMSAA = false;
        Cam.allowDynamicResolution = false;
        Cam.stereoSeparation = 0f;
        Cam.stereoConvergence = Height;
        Cam.targetDisplay = 0;
    }
    
    /// <summary>
    /// Configure the <see cref="Cam"/>.
    /// </summary>
    protected abstract void SetupCamera();
}