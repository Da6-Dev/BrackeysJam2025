// Scripts/Managers/GameCoordinator.cs
using UnityEngine;

public class GameCoordinator : MonoBehaviour
{
    public static GameCoordinator Instance { get; private set; }

    [Header("System References")]
    public WorldGenerator worldGenerator;
    public MapGenerator mapGenerator;
    public OpportunityManager opportunityManager;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            // Se uma instância já existe, este é um duplicata. Destrua-o.
            Destroy(this.gameObject);
            return;
        }
        // Se não, este se torna a instância única.
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        if (worldGenerator == null || mapGenerator == null || opportunityManager == null)
        {
            Debug.LogError("!!! ATENÇÃO: As referências no GameCoordinator não foram atribuídas no Inspector !!!");
            return;
        }
        StartNewGame();
    }

    public void StartNewGame()
    {
        Debug.Log("--- GameCoordinator: INICIANDO NOVO JOGO ---");
        worldGenerator.GenerateWorld();
        mapGenerator.GenerateMapFromData(worldGenerator.world);
        opportunityManager.GenerateNewOpportunities(worldGenerator.world);
    }

    public void EndTurn()
    {
        if (worldGenerator.world == null) return;
        Debug.Log("--- GameCoordinator: FINALIZANDO RODADA ---");
        opportunityManager.GenerateNewOpportunities(worldGenerator.world);
    }
}