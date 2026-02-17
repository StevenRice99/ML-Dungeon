using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handle creating the level.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class Level : MonoBehaviour
{
    /// <summary>
    /// The size of the level to generate.
    /// </summary>
    [Header("Generation")]
    [Tooltip("The size of the level to generate.")]
    [Min(2)]
    [SerializeField]
    private int size = 10;
    
    /// <summary>
    /// What percentage of floors to try turning into walls.
    /// </summary>
    [Tooltip("What percentage of floors to try turning into walls.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float wallPercent = 0.1f;
    
    /// <summary>
    /// The number of enemies to attempt to spawn.
    /// </summary>
    [Tooltip("The number of enemies to attempt to spawn.")]
    [Min(0)]
    [SerializeField]
    private int desiredEnemies = 3;

    /// <summary>
    /// The spacing of pieces of the level.
    /// </summary>
    [Tooltip("The spacing of pieces of the level.")]
    [SerializeField]
    private float pieceSpacing = 1f;
    
    /// <summary>
    /// The floors we can use. These spaces are traversable.
    /// </summary>
    [Header("Prefabs")]
    [Tooltip("The floors we can use. These spaces are traversable.")]
    [SerializeField]
    private GameObject[] floorPrefabs;
    
    /// <summary>
    /// The walls we can use. These spaces are not traversable.
    /// </summary>
    [Tooltip("The walls we can use. These spaces are not traversable.")]
    [SerializeField]
    private GameObject[] wallPrefabs;
    
    /// <summary>
    /// The prefab for the player. This spawns over a floor, meaning its space is traversable.
    /// </summary>
    [Tooltip("The prefab for the player. This spawns over a floor, meaning its space is traversable.")]
    [SerializeField]
    private GameObject playerPrefab;
    
    /// <summary>
    /// The prefab to use for the enemy. These spawn over floors, meaning their space is traversable.
    /// </summary>
    [Tooltip("The prefab to use for the enemy. These spawn over floors, meaning their space is traversable.")]
    [SerializeField]
    private GameObject enemyPrefab;
    
    /// <summary>
    /// The prefab for the objective coin. This space is traversable.
    /// </summary>
    [Tooltip("The prefab for the objective coin. This space is traversable.")]
    [SerializeField]
    private GameObject coinPrefab;
    
    /// <summary>
    /// The prefab for the health pickup. These spaces are traversable.
    /// </summary>
    [Tooltip("The prefab for the health pickup. These spaces are traversable.")]
    [SerializeField]
    private GameObject healthPrefab;
    
    /// <summary>
    /// The prefab for the weapon pickup. These spaces are traversable.
    /// </summary>
    [Tooltip("The prefab for the weapon pickup. These spaces are traversable.")]
    [SerializeField]
    private GameObject weaponPrefab;
    
    /// <summary>
    /// All spawned parts.
    /// </summary>
    private readonly List<GameObject> _parts = new();
    
    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
    /// </summary>
    private void Start()
    {
        PlaceLevel(GenerateLevel());
    }
    
    /// <summary>
    /// Generate the level.
    /// </summary>
    /// <returns>The generated level.</returns>
    private LevelParts[,] GenerateLevel()
    {
        LevelParts[,] level = new LevelParts[size, size];
        
        // Initialize all spaces as floors
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                level[i, j] = LevelParts.Floor;
            }
        }
        
        // 1. Place Start and End in distinct corners
        Vector2Int[] corners = {
            new Vector2Int(0, 0),
            new Vector2Int(0, size - 1),
            new Vector2Int(size - 1, 0),
            new Vector2Int(size - 1, size - 1)
        };
        
        int startIndex = Random.Range(0, 4);
        int endIndex = Random.Range(0, 4);
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, 4);
        }
        
        Vector2Int startPos = corners[startIndex];
        Vector2Int endPos = corners[endIndex];
        
        level[startPos.x, startPos.y] = LevelParts.Start;
        level[endPos.x, endPos.y] = LevelParts.End;
        
        // 4. Place Walls ensuring full connectivity
        int totalCells = size * size;
        int targetWalls = Mathf.FloorToInt(totalCells * wallPercent);
        int wallsPlaced = 0;
        int maxWallAttempts = targetWalls * 10; // Prevent infinite loops
        int attempts = 0;
        int currentTraversable = totalCells;
        
        while (wallsPlaced < targetWalls && attempts < maxWallAttempts)
        {
            attempts++;
            int rx = Random.Range(0, size);
            int ry = Random.Range(0, size);
            
            // Only place walls on empty floors
            if (level[rx, ry] == LevelParts.Floor)
            {
                level[rx, ry] = LevelParts.Wall;
                currentTraversable--;
                
                // If the level is still fully connected, keep the wall
                if (IsConnected(level, startPos, currentTraversable))
                {
                    wallsPlaced++;
                }
                else
                {
                    // Revert the wall if it breaks the path
                    level[rx, ry] = LevelParts.Floor;
                    currentTraversable++;
                }
            }
        }
        
        // 3. Place Enemies as far away from the player as possible
        // Calculate the true walking distance from the start position using BFS
        int[,] distances = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                distances[i, j] = -1;
            }
        }
        
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(startPos);
        distances[startPos.x, startPos.y] = 0;
        
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };
        
        while (q.Count > 0)
        {
            Vector2Int curr = q.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                
                if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                {
                    if (level[nx, ny] != LevelParts.Wall && distances[nx, ny] == -1)
                    {
                        distances[nx, ny] = distances[curr.x, curr.y] + 1;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
        
        // Gather all remaining floors and their distances from the player
        List<KeyValuePair<Vector2Int, int>> floorDistances = new();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (level[i, j] == LevelParts.Floor)
                {
                    floorDistances.Add(new KeyValuePair<Vector2Int, int>(new Vector2Int(i, j), distances[i, j]));
                }
            }
        }
        
        // Sort floors by distance descending (furthest first)
        floorDistances.Sort((a, b) => b.Value.CompareTo(a.Value));
        
        int enemiesPlaced = 0;
        for (int i = 0; i < desiredEnemies && i < floorDistances.Count; i++)
        {
            Vector2Int pos = floorDistances[i].Key;
            level[pos.x, pos.y] = LevelParts.Enemy;
            enemiesPlaced++;
        }
        
        // 2. Place Weapon and Health if any enemies were placed
        if (enemiesPlaced > 0)
        {
            List<Vector2Int> remainingFloors = new();
            for (int i = enemiesPlaced; i < floorDistances.Count; i++)
            {
                remainingFloors.Add(floorDistances[i].Key);
            }
            
            // Shuffle the remaining floors for random item placement
            for (int i = 0; i < remainingFloors.Count; i++)
            {
                int swap = Random.Range(i, remainingFloors.Count);
                (remainingFloors[i], remainingFloors[swap]) = (remainingFloors[swap], remainingFloors[i]);
            }
            
            if (remainingFloors.Count > 0)
            {
                Vector2Int pos = remainingFloors[0];
                level[pos.x, pos.y] = LevelParts.Weapon;
                remainingFloors.RemoveAt(0);
            }
            
            if (remainingFloors.Count > 0)
            {
                Vector2Int pos = remainingFloors[0];
                level[pos.x, pos.y] = LevelParts.Health;
                remainingFloors.RemoveAt(0);
            }
        }
        
        return level;
    }
    
    /// <summary>
    /// Validates if all traversable spaces in the grid are fully connected.
    /// </summary>
    /// <param name="grid">The current state of the level grid.</param>
    /// <param name="startPos">A known traversable starting position.</param>
    /// <param name="targetCount">The expected number of reachable cells.</param>
    /// <returns>True if every non-wall piece can be reached.</returns>
    private bool IsConnected(LevelParts[,] grid, Vector2Int startPos, int targetCount)
    {
        int s = grid.GetLength(0);
        bool[,] visited = new bool[s, s];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        queue.Enqueue(startPos);
        visited[startPos.x, startPos.y] = true;
        int reachableCount = 1;
        
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { -1, 1, 0, 0 };
        
        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            
            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                
                if (nx >= 0 && nx < s && ny >= 0 && ny < s)
                {
                    if (!visited[nx, ny] && grid[nx, ny] != LevelParts.Wall)
                    {
                        visited[nx, ny] = true;
                        reachableCount++;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
        
        return reachableCount == targetCount;
    }
    
    /// <summary>
    /// Place the level.
    /// </summary>
    /// <param name="level">The level data.</param>
    private void PlaceLevel(LevelParts[,] level)
    {
        // Get rid of previous levels.
        foreach (GameObject part in _parts)
        {
            if (part)
            {
                Destroy(part);
            }
        }
        
        // Reset the cache.
        _parts.Clear();
        
        // Place the generated level.
        Transform t = transform;
        Vector3 p = t.position;
        float shift = size / 2f * pieceSpacing;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                switch (level[i, j])
                {
                    case LevelParts.Floor:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        break;
                    case LevelParts.Wall:
                        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, j, t, p, shift);
                        break;
                    case LevelParts.Start:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        InstantiatePiece(playerPrefab, i, j, t, p, shift);
                        break;
                    case LevelParts.End:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        InstantiatePiece(coinPrefab, i, j, t, p, shift);
                        break;
                    case LevelParts.Enemy:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        InstantiatePiece(enemyPrefab, i, j, t, p, shift);
                        break;
                    case LevelParts.Health:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        InstantiatePiece(healthPrefab, i, j, t, p, shift);
                        break;
                    case LevelParts.Weapon:
                        InstantiatePiece(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift);
                        InstantiatePiece(weaponPrefab, i, j, t, p, shift);
                        break;
                }
            }
        }
        
        // Place outer walls.
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, -1, t, p, shift);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, size, t, p, shift);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, -1, t, p, shift);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, size, t, p, shift);
        for (int i = 0; i < size; i++)
        {
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, i, t, p, shift);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, -1, t, p, shift);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, i, t, p, shift);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, size, t, p, shift);
        }
    }
    
    /// <summary>
    /// Instantiate a piece.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="i">The first index.</param>
    /// <param name="j">The second index.</param>
    /// <param name="t">The transform of this.</param>
    /// <param name="p">The position of this.</param>
    /// <param name="shift">How much to shift each piece for centering.</param>
    /// <returns></returns>
    private GameObject InstantiatePiece(GameObject prefab, int i, int j, Transform t, Vector3 p, float shift)
    {
        GameObject go = Instantiate(prefab, t);
        go.transform.position = new(p.x + i * pieceSpacing - shift, p.y, p.z + j * pieceSpacing - shift);
        go.name = prefab.name;
        _parts.Add(go);
        return go;
    }
    
    /// <summary>
    /// The flags for each part of the level.
    /// </summary>
    private enum LevelParts
    {
        /// <summary>
        /// Empty floor spaces.
        /// </summary>
        Floor = 0,
        
        /// <summary>
        /// Wall or obstacle spaces.
        /// </summary>
        Wall = 1,
        
        /// <summary>
        /// Where the player spawns.
        /// </summary>
        Start = 2,
        
        /// <summary>
        /// Where the coin to end the level is placed.
        /// </summary>
        End = 3,
        
        /// <summary>
        /// Where enemies are placed.
        /// </summary>
        Enemy = 4,
        
        /// <summary>
        /// Where health pickups are placed.
        /// </summary>
        Health = 5,
        
        /// <summary>
        /// Where the weapon pickup is placed.
        /// </summary>
        Weapon = 6
    }
}