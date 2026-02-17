using System;
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
        
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // TODO - Implement a full level design.
                //  1 - There must always be a player/start and a coin/end in the level. They should each be in a distinct corner of the level.
                //  2 - If there are any enemies in the level, there must be at least one weapon and one health pick-up in the level.
                //  3 - Attempt to place up to the desired number of enemies, spawning as far away from the player as possible. If there are no more valid spaces for enemies, stop spawning them.
                //  4 - Attempt to fill the desired percentage of floors as walls, ensuring all traversable areas (all other types of parts) are fully-connected and reachable from each other. If there are no more valid spaces for walls, stop spawning them.
                level[i, j] = LevelParts.Floor;
            }
        }
        
        return level;
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