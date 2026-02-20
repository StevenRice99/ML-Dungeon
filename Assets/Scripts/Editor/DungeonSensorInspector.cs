#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Inspector for the <see cref="DungeonSensor"/>.
/// </summary>
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
            // Grab the calculated width of the container.
            float containerWidth = visualization.layout.width;
            
            // If the width is 0 or NaN, the layout engine hasn't processed the UI yet. Skip this frame.
            if (containerWidth is <= 0 or float.NaN)
            {
                return;
            }

            // Clear the container first.
            visualization.Clear();
            
            // Nothing to do if not playing or no sensor data.
            if (!Application.isPlaying || target is not DungeonSensor sensor || sensor.Sensed == null)
            {
                return;
            }
            
            // Nothing to do if the data is empty.
            int a = sensor.Sensed.GetLength(0);
            int b = sensor.Sensed.GetLength(1);
            if (a < 1 || b < 1)
            {
                return;
            }
            
            // Calculate the perfect square size based on the available width and column count.
            float cellSize = containerWidth / b;
            
            // Loop through the array to build the grid.
            for (int i = 0; i < a; i++)
            {
                // Create a horizontal container for each row.
                VisualElement row = new()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        height = cellSize
                    }
                };
                
                for (int j = 0; j < b; j++)
                {
                    row.Add(new()
                    {
                        style =
                        {
                            // Apply strict width and height to force a square.
                            width = cellSize,
                            height = cellSize,
                            
                            // Evaluate the sensed value and assign the correct background color.
                            backgroundColor = sensor.Sensed[i, j] switch
                            {
                                <= 0.25f => Color.red,
                                >= 0.75f => Color.black,
                                _ => Color.white
                            }
                        }
                    });
                }
                
                visualization.Add(row);
            }
        }).Every(16);
        
        return root;
    }
}
#endif