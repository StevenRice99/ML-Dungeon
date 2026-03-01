using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Read local environment as a visual grid.
/// </summary>
[AddComponentMenu("ML-Dungeon/Dungeon Sensor")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class DungeonSensor : SensorComponent, ISensor
{
    /// <summary>
    /// The unique name to use for sensor collection.
    /// </summary>
    [Tooltip("The unique name to use for sensor collection.")]
    [SerializeField]
    private string identifier = "DungeonSensor";
    
    /// <summary>
    /// The <see cref="Player"/> this is attached to.
    /// </summary>
    [HideInInspector]
    [Tooltip("The player this is attached to.")]
    [SerializeField]
    private Player player;
    
    /// <summary>
    /// The last sensed data. Updated to a 3D array for 3-channel one-hot encoding.
    /// </summary>
    public float[,,] Sensed { get; private set; }
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        GetPlayer();
    }
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    private void Start()
    {
        GetPlayer();
    }
    
    /// <summary>
    /// Get the <see cref="player"/>.
    /// </summary>
    private void GetPlayer()
    {
        if (player == null || player.gameObject != gameObject)
        {
            player = GetComponent<Player>();
        }
    }
    
    /// <summary>
    /// The number of steps in each direction to collect for the sensor.
    /// </summary>
    [Tooltip("The number of steps in each direction to collect for the sensor...")]
    [SerializeField]
    private int size = 10;
    
    /// <summary>
    /// Create the sensors, being just this.
    /// </summary>
    public override ISensor[] CreateSensors() => new ISensor[] { this };
    
    /// <summary>
    /// Get the size of this visual sensor.
    /// </summary>
    public ObservationSpec GetObservationSpec()
    {
        int dimension = size * 2 + 1;
        return ObservationSpec.Visual(3, dimension, dimension);
    }
    
    /// <summary>
    /// Indicate that this sensor cannot be compressed.
    /// </summary>
    public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
    
    /// <summary>
    /// Give the compressed version which is nothing for this sensor.
    /// </summary>
    public byte[] GetCompressedObservation() => null;
    
    /// <summary>
    /// Get the unique name to use for sensor collection.
    /// </summary>
    public string GetName() => identifier;
    
    /// <summary>
    /// Write the sensor data.
    /// </summary>
    public int Write(ObservationWriter writer)
    {
        Sensed = player.Instance.SensorMap(size);
        
        int width = Sensed.GetLength(0);
        int height = Sensed.GetLength(1);
        int channels = Sensed.GetLength(2);
        
        int total = 0;
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < channels; k++)
                {
                    // Write directly via 3D index so ML-Agents maps it properly
                    writer[i, j, k] = Sensed[i, j, k];
                    total++;
                }
            }
        }
        
        return total;
    }
    
    /// <summary>
    /// Update any internal state of the sensor.
    /// </summary>
    public void Update() { }
    
    /// <summary>
    /// Resets the internal state of the sensor.
    /// </summary>
    public void Reset() { }
}