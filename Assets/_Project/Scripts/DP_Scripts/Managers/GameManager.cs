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
    public WorldUpdater worldUpdater;

    void Start()
    {
        StartNewGame();
    }

    #region Game Loop

    // EM GameManager.cs

    #region Game Loop

    public void StartNewGame()
    {
        worldGenerator.worldSeed = Random.Range(0, 999999);
        worldGenerator.GenerateWorld();

        mapGenerator.GenerateMapFromData(worldGenerator.world);
    }

    /// <summary>
    /// Função pública para ser chamada por um botão de UI para avançar o tempo.
    /// </summary>
    public void AdvanceSemester()
    {
        if (worldUpdater != null && worldGenerator.world != null && worldGenerator.world.Count > 0)
        {
            worldUpdater.AdvanceSemester(worldGenerator.world);
        }
        else
        {
            Debug.LogError("WorldUpdater não está referenciado ou o mundo não foi gerado!");
        }
    }

    #endregion

    #endregion
}