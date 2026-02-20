#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(DungeonSensor), true)]
public class DungeonSensorInspector : Editor
{
    /// <summary>
    /// Implement this method to make a custom UIElements inspector.
    /// </summary>
    /// <returns>The custom UIElements inspector.</returns>
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new();
        
        // Create the default inspector.
        InspectorElement.FillDefaultInspector(root, serializedObject, this);
        
        // Create a container for the visualization of the sensor.
        VisualElement visualization = new() { style = { marginTop = 10 } };
        root.Add(visualization);
            
        // Schedule an update task on the container at roughly 60 FPS.
        visualization.schedule.Execute(() =>
        {
            // Clear the container first.
            visualization.Clear();
            
            // Nothing to do if not playing or no sensor data.
            if (!Application.isPlaying || target is not DungeonSensor sensor || sensor.Sensed == null)
            {
                return;
            }
            
            // Nothing to do if the data is empty.
            int a = sensor.Sensed.GetLength(0);
            if (a < 1)
            {
                return;
            }
            
            int b = sensor.Sensed.GetLength(1);
            if (b < 1)
            {
                return;
            }
            
            // TODO - Display as a color grid as follows depending on the value of each cell:
            //  <= 0.25 - Red.
            //  >= 0.75 - Black.
            //  Everything else - White.
            //  This should fill the entire width of the inspector, and each cell should be evenly sized
        }).Every(16);
        
        return root;
    }
}
#endif