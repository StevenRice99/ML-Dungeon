using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// Handle creating the level.
/// </summary>
[AddComponentMenu("ML-Dungeon/Level")]
[HelpURL("https://github.com/StevenRice99/ML-Dungeon")]
[SelectionBase]
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshSurface))]
public class Level : MonoBehaviour
{
    /// <summary>
    /// The size of the level to generate.
    /// </summary>
    public int Size
    {
        get => size;
        set => size = Mathf.Max(value, 2);
    }
    
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
    public float WallPercent
    {
        get => wallPercent;
        set => wallPercent = Mathf.Clamp01(value);
    }
    
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
    public int DesiredEnemies
    {
        get => desiredEnemies;
        set => desiredEnemies = Mathf.Max(value, 0);
    }
    
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
    [field: Tooltip("The spacing of pieces of the level.")]
    [field: SerializeField]
    public float PieceSpacing { get; private set; } = 1f;
    
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
    private Player playerPrefab;
    
    /// <summary>
    /// The prefab to use for the enemy. These spawn over floors, meaning their space is traversable.
    /// </summary>
    [Tooltip("The prefab to use for the enemy. These spawn over floors, meaning their space is traversable.")]
    [SerializeField]
    private Enemy enemyPrefab;
    
    /// <summary>
    /// The prefab for the weapon pickup. These spaces are traversable.
    /// </summary>
    [Tooltip("The prefab for the weapon pickup. These spaces are traversable.")]
    [SerializeField]
    private GameObject weaponPrefab;
    
    /// <summary>
    /// The <see cref="NavMeshSurface"/> surface to bake for this.
    /// </summary>
    [HideInInspector]
    [Tooltip("The navigation mesh surface to bake for this.")]
    [SerializeField]
    private NavMeshSurface surface;
    
    /// <summary>
    /// All spawned floors.
    /// </summary>
    private readonly List<GameObject> _floors = new();
    
    /// <summary>
    /// All currently unused but previously spawned floors.
    /// </summary>
    private readonly List<GameObject> _floorsExcess = new();
    
    /// <summary>
    /// All spawned walls.
    /// </summary>
    private readonly List<GameObject> _walls = new();
    
    /// <summary>
    /// All currently unused but previously spawned floors.
    /// </summary>
    private readonly List<GameObject> _wallsExcess = new();
    
    /// <summary>
    /// The weapon pickup.
    /// </summary>
    [NonSerialized]
    public GameObject Weapon;
    
    /// <summary>
    /// The <see cref="Player"/>.
    /// </summary>
    public Player Agent { get; private set; }
    
    /// <summary>
    /// The active enemies.
    /// </summary>
    public readonly HashSet<Enemy> EnemiesActive = new();
    
    /// <summary>
    /// The inactive enemies.
    /// </summary>
    private readonly HashSet<Enemy> _enemiesInactive = new();

    /// <summary>
    /// Level data about what areas are walkable, with walkable spaces being true and non-walkable being false.
    /// </summary>
    private bool[,] _map;
    
    /// <summary>
    /// Level data about what areas are walkable, with walkable spaces being true and non-walkable being false.
    /// </summary>
    public bool[,] Map
    {
        get
        {
            bool[,] data = new bool[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    data[i, j] = _map[i, j];
                }
            }
            
            return data;
        }
    }
    
    /// <summary>
    /// Get a <see cref="_map"/> data centered around the <see cref="Agent"/> which can see a given size away. Any out-of-bounds locations will be returned as un-walkable.<br/>
    /// Unwalkable = 1<br/>
    /// Walkable = 0.5<br/>
    /// Walkable but has an enemy = 0
    /// </summary>
    /// <param name="distance">How many locations away can the relative map see.</param>
    /// <returns>The <see cref="_map"/> data centered around the <see cref="Agent"/> which can see a given size away.</returns>
    public float[,] SensorMap(int distance)
    {
        int2 coordinates = PositionToIndex(Agent.transform.position);
        
        // Calculate the dimension of the square grid.
        int length = distance * 2 + 1;
        float[,] localMap = new float[length, length];

        HashSet<int2> enemies = new();
        foreach (Enemy enemy in EnemiesActive)
        {
            enemies.Add(PositionToIndex(enemy.transform.position));
        }

        int a = _map.GetLength(0);
        int b = _map.GetLength(1);
        
        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < length; y++)
            {
                // Translate local grid coordinates to global map coordinates.
                int2 real = new(coordinates.x - distance + x, coordinates.y - distance + y);
                
                // Check if the calculated global coordinates are within bounds.
                if (real.x >= 0 && real.x < a && real.y >= 0 && real.y < b)
                {
                    localMap[x, y] = enemies.Contains(real) ? 0f : _map[real.x, real.y] ? 0.5f : 1f;
                }
                else
                {
                    // Out-of-bounds locations default to unwalkable.
                    localMap[x, y] = 1f; 
                }
            }
        }
        
        return localMap;
    }
    
    /// <summary>
    /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        GetNavMeshSurface();
    }
    
    /// <summary>
    /// Get the <see cref="surface"/>.
    /// </summary>
    private void GetNavMeshSurface()
    {
        if (surface == null || surface.gameObject != gameObject)
        {
            surface = GetComponent<NavMeshSurface>();
        }
        
        if (!surface)
        {
            return;
        }
        
        // Only get the current volume.
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.buildHeightMesh = true;
    }
    
    /// <summary>
    /// Awake is called when an enabled script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        if (!Agent)
        {
            Transform t = transform;
            Agent = Instantiate(playerPrefab, t.position, Quaternion.identity, t);
            Agent.name = playerPrefab.name;
        }
        
        Agent.Instance = this;
        GetNavMeshSurface();
    }
    
    /// <summary>
    /// Create the level.
    /// </summary>
    public void CreateLevel()
    {
        LevelParts[,] data = GenerateLevel();
        _map = new bool[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                _map[i, j] = data[i, j] != LevelParts.Wall;
            }
        }
        PlaceLevel(data);
    }
    
    /// <summary>
    /// Generate the level.
    /// </summary>
    /// <returns>The generated level.</returns>
    private LevelParts[,] GenerateLevel()
    {
        LevelParts[,] level = new LevelParts[size, size];
        
        // Initialize all spaces as floors.
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                level[i, j] = LevelParts.Floor;
            }
        }
        
        // Place the start and end in distinct corners.
        Vector2Int[] corners = {
            new(0, 0),
            new(0, size - 1),
            new(size - 1, 0),
            new(size - 1, size - 1)
        };
        
        int startIndex = Random.Range(0, 4);
        int weaponIndex = Random.Range(0, 4);
        while (weaponIndex == startIndex)
        {
            weaponIndex = Random.Range(0, 4);
        }
        
        Vector2Int startPos = corners[startIndex];
        Vector2Int weaponPos = corners[weaponIndex];
        
        level[startPos.x, startPos.y] = LevelParts.Start;
        level[weaponPos.x, weaponPos.y] = LevelParts.Weapon;
        
        // Place walls ensuring full connectivity.
        int totalCells = size * size;
        int maxWalls = totalCells - 2 - desiredEnemies;
        if (maxWalls > 0)
        {
            int targetWalls = Mathf.Min(Mathf.FloorToInt(totalCells * wallPercent), maxWalls);
            int wallsPlaced = 0;
            int maxWallAttempts = targetWalls * 10;
            int attempts = 0;
            int currentTraversable = totalCells;
            
            while (wallsPlaced < targetWalls && attempts < maxWallAttempts)
            {
                attempts++;
                int rx = Random.Range(0, size);
                int ry = Random.Range(0, size);
                
                // Only place walls on empty floors.
                if (level[rx, ry] != LevelParts.Floor)
                {
                    continue;
                }
                
                level[rx, ry] = LevelParts.Wall;
                currentTraversable--;
                
                // If the level is still fully connected, keep the wall.
                if (IsConnected(level, startPos, currentTraversable))
                {
                    wallsPlaced++;
                    continue;
                }
                
                // Revert the wall if it breaks the path.
                level[rx, ry] = LevelParts.Floor;
                currentTraversable++;
            }
        }
        
        // Place Enemies as far away from the player as possible from true walking distance from the start position using BFS.
        int[,] distances = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                distances[i, j] = -1;
            }
        }
        
        Queue<Vector2Int> q = new();
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
                        q.Enqueue(new(nx, ny));
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
                    floorDistances.Add(new(new(i, j), distances[i, j]));
                }
            }
        }
        
        // Sort floors by distance, with the furthest first.
        floorDistances.Sort((a, b) => b.Value.CompareTo(a.Value));

        int maxEnemies = Mathf.Min(desiredEnemies, floorDistances.Count - 1);
        for (int i = 0; i < maxEnemies && i < floorDistances.Count; i++)
        {
            Vector2Int pos = floorDistances[i].Key;
            level[pos.x, pos.y] = LevelParts.Enemy;
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
    private static bool IsConnected(LevelParts[,] grid, Vector2Int startPos, int targetCount)
    {
        int s = grid.GetLength(0);
        bool[,] visited = new bool[s, s];
        Queue<Vector2Int> queue = new();
        
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
                        queue.Enqueue(new(nx, ny));
                    }
                }
            }
        }
        
        return reachableCount == targetCount;
    }
    
    /// <summary>
    /// Pool active items to an inactive cache.
    /// </summary>
    /// <param name="active">The active items.</param>
    /// <param name="inactive">The inactive cache.</param>
    private static void Hide([NotNull] List<GameObject> active, [NotNull] List<GameObject> inactive)
    {
        foreach (GameObject item in active)
        {
            if (!item)
            {
                continue;
            }
            
            item.SetActive(false);
            inactive.Add(item);
        }
        
        active.Clear();
    }
    
    /// <summary>
    /// Place the level.
    /// </summary>
    /// <param name="level">The level data.</param>
    private void PlaceLevel(LevelParts[,] level)
    {
        // Hide all previous parts.
        Hide(_floors, _floorsExcess);
        Hide(_walls, _wallsExcess);
        
        // Reset enemies.
        foreach (Enemy enemy in EnemiesActive)
        {
            if (enemy)
            {
                _enemiesInactive.Add(enemy);
            }
        }
        
        EnemiesActive.Clear();

        foreach (Enemy enemy in _enemiesInactive)
        {
            enemy.gameObject.SetActive(false);
        }
        
        if (Weapon)
        {
            Weapon.SetActive(false);
        }
        
        // Place the generated level.
        Transform t = transform;
        Vector3 p = t.position;
        float shift = (size - 1) / 2f * PieceSpacing;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                switch (level[i, j])
                {
                    case LevelParts.Floor:
                    case LevelParts.Enemy:
                    case LevelParts.Start:
                    case LevelParts.Weapon:
                        InstantiateFixed(floorPrefabs[Random.Range(0, floorPrefabs.Length)], i, j, t, p, shift, _floors, _floorsExcess);
                        break;
                    case LevelParts.Wall:
                        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, j, t, p, shift, _walls, _wallsExcess);
                        break;
                }
            }
        }
        
        // Place outer walls.
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, -1, t, p, shift, _walls, _wallsExcess);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, size, t, p, shift, _walls, _wallsExcess);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, -1, t, p, shift, _walls, _wallsExcess);
        InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, size, t, p, shift, _walls, _wallsExcess);
        for (int i = 0; i < size; i++)
        {
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], -1, i, t, p, shift, _walls, _wallsExcess);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, -1, t, p, shift, _walls, _wallsExcess);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], size, i, t, p, shift, _walls, _wallsExcess);
            InstantiatePiece(wallPrefabs[Random.Range(0, wallPrefabs.Length)], i, size, t, p, shift, _walls, _wallsExcess);
        }
        
        // Build the mesh.
        Physics.SyncTransforms();
        surface?.BuildNavMesh();
        
        // Place dynamic elements.
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector3 position;
                Quaternion orientation;
                switch (level[i, j])
                {
                    case LevelParts.Start:
                        position = IndexToPosition(i, j, p, shift);
                        orientation = FaceCenter(position, p);
                        if (Agent == null)
                        {
                            Agent = Instantiate(playerPrefab, position, orientation, t);
                            Agent.name = playerPrefab.name;
                            Agent.Instance = this;
                        }
                        else
                        {
                            Agent.body.position = position;
                            Agent.body.rotation = orientation;
                        }
                        break;
                    case LevelParts.Weapon:
                        if (Weapon)
                        {
                            Weapon.transform.SetPositionAndRotation(IndexToPosition(i, j, p, shift), Quaternion.Euler(new(0, 90f * Random.Range(0, 4), 0)));
                            Weapon.SetActive(true);
                        }
                        else
                        {
                            Weapon = InstantiateFixed(weaponPrefab, i, j, t, p, shift);
                        }
                        break;
                    case LevelParts.Enemy:
                        position = IndexToPosition(i, j, p, shift);
                        orientation = FaceCenter(position, p);
                        Enemy enemy;
                        if (_enemiesInactive.Count > 0)
                        {
                            enemy = _enemiesInactive.First();
                            _enemiesInactive.Remove(enemy);
                            EnemiesActive.Add(enemy);
                            enemy.transform.SetPositionAndRotation(position, orientation);
                            enemy.gameObject.SetActive(true);
                        }
                        else
                        {
                            enemy = Instantiate(enemyPrefab, position, orientation, t);
                            enemy.name = enemyPrefab.name;
                            enemy.Instance = this;
                            EnemiesActive.Add(enemy);
                        }
                        break;
                    case LevelParts.Floor:
                    case LevelParts.Wall:
                    default:
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Instantiate a prefab at a random, fixed rotation.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="i">The first index.</param>
    /// <param name="j">The second index.</param>
    /// <param name="t">The transform of this.</param>
    /// <param name="p">The position of this.</param>
    /// <param name="shift">How much to shift each piece for centering.</param>
    /// <param name="cache">The cache to add this to.</param>
    /// <param name="inactive">The inactive objects we can pull from to reuse.</param>
    /// <returns>The spawned instance.</returns>
    private GameObject InstantiateFixed(GameObject prefab, int i, int j, Transform t, Vector3 p, float shift, List<GameObject> cache = null, List<GameObject> inactive = null)
    {
        return InstantiatePiece(prefab, IndexToPosition(i, j, p, shift), Quaternion.Euler(new(0, 90f * Random.Range(0, 4), 0)), t, cache, inactive);
    }
    
    /// <summary>
    /// Instantiate a piece at a given grid offset.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="i">The first index.</param>
    /// <param name="j">The second index.</param>
    /// <param name="t">The transform of this.</param>
    /// <param name="p">The position of this.</param>
    /// <param name="shift">How much to shift each piece for centering.</param>
    /// <param name="cache">The cache to add this to.</param>
    /// <param name="inactive">The inactive objects we can pull from to reuse.</param>
    /// <returns>The spawned instance.</returns>
    private void InstantiatePiece(GameObject prefab, int i, int j, Transform t, Vector3 p, float shift, List<GameObject> cache = null, List<GameObject> inactive = null)
    {
        InstantiatePiece(prefab, IndexToPosition(i, j, p, shift), Quaternion.identity, t, cache, inactive);
    }
    
    /// <summary>
    /// Instantiate a piece.
    /// </summary>
    /// <param name="prefab">The prefab to spawn.</param>
    /// <param name="p">The position to spawn it at.</param>
    /// <param name="r">The rotation to spawn it at.</param>
    /// <param name="t">The transform of this.</param>
    /// <param name="cache">The cache to add this to.</param>
    /// <param name="inactive">The inactive objects we can pull from to reuse.</param>
    /// <returns>The spawned instance.</returns>
    private static GameObject InstantiatePiece(GameObject prefab, Vector3 p, Quaternion r, Transform t, List<GameObject> cache = null, List<GameObject> inactive = null)
    {
        GameObject go;
        if (inactive == null || inactive.Count < 1)
        {
            go = Instantiate(prefab, p, r, t);
            go.name = prefab.name;
        }
        else
        {
            int index = Random.Range(0, inactive.Count);
            go = inactive[index];
            inactive.RemoveAt(index);
            go.transform.SetPositionAndRotation(p, r);
            go.SetActive(true);
        }
        
        cache?.Add(go);
        return go;
    }
    
    /// <summary>
    /// Convert an index to a position.
    /// </summary>
    /// <param name="i">The first index.</param>
    /// <param name="j">The second index.</param>
    /// <param name="p">The position of this.</param>
    /// <param name="shift">How much to shift each piece for centering.</param>
    /// <returns>The position to place this at.</returns>
    private Vector3 IndexToPosition(int i, int j, Vector3 p, float shift)
    {
        return new(p.x + i * PieceSpacing - shift, p.y, p.z + j * PieceSpacing - shift);
    }
    
    /// <summary>
    /// Have an object face the center.
    /// </summary>
    /// <param name="current">The current position.</param>
    /// <param name="center">The center position.</param>
    private static Quaternion FaceCenter(Vector3 current, Vector3 center)
    {
        // Calculate the direction from the object's current position to the center.
        Vector3 directionToCenter = center - current;
        
        // Zero out the Y axis to ensure the object stays flat on the floor.
        directionToCenter.y = 0f;
        
        // Prevent "Look rotation viewing vector is zero" warnings.
        return directionToCenter != Vector3.zero ? Quaternion.LookRotation(directionToCenter) : Quaternion.identity;
    }
    
    /// <summary>
    /// Get a random walkable point.
    /// </summary>
    /// <returns>A random walkable point.</returns>
    public Vector3 RandomWalkable() => _floors[Random.Range(0, _floors.Count)].transform.position;
    
    /// <summary>
    /// The number of active <see cref="Enemy"/>s.
    /// </summary>
    public int EnemiesCount => EnemiesActive.Count;
    
    /// <summary>
    /// Eliminate an <see cref="Enemy"/>.
    /// </summary>
    /// <param name="enemy">The <see cref="Enemy"/> to eliminate.</param>
    public void EliminateEnemy([NotNull] Enemy enemy)
    {
        enemy.Eliminate();
        if (EnemiesActive.Remove(enemy))
        {
            _enemiesInactive.Add(enemy);
        }
    }
    
    /// <summary>
    /// Get the tile coordinates that a position falls within.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="clamp">If the position should be clamped within bounds.</param>
    /// <returns>The tile coordinates that a position falls within.</returns>
    public int2 PositionToIndex(Vector3 position, bool clamp = true)
    {
        return PositionToIndex(new Vector2(position.x, position.z), clamp);
    }
    
    /// <summary>
    /// Get the tile coordinates that a position falls within.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="clamp">If the position should be clamped within bounds.</param>
    /// <returns>The tile coordinates that a position falls within.</returns>
    public int2 PositionToIndex(Vector2 position, bool clamp = true)
    {
        // Calculate the same shift used during generation.
        float shift = (size - 1) / 2f * PieceSpacing;
        
        // Reverse the logic from an index to a position.
        Vector3 o = transform.position;
        int i = Mathf.RoundToInt((position.x - o.x + shift) / PieceSpacing);
        int j = Mathf.RoundToInt((position.y - o.z + shift) / PieceSpacing);
        
        // Clamp the values to ensure they safely stay within the grid array bounds.
        return clamp ? new(Mathf.Clamp(i, 0, size - 1), Mathf.Clamp(j, 0, size - 1)) : new(i, j);
    }
    
    /// <summary>
    /// Get the current coordinate of the position corresponding to the level in the range of [0, 1] on each axis.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <returns>The current coordinate of the position corresponding to the level in the range of [0, 1] on each axis</returns>
    public Vector2 PositionToPercentage(Vector3 position)
    {
        return PositionToPercentage(new Vector2(position.x, position.z));
    }
    
    /// <summary>
    /// Get the current coordinate of the position corresponding to the level in the range of [0, 1] on each axis.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <returns>The current coordinate of the position corresponding to the level in the range of [0, 1] on each axis</returns>
    public Vector2 PositionToPercentage(Vector2 position)
    {
        // Calculate the shift used to center the grid during generation.
        float shift = (size - 1) / 2f * PieceSpacing;
        
        // Determine the minimum bounds of the level. Since the tile center is at (origin - shift), the edge is another half-spacing away.
        Vector3 origin = transform.position;
        float minX = origin.x - shift - PieceSpacing / 2f;
        float minZ = origin.z - shift - PieceSpacing / 2f; 
        
        // The total size of the level in world units.
        float totalSize = size * PieceSpacing;
        
        // Calculate the raw percentage based on distance from the minimum bounds.
        float percentX = (position.x - minX) / totalSize;
        float percentY = (position.y - minZ) / totalSize;
        
        // Return the values clamped to ensure they strictly stay within the [0, 1] range.
        return new(Mathf.Clamp01(percentX), Mathf.Clamp01(percentY));
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
        /// Where the weapon pickup is placed.
        /// </summary>
        Weapon = 4,
        
        /// <summary>
        /// Where enemies are placed.
        /// </summary>
        Enemy = 5
    }
}