using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using TycoonGame;

public class WorldGenerator : MonoBehaviour
{
    #region Configuration
    [Header("World Generation Settings")]
    public int worldSeed;
    public int minCountriesInWorld = 8;
    public int maxCountriesInWorld = 15;

    [Header("Balancing - Initial Country Attributes (GDD)")]
    [Tooltip("MIN and MAX values for a country's initial Risk Level.")]
    [Range(0f, 1f)] public float minInitialRisk = 0.1f;
    [Range(0f, 1f)] public float maxInitialRisk = 0.7f;

    [Tooltip("MIN and MAX values for a country's initial Investment Climate.")]
    [Range(0f, 1f)] public float minInitialClimate = 0.2f;
    [Range(0f, 1f)] public float maxInitialClimate = 0.8f;

    [Header("Procedural Naming (Optional)")]
    [SerializeField] private List<string> countryNameBases = new List<string> { "Aev", "Ast", "Ald", "Bryn", "Bel", "Cor", "Cym", "Cas", "Dorn", "El", "Eld", "Fen", "Gal", "Gwyn", "Hae", "Hol", "Ist", "Il", "Jor", "Kel", "Kov", "Kor", "Luth", "Lyr", "Mar", "Mor", "Nov", "Nort", "Ost", "Oph", "Pyr", "Quint", "Rhyn", "Ros", "Sten", "Silv", "Ser", "Sor", "Tor", "Tyl", "Umbr", "Val", "Verd", "Wes", "Wyc", "Yar", "Zan" };
    [SerializeField] private List<string> countryNameSuffixes = new List<string> { "ia", "a", "stan", "land", "gard", "grad", "burg", "mar", "os", "us", "ea", "ana", "dor", "eth", "or", "on", "ar" };

    [Header("Generated World Data")]
    public List<Country> world;
    #endregion

    public void GenerateWorld()
    {
        UnityEngine.Random.InitState(worldSeed);
        world = new List<Country>();

        // Used to generate distinct colors for each country.
        const float goldenRatioConjugate = 0.61803398875f;
        float currentHue = UnityEngine.Random.value;

        int numberOfCountries = UnityEngine.Random.Range(minCountriesInWorld, maxCountriesInWorld + 1);
        for (int i = 0; i < numberOfCountries; i++)
        {
            currentHue = (currentHue + goldenRatioConjugate) % 1.0f;
            Country newCountry = GenerateCountry(currentHue, i);
            world.Add(newCountry);
        }

        Debug.Log($"World generated with {world.Count} nations, according to the GDD. Seed: {worldSeed}");
    }

    private Country GenerateCountry(float hue, int id)
    {
        Country newCountry = new Country
        {
            countryID = id,
            countryName = GenerateCountryName(),
            mapColor = Color.HSVToRGB(hue, UnityEngine.Random.Range(0.75f, 0.95f), UnityEngine.Random.Range(0.85f, 1.0f)),

            // Generating the 3 key attributes defined in the GDD.
            riskLevel = UnityEngine.Random.Range(minInitialRisk, maxInitialRisk),
            investmentClimate = UnityEngine.Random.Range(minInitialClimate, maxInitialClimate),
            featuredSector = GetRandomEnumValue<Sector>()
        };

        return newCountry;
    }

    #region Helper Methods
    private T GetRandomEnumValue<T>() where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    private string GenerateCountryName()
    {
        if (countryNameBases.Count == 0 || countryNameSuffixes.Count == 0)
        {
            return "Terra Incognita";
        }
        
        string nameBase = countryNameBases[UnityEngine.Random.Range(0, countryNameBases.Count)];
        string suffix = countryNameSuffixes[UnityEngine.Random.Range(0, countryNameSuffixes.Count)];
        
        return $"{nameBase}{suffix}";
    }
    #endregion
}