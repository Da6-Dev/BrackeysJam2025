using UnityEngine;
using System.Collections.Generic;
using TycoonGame;
using System.Linq;

public class OpportunityManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The prefab for the opportunity icon that will be displayed on the map.")]
    public GameObject opportunityIconPrefab; // <-- NEW: Reference to the icon prefab
    [Tooltip("The parent transform for the icons (should be the map image itself).")]
    public RectTransform mapIconParent;      // <-- NEW: Reference to the map's RectTransform
    [Tooltip("Reference to the map generator to get map dimensions.")]
    public MapGenerator mapGenerator;        // <-- NEW: Reference to get map size

    [Header("Balancing")]
    [SerializeField] private int minOpportunitiesPerRound = 3;
    [SerializeField] private int maxOpportunitiesPerRound = 5;

    [Header("Live Data")]
    public List<InvestmentOpportunity> activeOpportunities;
    private List<GameObject> activeIcons = new List<GameObject>(); // <-- NEW: To keep track of spawned icons

    /// <summary>
    /// Clears old opportunities and generates a new set for the upcoming round.
    /// </summary>
    public void GenerateNewOpportunities(List<Country> world)
    {
        if (world == null || world.Count == 0 || opportunityIconPrefab == null || mapIconParent == null || mapGenerator == null)
        {
            Debug.LogError("A required reference is missing in OpportunityManager! Cannot generate opportunities.");
            return;
        }

        // --- NEW: Clean up icons from the previous round ---
        foreach (GameObject icon in activeIcons)
        {
            Destroy(icon);
        }
        activeIcons.Clear();
        // ---------------------------------------------------

        activeOpportunities = new List<InvestmentOpportunity>();
        int opportunitiesToCreate = Random.Range(minOpportunitiesPerRound, maxOpportunitiesPerRound + 1);

        for (int i = 0; i < opportunitiesToCreate; i++)
        {
            Country randomCountry = world[Random.Range(0, world.Count)];
            
            Sector randomSector = GetRandomEnumValue<Sector>();
            string projectName = $"{randomSector} Initiative in {randomCountry.countryName}";
            string description = $"A promising new venture in the {randomSector} sector.";
            float cost = Random.Range(10, 51);
            float reward = cost * Random.Range(2.0f, 5.0f);
            float successChance = Random.Range(0.4f, 0.85f);
            
            InvestmentOpportunity newOpportunity = new InvestmentOpportunity(projectName, description, randomSector, cost, reward, successChance, randomCountry);
            activeOpportunities.Add(newOpportunity);

            // --- NEW: Instantiate and position the visual icon ---
            SpawnIconForOpportunity(newOpportunity);
            // ----------------------------------------------------
        }

        Debug.Log($"{activeOpportunities.Count} new investment opportunities have been generated and displayed.");
    }

    private void SpawnIconForOpportunity(InvestmentOpportunity opportunity)
    {
        // Create an instance of the prefab as a child of the map parent
        GameObject iconInstance = Instantiate(opportunityIconPrefab, mapIconParent);
        activeIcons.Add(iconInstance); // Add to our list for cleanup later

        // Position the icon on the map
        RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
        if (iconRect != null)
        {
            // Convert map coordinates (e.g., 0-1000) to UI anchored position
            float parentWidth = mapIconParent.rect.width;
            float parentHeight = mapIconParent.rect.height;

            // Using the actual map dimensions from MapGenerator
            float mapWidth = mapGenerator.mapWidth;
            float mapHeight = mapGenerator.mapHeight;

            // Calculate position relative to the parent's size
            float newX = (opportunity.mapPosition.x / mapWidth) * parentWidth;
            float newY = (opportunity.mapPosition.y / mapHeight) * parentHeight;

            // The anchor for UI is in the center, so we offset by half the parent's size
            // to align with the bottom-left origin of our map data.
            iconRect.anchoredPosition = new Vector2(newX - (parentWidth / 2), newY - (parentHeight / 2));
        }

        // Link the icon's display script to the opportunity data
        OpportunityDisplay display = iconInstance.GetComponent<OpportunityDisplay>();
        if (display != null)
        {
            display.Initialize(opportunity);
        }
    }

    private T GetRandomEnumValue<T>() where T : System.Enum
    {
        var values = System.Enum.GetValues(typeof(T));
        return (T)values.GetValue(Random.Range(0, values.Length));
    }
}