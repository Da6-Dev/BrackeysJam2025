using TycoonGame; // For the 'Sector' enum
using UnityEngine;

[System.Serializable]
public class InvestmentOpportunity // Class name must match the filename
{
    public string projectName;
    public string description;
    public Sector sector;

    // Base project values, before country modifiers.
    public float baseCost; // In millions
    public float baseReward; // In millions
    [Range(0f, 1f)]
    public float baseSuccessChance;

    // Reference to the country where the opportunity is located.
    public Country hostCountry;
    public int hostCountryID;

    // Position on the map for the opportunity's icon.
    public Vector2Int mapPosition;

    /// <summary>
    /// Constructor to create a new investment opportunity.
    /// </summary>
    public InvestmentOpportunity(string name, string desc, Sector projSector, float cost, float reward, float successChance, Country country)
    {
        this.projectName = name;
        this.description = desc;
        this.sector = projSector;
        this.baseCost = cost;
        this.baseReward = reward;
        this.baseSuccessChance = successChance;
        this.hostCountry = country;
        this.hostCountryID = country.countryID;

        // Simple logic to place the icon near the country's capital.
        int offsetX = Random.Range(-15, 16);
        int offsetY = Random.Range(-15, 16);
        this.mapPosition = country.capitalPosition + new Vector2Int(offsetX, offsetY);
    }
}