// Scripts/Gameplay/GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        if (instance == null) { instance = this; } else { Destroy(gameObject); }
    }

    [Header("References")]
    public WorldGenerator worldGenerator;
    public MapGenerator mapGenerator;

    void Start()
    {
        StartNewGame();
    }

    #region Game Loop

    public void StartNewGame()
    {
        worldGenerator.worldSeed = Random.Range(0, 999999);
        worldGenerator.GenerateWorld();

        mapGenerator.GenerateMapFromData(worldGenerator.world);
    }

    #endregion
}