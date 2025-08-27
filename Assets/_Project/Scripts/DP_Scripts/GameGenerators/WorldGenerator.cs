// Scripts/Gameplay/WorldGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WorldGenerator : MonoBehaviour
{
    [Header("World Generation Settings")]
    [Tooltip("The seed used to generate the world. The same seed will always produce the same world.")]
    public int worldSeed;

    [Tooltip("The minimum number of countries that will be generated.")]
    public int minCountriesInWorld = 5;

    [Tooltip("The maximum number of countries that will be generated.")]
    public int maxCountriesInWorld = 10;

    [Header("Generated World Data")]
    public List<Country> world;

    [Header("Data for Single Country Testing")]
    public Country testCountry;

    // Naming rules and lists for procedural generation
    private Dictionary<GovernmentType, List<string>> countryNamingRules;
    private List<string> countryNameBases = new List<string> { "Aev", "Ast", "Ald", "Bryn", "Bel", "Cor", "Cym", "Cas", "Dorn", "El", "Eld", "Fen", "Gal", "Gwyn", "Hae", "Hol", "Ist", "Il", "Jor", "Kel", "Kov", "Kor", "Luth", "Lyr", "Mar", "Mor", "Nov", "Nort", "Ost", "Oph", "Pyr", "Quint", "Rhyn", "Ros", "Sten", "Silv", "Ser", "Sor", "Tor", "Tyl", "Umbr", "Val", "Verd", "Wes", "Wyc", "Yar", "Zan", "Aethel", "Albia", "Arden", "Avero", "Balar", "Calad", "Caspia", "Corin", "Dalen", "Devon", "Elessar", "Elysia", "Falken", "Gilead", "Harkan", "Ithil", "Javan", "Kaledon", "Lyonesse", "Midden", "Morvan", "Norden", "Orlan", "Pendor", "Quel", "Riven", "Rohan", "Suden", "Solas", "Taran", "Tirion", "Valen", "Wessex", "Zem" };
    private List<string> countryNameSuffixes = new List<string> { "ia", "ia", "ia", "a", "o", "stan", "land", "gard", "grad", "burg", "shire", "mar", "nes", "os", "us", "ea", "ana", "dor", "eth", "or", "on", "ar", "ia", "nia", "ria", "lia", "sia", "via" };

    private void Awake()
    {
        InitializeNamingRules();
    }

    #region Generation Logic

    /// <summary>
    /// Generates the entire game world, focusing on creating the countries.
    /// Company generation is called separately by the GameManager.
    /// </summary>
    public void GenerateWorld()
    {
        if (countryNamingRules == null) { InitializeNamingRules(); }

        UnityEngine.Random.InitState(worldSeed);
        if (world == null) { world = new List<Country>(); }
        world.Clear();

        const float goldenRatioConjugate = 0.61803398875f;
        float currentHue = UnityEngine.Random.value;

        int numberOfCountries = UnityEngine.Random.Range(minCountriesInWorld, maxCountriesInWorld + 1);
        for (int i = 0; i < numberOfCountries; i++)
        {
            currentHue = (currentHue + goldenRatioConjugate) % 1.0f;

            Country newCountry = GenerateCountry(currentHue);
            newCountry.countryID = i;
            world.Add(newCountry);
        }

        GenerateGeopolitics(world);
        Debug.Log($"World generated with {world.Count} nations, using Seed: {worldSeed}");
    }

    private Country GenerateCountry(float hue)
    {
        Country newCountry = new Country();

        newCountry.governmentType = GetRandomGovernmentType();
        newCountry.countryName = GenerateCountryName(newCountry.governmentType);
        newCountry.politicalStability = UnityEngine.Random.Range(0.2f, 0.9f);

        const float minBlueHue = 0.55f;
        const float maxBlueHue = 0.75f;

        if (hue >= minBlueHue && hue <= maxBlueHue)
        {
            hue = (hue + 0.3f) % 1.0f;
        }

        newCountry.mapColor = Color.HSVToRGB(hue, UnityEngine.Random.Range(0.75f, 0.95f), UnityEngine.Random.Range(0.85f, 1.0f));

        GenerateEconomicAttributes(newCountry);
        GenerateSocialAttributes(newCountry);
        newCountry.sectorAffinities = GenerateSectorAffinities(newCountry.governmentType, newCountry.primaryNaturalResource);

        return newCountry;
    }

    private void GenerateGeopolitics(List<Country> allCountries)
    {
        // --- Generate Economic Blocs ---
        List<string> blocNames = new List<string> { "Northern Trade Union", "Southern Economic Pact", "Meridian Alliance", "Oceanic Syndicate" };
        int numberOfBlocs = UnityEngine.Random.Range(2, (blocNames.Count / 2) + 2);

        for (int i = 0; i < numberOfBlocs; i++)
        {
            string blocName = blocNames[UnityEngine.Random.Range(0, blocNames.Count)];
            blocNames.Remove(blocName); // Ensure unique bloc names

            foreach (Country country in allCountries)
            {
                // If country is not already in a bloc, 50% chance to join this new one
                if (string.IsNullOrEmpty(country.economicBlocName) && UnityEngine.Random.value > 0.5f)
                {
                    country.economicBlocName = blocName;
                }
            }
        }

        // --- Generate Diplomatic Relations ---
        for (int i = 0; i < allCountries.Count; i++)
        {
            for (int j = i + 1; j < allCountries.Count; j++)
            {
                Country countryA = allCountries[i];
                Country countryB = allCountries[j];

                DiplomaticStatus status;

                // If countries are in the same economic bloc, they are likely allies or friendly
                bool inSameBloc = !string.IsNullOrEmpty(countryA.economicBlocName) && countryA.economicBlocName == countryB.economicBlocName;
                if (inSameBloc)
                {
                    status = (UnityEngine.Random.value > 0.5f) ? DiplomaticStatus.Ally : DiplomaticStatus.Friendly;
                }
                else
                {
                    status = GetRandomWeightedDiplomaticStatus();
                }

                countryA.diplomaticRelations[countryB.countryID] = status;
                countryB.diplomaticRelations[countryA.countryID] = status;
            }
        }
        Debug.Log("Diplomatic relations between all nations have been established.");
    }

    #endregion

    #region Private Helpers & Generators

    private void GenerateSocialAttributes(Country country)
    {
        switch (country.governmentType)
        {
            case GovernmentType.Democracy:
            case GovernmentType.ParliamentaryRepublic:
                country.populationMorale = UnityEngine.Random.Range(0.50f, 0.95f); // Generally happier populace
                country.educationLevel = UnityEngine.Random.Range(0.45f, 0.90f);
                break;
            case GovernmentType.Autocracy:
            case GovernmentType.MilitaryDictatorship:
                country.populationMorale = UnityEngine.Random.Range(0.20f, 0.60f); // Often oppressed/unhappy populace
                country.educationLevel = UnityEngine.Random.Range(0.25f, 0.70f);
                break;
            case GovernmentType.Monarchy:
                country.populationMorale = UnityEngine.Random.Range(0.40f, 0.85f);
                country.educationLevel = UnityEngine.Random.Range(0.30f, 0.75f);
                break;
            case GovernmentType.Technocracy:
                country.populationMorale = UnityEngine.Random.Range(0.35f, 0.75f);
                country.educationLevel = UnityEngine.Random.Range(0.60f, 0.98f); // Education is a top priority
                break;
        }

        Array policyValues = Enum.GetValues(typeof(NationalPolicy));
        country.currentPolicy = (NationalPolicy)policyValues.GetValue(UnityEngine.Random.Range(0, policyValues.Length));
    }

    private void GenerateEconomicAttributes(Country country)
    {
        switch (country.governmentType)
        {
            case GovernmentType.Democracy:
            case GovernmentType.ParliamentaryRepublic:
                country.corruptionLevel = UnityEngine.Random.Range(0.01f, 0.40f); // Tends to be less corrupt
                country.taxRate = UnityEngine.Random.Range(0.20f, 0.55f); // Tends to have higher taxes (public services)
                break;
            case GovernmentType.Autocracy:
            case GovernmentType.MilitaryDictatorship:
                country.corruptionLevel = UnityEngine.Random.Range(0.30f, 0.90f); // Tends to be more corrupt
                country.taxRate = UnityEngine.Random.Range(0.10f, 0.40f); // Taxes might be lower to keep elites happy
                break;
            case GovernmentType.Monarchy:
                country.corruptionLevel = UnityEngine.Random.Range(0.15f, 0.60f);
                country.taxRate = UnityEngine.Random.Range(0.15f, 0.50f);
                break;
            case GovernmentType.Technocracy:
                country.corruptionLevel = UnityEngine.Random.Range(0.05f, 0.25f); // Tends to be efficient and low-corruption
                country.taxRate = UnityEngine.Random.Range(0.30f, 0.65f); // High taxes to fund research
                break;
        }

        country.infrastructureLevel = UnityEngine.Random.Range(0.30f, 0.95f);
        Array resourceValues = Enum.GetValues(typeof(NaturalResource));
        country.primaryNaturalResource = (NaturalResource)resourceValues.GetValue(UnityEngine.Random.Range(0, resourceValues.Length));
    }

    private List<SectorAffinity> GenerateSectorAffinities(GovernmentType govType, NaturalResource resource)
    {
        List<SectorAffinity> affinities = new List<SectorAffinity>();

        foreach (Sector sector in Enum.GetValues(typeof(Sector)))
        {
            affinities.Add(new SectorAffinity
            {
                sector = sector,
                multiplier = UnityEngine.Random.Range(0.7f, 1.5f)
            });
        }

        // Give an extra bonus to a random sector
        int randomIndex = UnityEngine.Random.Range(0, affinities.Count);
        SectorAffinity bonusAffinity = affinities[randomIndex];
        bonusAffinity.multiplier *= 1.5f;
        affinities[randomIndex] = bonusAffinity;

        // --- Coherence Bonuses ---
        // Bonus by government type
        if (govType == GovernmentType.MilitaryDictatorship)
        {
            AdjustAffinity(ref affinities, Sector.Military, 0.5f);
        }
        if (govType == GovernmentType.Technocracy)
        {
            AdjustAffinity(ref affinities, Sector.SoftwareAndServices, 0.4f);
            AdjustAffinity(ref affinities, Sector.HardwareAndSemiconductors, 0.4f);
            AdjustAffinity(ref affinities, Sector.Biotechnology, 0.3f);
        }

        // Bonus by natural resource
        if (resource == NaturalResource.Oil || resource == NaturalResource.NaturalGas)
        {
            AdjustAffinity(ref affinities, Sector.FossilFuels, 0.6f);
        }
        if (resource == NaturalResource.FertileLand)
        {
            AdjustAffinity(ref affinities, Sector.Agriculture, 0.5f);
        }
        if (resource == NaturalResource.RareEarthMetals)
        {
            AdjustAffinity(ref affinities, Sector.HardwareAndSemiconductors, 0.5f);
        }

        return affinities;
    }

    private void AdjustAffinity(ref List<SectorAffinity> affinities, Sector sector, float bonus)
    {
        int index = affinities.FindIndex(a => a.sector == sector);
        if (index != -1)
        {
            // 1. LER: Pega uma cópia da struct.
            SectorAffinity affinity = affinities[index];

            // 2. MODIFICAR: Altera a propriedade na cópia local.
            affinity.multiplier += bonus;

            // 3. ESCREVER: Coloca a cópia modificada de volta na lista.
            affinities[index] = affinity;
        }
    }

    private string GenerateCountryName(GovernmentType govType)
    {
        List<string> prefixOptions = countryNamingRules[govType];
        string prefix = prefixOptions[UnityEngine.Random.Range(0, prefixOptions.Count)];
        string nameBase = countryNameBases[UnityEngine.Random.Range(0, countryNameBases.Count)];
        string suffix = countryNameSuffixes[UnityEngine.Random.Range(0, countryNameSuffixes.Count)];
        string finalName = $"{nameBase}{suffix}"; if (nameBase.EndsWith("a") && (suffix == "a" || suffix == "ia")) { finalName = nameBase; } else if (nameBase.EndsWith("ia") && suffix == "ia") { finalName = nameBase; }
        if (!string.IsNullOrEmpty(prefix)) { return $"{prefix} {finalName}"; } else { return finalName; }
    }

    private GovernmentType GetRandomGovernmentType()
    {
        Array values = Enum.GetValues(typeof(GovernmentType));
        return (GovernmentType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    private DiplomaticStatus GetRandomWeightedDiplomaticStatus()
    {
        float roll = UnityEngine.Random.value; // Random number between 0.0 and 1.0

        if (roll < 0.05f) return DiplomaticStatus.War;       // 5% chance
        if (roll < 0.20f) return DiplomaticStatus.Rival;     // 15% chance
        if (roll < 0.45f) return DiplomaticStatus.Tense;     // 25% chance
        if (roll < 0.70f) return DiplomaticStatus.Friendly;  // 25% chance
        return DiplomaticStatus.Neutral;                         // 30% chance
    }

    private void InitializeNamingRules()
    {
        countryNamingRules = new Dictionary<GovernmentType, List<string>>();
        countryNamingRules[GovernmentType.Democracy] = new List<string> { "Republic of", "Federal Republic of", "Federation of", "Free State of", "" };
        countryNamingRules[GovernmentType.Autocracy] = new List<string> { "State of", "Greater Empire of", "Dominion of", "People's Republic of", "" };
        countryNamingRules[GovernmentType.Technocracy] = new List<string> { "The Technate of", "Directive of", "Union of", "" };
        countryNamingRules[GovernmentType.Monarchy] = new List<string> { "Kingdom of", "Grand Duchy of", "Principality of", "Sultanate of", "Empire of", "" };
        countryNamingRules[GovernmentType.ParliamentaryRepublic] = new List<string> { "Republic of", "Commonwealth of", "Union of", "" };
        countryNamingRules[GovernmentType.MilitaryDictatorship] = new List<string> { "The Military Junta of", "Military State of", "Dominion of", "" };
    }

    #endregion

    #region Editor Tools & Debugging

    [ContextMenu("Generate Random Seed")]
    private void GenerateRandomSeed()
    {
        worldSeed = UnityEngine.Random.Range(0, 999999);
    }

    [ContextMenu("Generate New World (Editor)")]
    private void GenerateWorldForEditor()
    {
        GenerateWorld();
    }

    [ContextMenu("Print Diplomatic Relations to Console")]
    private void PrintDiplomaticRelations()
    {
        if (world == null || world.Count == 0)
        {
            Debug.LogWarning("World has not been generated yet! Please use 'Generate New World' first.");
            return;
        }

        Debug.Log("--- WORLD DIPLOMATIC RELATIONS ---");

        foreach (Country country in world)
        {
            Debug.Log($"\n--- Relations for {country.countryName} (Bloc: {country.economicBlocName ?? "None"}) ---");

            foreach (var relation in country.diplomaticRelations)
            {
                int otherCountryID = relation.Key;
                DiplomaticStatus status = relation.Value;

                Country otherCountry = world.FirstOrDefault(p => p.countryID == otherCountryID);
                if (otherCountry != null)
                {
                    Debug.Log($"   -> with {otherCountry.countryName}: {status}");
                }
            }
        }
        Debug.Log("\n--- END OF REPORT ---");
    }

    #endregion
}