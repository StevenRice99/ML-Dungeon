using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.AI.Navigation;
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
    /// The prefab for the objective coin. This space is traversable.
    /// </summary>
    [Tooltip("The prefab for the objective coin. This space is traversable.")]
    [SerializeField]
    private GameObject coinPrefab;
    
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
    /// All weapon pickup.
    /// </summary>
    private GameObject _weapon;
    
    /// <summary>
    /// All end-of-level coin.
    /// </summary>
    private GameObject _coin;
    
    /// <summary>
    /// The <see cref="Player"/>.
    /// </summary>
    public Player Agent { get; private set; }
    
    /// <summary>
    /// The active enemies.
    /// </summary>
    private readonly HashSet<Enemy> _enemiesActive = new();
    
    /// <summary>
    /// The inactive enemies.
    /// </summary>
    private readonly HashSet<Enemy> _enemiesInactive = new();
    
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
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. This function can be a coroutine.
    /// </summary>
    private void Start()
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
        PlaceLevel(GenerateLevel());
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
        int endIndex = Random.Range(0, 4);
        while (endIndex == startIndex)
        {
            endIndex = Random.Range(0, 4);
        }
        
        Vector2Int startPos = corners[startIndex];
        Vector2Int endPos = corners[endIndex];
        
        level[startPos.x, startPos.y] = LevelParts.Start;
        level[endPos.x, endPos.y] = LevelParts.End;
        
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
                if (level[rx, ry] == LevelParts.Floor)
                {
                    level[rx, ry] = LevelParts.Wall;
                    currentTraversable--;
                
                    // If the level is still fully connected, keep the wall.
                    if (IsConnected(level, startPos, currentTraversable))
                    {
                        wallsPlaced++;
                    }
                    else
                    {
                        // Revert the wall if it breaks the path.
                        level[rx, ry] = LevelParts.Floor;
                        currentTraversable++;
                    }
                }
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
        
        // Place the weapon pickup.
        List<Vector2Int> remainingFloors = new();
        for (int i = maxEnemies; i < floorDistances.Count; i++)
        {
            remainingFloors.Add(floorDistances[i].Key);
        }
        
        Vector2Int weapon = remainingFloors[Random.Range(0, remainingFloors.Count)];
        level[weapon.x, weapon.y] = LevelParts.Weapon;
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
        
        // Reset the active enemies.
        foreach (Enemy enemy in _enemiesActive)
        {
            if (!enemy)
            {
                continue;
            }
            
            enemy.gameObject.SetActive(false);
            _enemiesInactive.Add(enemy);
        }
        
        _enemiesActive.Clear();
        
        // Hide interactable items.
        if (_coin)
        {
            _coin?.SetActive(false);
        }
        
        if (_weapon)
        {
            _weapon.SetActive(false);
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
                    case LevelParts.End:
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
                    case LevelParts.End:
                        if (_coin)
                        {
                            _coin.transform.position = IndexToPosition(i, j, p, shift);
                            _coin.SetActive(true);
                        }
                        else
                        {
                            _coin = InstantiatePiece(coinPrefab, i, j, t, p, shift);
                        }
                        break;
                    case LevelParts.Weapon:
                        if (_weapon)
                        {
                            _weapon.transform.SetPositionAndRotation(IndexToPosition(i, j, p, shift), Quaternion.Euler(new(0, 90f * Random.Range(0, 4), 0)));
                            _weapon.SetActive(true);
                        }
                        else
                        {
                            _weapon = InstantiateFixed(weaponPrefab, i, j, t, p, shift);
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
                            _enemiesActive.Add(enemy);
                            enemy.transform.SetPositionAndRotation(position, orientation);
                            enemy.gameObject.SetActive(true);
                        }
                        else
                        {
                            enemy = Instantiate(enemyPrefab, position, orientation, t);
                            enemy.name = enemyPrefab.name;
                            enemy.Instance = this;
                            _enemiesActive.Add(enemy);
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
    /// Instantiate a prefab which looks towards the center position, passed as the "p" parameter.
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
    private GameObject InstantiateCenter(GameObject prefab, int i, int j, Transform t, Vector3 p, float shift, List<GameObject> cache = null, List<GameObject> inactive = null)
    {
        Vector3 position = IndexToPosition(i, j, p, shift);
        GameObject go = InstantiatePiece(prefab, position, FaceCenter(position, p), t, cache, inactive);
        return go;
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
    private GameObject InstantiatePiece(GameObject prefab, int i, int j, Transform t, Vector3 p, float shift, List<GameObject> cache = null, List<GameObject> inactive = null)
    {
        return InstantiatePiece(prefab, IndexToPosition(i, j, p, shift), Quaternion.identity, t, cache, inactive);
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
    public int EnemiesCount => _enemiesActive.Count;
    
    /// <summary>
    /// Eliminate an <see cref="Enemy"/>.
    /// </summary>
    /// <param name="enemy">The <see cref="Enemy"/> to eliminate.</param>
    public void EliminateEnemy([NotNull] Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        if (_enemiesActive.Remove(enemy))
        {
            _enemiesInactive.Add(enemy);
        }
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
        /// Where the weapon pickup is placed.
        /// </summary>
        Weapon = 5
    }
}