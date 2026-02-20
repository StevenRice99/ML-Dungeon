using System;
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
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        GetPlayer();
    }
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
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
    /// This will create a sensor of "(size * 2) + 1".
    /// In the "trainer_config.yaml" under the "network_settings", the "vis_encode_type" must be set to a model which can take these inputs. Their minimum sizes are below:"<br/>
    /// match3 - 5x5 - This must be at least 3 to use this.<br/>
    /// resnet - 15x15 - This must be at least 7 to use this.<br/>
    /// simple - 20x20 - This must be at least 10 to use this.<br/>
    /// nature_cnn - 36x36 - This must be at least 18 to use this.<br/>
    /// </summary>
    [Tooltip("The number of steps in each direction to collect for the sensor. " +
             "This will create a sensor of \"(size * 2) + 1\". " +
             "In the \"trainer_config.yaml\" under the \"network_settings\", the \"vis_encode_type\" must be set to a model which can take these inputs. Their minimum sizes are below:\n" +
             "match3 - 5x5 - This must be at least 3 to use this.\n" +
             "resnet - 15x15 - This must be at least 7 to use this.\n" +
             "simple - 20x20 - This must be at least 10 to use this.\n" +
             "nature_cnn - 36x36 - This must be at least 18 to use this.")]
    [SerializeField]
    private int size = 7;
    
    /// <summary>
    /// Create the sensors, being just this.
    /// </summary>
    /// <returns>This sensor.</returns>
    public override ISensor[] CreateSensors() => new ISensor[] { this };
    
    /// <summary>
    /// Get the size of this visual sensor.
    /// </summary>
    /// <returns></returns>
    public ObservationSpec GetObservationSpec() => ObservationSpec.Visual(1, size, size);
    
    /// <summary>
    /// Indicate that this sensor cannot be compressed.
    /// </summary>
    /// <returns>The default specification which states this cannot be compressed.</returns>
    public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
    
    /// <summary>
    /// Give the compressed version which is nothing for this sensor.
    /// </summary>
    /// <returns>NULL.</returns>
    public byte[] GetCompressedObservation() => null;
    
    /// <summary>
    /// Get the unique name to use for sensor collection.
    /// </summary>
    /// <returns>The unique name to use for sensor collection.</returns>
    public string GetName() => identifier;
    
    /// <summary>
    /// Write the sensor data.
    /// </summary>
    /// <param name="writer">The observations to write to.</param>
    /// <returns>The number of points written.</returns>
    public int Write(ObservationWriter writer)
    {
        float[,] index = player.Instance.SensorMap(size);
        int a = index.GetLength(0);
        int b = index.GetLength(1);
        int total = 0;
        for (int i = 0; i < a; i++)
        {
            for (int j = 0; j < b; j++)
            {
                writer[total++] = index[i, j];
            }
        }
        
        return a * b;
    }
    
    /// <summary>
    /// Update any internal state of the sensor. This is called once per each agent step.
    /// </summary>
    public void Update() { }
    
    /// <summary>
    /// Resets the internal state of the sensor. This is called at the end of an Agent's episode. Most implementations can leave this empty.
    /// </summary>
    public void Reset() { }
}