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
        
        // --- Geração de Cor ---
        const float minBlueHue = 0.55f;
        const float maxBlueHue = 0.75f;
        if (hue >= minBlueHue && hue <= maxBlueHue)
        {
            hue = (hue + 0.3f) % 1.0f;
        }
        newCountry.mapColor = Color.HSVToRGB(hue, UnityEngine.Random.Range(0.75f, 0.95f), UnityEngine.Random.Range(0.85f, 1.0f));

        // --- Lógica de Geração Interconectada --- // ALTERADO
        // A ordem agora é importante para que os valores possam depender uns dos outros.
        GenerateSocialAttributes(newCountry);
        GenerateEconomicAttributes(newCountry);
        GenerateMilitaryAndTechAttributes(newCountry);
        GenerateEnvironmentalAttributes(newCountry);
        GenerateInitialBudget(newCountry); // ADICIONADO: Define o orçamento inicial

        newCountry.sectorAffinities = GenerateSectorAffinities(newCountry.governmentType, newCountry.primaryNaturalResource);

        // Clamp final para garantir que nenhum valor saia do intervalo 0-1
        newCountry.developmentLevel = Mathf.Clamp01(newCountry.developmentLevel);
        newCountry.technologyLevel = Mathf.Clamp01(newCountry.technologyLevel);
        newCountry.environmentalHealth = Mathf.Clamp01(newCountry.environmentalHealth);

        return newCountry;
    }

    private void GenerateGeopolitics(List<Country> allCountries)
    {
        // ... (Nenhuma mudança necessária aqui, seu código existente é bom)
        List<string> blocNames = new List<string> { "Northern Trade Union", "Southern Economic Pact", "Meridian Alliance", "Oceanic Syndicate" };
        int numberOfBlocs = UnityEngine.Random.Range(2, (blocNames.Count / 2) + 2);
        for (int i = 0; i < numberOfBlocs; i++)
        {
            string blocName = blocNames[UnityEngine.Random.Range(0, blocNames.Count)];
            blocNames.Remove(blocName);
            foreach (Country country in allCountries)
            {
                if (string.IsNullOrEmpty(country.economicBlocName) && UnityEngine.Random.value > 0.5f)
                {
                    country.economicBlocName = blocName;
                }
            }
        }
        for (int i = 0; i < allCountries.Count; i++)
        {
            for (int j = i + 1; j < allCountries.Count; j++)
            {
                Country countryA = allCountries[i];
                Country countryB = allCountries[j];
                DiplomaticStatus status;
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
        country.population = UnityEngine.Random.Range(1_000_000, 150_000_000);
        country.politicalStability = UnityEngine.Random.Range(0.3f, 0.8f);

        switch (country.governmentType)
        {
            case GovernmentType.Democracy:
            case GovernmentType.ParliamentaryRepublic:
            case GovernmentType.Federation: // CORRIGIDO: Adicionado caso faltante
                country.populationMorale = UnityEngine.Random.Range(0.50f, 0.95f);
                country.educationLevel = UnityEngine.Random.Range(0.45f, 0.90f);
                country.unemploymentRate = UnityEngine.Random.Range(0.03f, 0.10f);
                break;
            case GovernmentType.Autocracy:
            case GovernmentType.MilitaryDictatorship:
                country.populationMorale = UnityEngine.Random.Range(0.20f, 0.60f);
                country.educationLevel = UnityEngine.Random.Range(0.25f, 0.70f);
                country.unemploymentRate = UnityEngine.Random.Range(0.08f, 0.25f);
                break;
            case GovernmentType.Monarchy:
                country.populationMorale = UnityEngine.Random.Range(0.40f, 0.85f);
                country.educationLevel = UnityEngine.Random.Range(0.30f, 0.75f);
                country.unemploymentRate = UnityEngine.Random.Range(0.05f, 0.15f);
                break;
            case GovernmentType.Technocracy:
                country.populationMorale = UnityEngine.Random.Range(0.35f, 0.75f);
                country.educationLevel = UnityEngine.Random.Range(0.60f, 0.98f);
                country.unemploymentRate = UnityEngine.Random.Range(0.05f, 0.12f);
                break;
            case GovernmentType.Theocracy: // CORRIGIDO: Adicionado caso faltante
                country.populationMorale = UnityEngine.Random.Range(0.45f, 0.90f); // Moral alta por fé
                country.educationLevel = UnityEngine.Random.Range(0.20f, 0.65f); // Educação focada em religião, não ciência
                country.unemploymentRate = UnityEngine.Random.Range(0.06f, 0.18f);
                break;
            case GovernmentType.Communism: // CORRIGIDO: Adicionado caso faltante
                country.populationMorale = UnityEngine.Random.Range(0.30f, 0.70f);
                country.educationLevel = UnityEngine.Random.Range(0.40f, 0.85f);
                country.unemploymentRate = UnityEngine.Random.Range(0.01f, 0.05f); // Oficialmente, desemprego é baixo
                break;
        }
        
        country.populationGrowthRate = UnityEngine.Random.Range(-0.005f, 0.015f) + ((country.populationMorale - 0.5f) / 10f);
        
        Array policyValues = Enum.GetValues(typeof(NationalPolicy));
        country.currentPolicy = (NationalPolicy)policyValues.GetValue(UnityEngine.Random.Range(0, policyValues.Length));
    }

    private void GenerateEconomicAttributes(Country country)
    {
        // Infraestrutura e Desenvolvimento são baseados em Educação para mais realismo
        country.infrastructureLevel = country.educationLevel * UnityEngine.Random.Range(0.8f, 1.2f);
        country.developmentLevel = (country.infrastructureLevel + country.educationLevel) / 2f * UnityEngine.Random.Range(0.9f, 1.1f);
        country.infrastructureLevel = Mathf.Clamp01(country.infrastructureLevel);
        country.developmentLevel = Mathf.Clamp01(country.developmentLevel);

        switch (country.governmentType)
        {
            // ... casos existentes ...
            case GovernmentType.Democracy:
            case GovernmentType.ParliamentaryRepublic:
            case GovernmentType.Federation: // CORRIGIDO
                country.corruptionLevel = UnityEngine.Random.Range(0.01f, 0.40f);
                country.taxRate = UnityEngine.Random.Range(0.20f, 0.55f);
                break;
            case GovernmentType.Autocracy:
            case GovernmentType.MilitaryDictatorship:
                country.corruptionLevel = UnityEngine.Random.Range(0.30f, 0.90f);
                country.taxRate = UnityEngine.Random.Range(0.10f, 0.40f);
                break;
            case GovernmentType.Monarchy:
                country.corruptionLevel = UnityEngine.Random.Range(0.15f, 0.60f);
                country.taxRate = UnityEngine.Random.Range(0.15f, 0.50f);
                break;
            case GovernmentType.Technocracy:
                country.corruptionLevel = UnityEngine.Random.Range(0.05f, 0.25f);
                country.taxRate = UnityEngine.Random.Range(0.30f, 0.65f);
                break;
            case GovernmentType.Theocracy: // CORRIGIDO
                country.corruptionLevel = UnityEngine.Random.Range(0.25f, 0.75f);
                country.taxRate = UnityEngine.Random.Range(0.10f, 0.30f); // Imposto baixo, baseado em doações/dízimos
                break;
            case GovernmentType.Communism: // CORRIGIDO
                country.corruptionLevel = UnityEngine.Random.Range(0.20f, 0.80f);
                country.taxRate = UnityEngine.Random.Range(0.40f, 0.80f); // Imposto/taxa estatal alta
                break;
        }

        // PIB e Dívida são calculados com base em outros fatores
        long gdpPerCapita = (long)(UnityEngine.Random.Range(2000, 70000) * country.developmentLevel);
        country.gdp = country.population * gdpPerCapita;
        country.nationalDebt = (long)(country.gdp * UnityEngine.Random.Range(0.1f, 1.6f));
        country.inflationRate = UnityEngine.Random.Range(0.01f, 0.05f);

        Array resourceValues = Enum.GetValues(typeof(NaturalResource));
        country.primaryNaturalResource = (NaturalResource)resourceValues.GetValue(UnityEngine.Random.Range(0, resourceValues.Length));
    }

    private void GenerateMilitaryAndTechAttributes(Country country)
    {
        // Nível tecnológico é fortemente influenciado pela educação
        float baseTech = country.educationLevel * UnityEngine.Random.Range(0.8f, 1.2f);
        if (country.governmentType == GovernmentType.Technocracy) baseTech += 0.15f;
        country.technologyLevel = Mathf.Clamp01(baseTech);

        // Força militar depende do PIB, tecnologia e política
        float gdpFactor = (float)country.gdp / 2_000_000_000_000f; // 0.5 de força por trilhão de PIB
        float techFactor = country.technologyLevel * 0.5f;
        country.militaryStrength = (gdpFactor + techFactor) / 2f;

        if (country.governmentType == GovernmentType.MilitaryDictatorship) country.militaryStrength *= 1.5f;
        if (country.currentPolicy == NationalPolicy.Militarism) country.militaryStrength *= 1.3f;
        country.militaryStrength = Mathf.Clamp01(country.militaryStrength);
    }

    private void GenerateEnvironmentalAttributes(Country country)
    {
        // Saúde ambiental é inversamente proporcional ao desenvolvimento
        float industrialPenalty = country.developmentLevel * UnityEngine.Random.Range(0.6f, 1.1f);
        country.environmentalHealth = 1.0f - industrialPenalty;

        if (country.primaryNaturalResource == NaturalResource.Oil || country.primaryNaturalResource == NaturalResource.Coal)
            country.environmentalHealth -= 0.2f;

        if (country.currentPolicy == NationalPolicy.Environmentalism)
            country.environmentalHealth += 0.25f;
            
        country.environmentalHealth = Mathf.Clamp01(country.environmentalHealth);
    }
    
    // ADICIONADO: Nova função para definir o orçamento inicial de forma balanceada
    private void GenerateInitialBudget(Country country)
    {
        country.educationSpendingRate = UnityEngine.Random.Range(0.12f, 0.20f);
        country.healthcareSpendingRate = UnityEngine.Random.Range(0.15f, 0.25f);
        country.infrastructureSpendingRate = UnityEngine.Random.Range(0.08f, 0.15f);
        country.militarySpendingRate = UnityEngine.Random.Range(0.05f, 0.18f);

        if (country.governmentType == GovernmentType.MilitaryDictatorship)
            country.militarySpendingRate *= 1.8f;
        if (country.governmentType == GovernmentType.Technocracy)
            country.infrastructureSpendingRate *= 1.5f;
    }

    private List<SectorAffinity> GenerateSectorAffinities(GovernmentType govType, NaturalResource resource)
    {
        // ... (Nenhuma mudança necessária aqui, seu código existente é bom)
        List<SectorAffinity> affinities = new List<SectorAffinity>();
        foreach (Sector sector in Enum.GetValues(typeof(Sector)))
        {
            affinities.Add(new SectorAffinity { sector = sector, multiplier = UnityEngine.Random.Range(0.7f, 1.5f) });
        }
        int randomIndex = UnityEngine.Random.Range(0, affinities.Count);
        SectorAffinity bonusAffinity = affinities[randomIndex];
        bonusAffinity.multiplier *= 1.5f;
        affinities[randomIndex] = bonusAffinity;
        if (govType == GovernmentType.MilitaryDictatorship) { AdjustAffinity(ref affinities, Sector.Military, 0.5f); }
        if (govType == GovernmentType.Technocracy)
        {
            AdjustAffinity(ref affinities, Sector.SoftwareAndServices, 0.4f);
            AdjustAffinity(ref affinities, Sector.HardwareAndSemiconductors, 0.4f);
            AdjustAffinity(ref affinities, Sector.Biotechnology, 0.3f);
        }
        if (resource == NaturalResource.Oil || resource == NaturalResource.NaturalGas) { AdjustAffinity(ref affinities, Sector.FossilFuels, 0.6f); }
        if (resource == NaturalResource.FertileLand) { AdjustAffinity(ref affinities, Sector.Agriculture, 0.5f); }
        if (resource == NaturalResource.RareEarthMetals) { AdjustAffinity(ref affinities, Sector.HardwareAndSemiconductors, 0.5f); }
        return affinities;
    }
    
    // ... (restante do código: AdjustAffinity, GenerateCountryName, etc. permanece o mesmo)

    private void AdjustAffinity(ref List<SectorAffinity> affinities, Sector sector, float bonus)
    {
        int index = affinities.FindIndex(a => a.sector == sector);
        if (index != -1)
        {
            SectorAffinity affinity = affinities[index];
            affinity.multiplier += bonus;
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
        float roll = UnityEngine.Random.value;
        if (roll < 0.05f) return DiplomaticStatus.War;
        if (roll < 0.20f) return DiplomaticStatus.Rival;
        if (roll < 0.45f) return DiplomaticStatus.Tense;
        if (roll < 0.70f) return DiplomaticStatus.Friendly;
        return DiplomaticStatus.Neutral;
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
        countryNamingRules[GovernmentType.Theocracy] = new List<string> { "Holy State of", "The Theocracy of", "Divine Mandate of", "" };
        countryNamingRules[GovernmentType.Communism] = new List<string> { "People's Socialist Republic of", "Workers' State of", "Union of", "" };
        countryNamingRules[GovernmentType.Federation] = new List<string> { "United Federation of", "Federation of", "Confederation of", "" };
    }

    #endregion

    #region Editor Tools & Debugging

    [ContextMenu("Generate Random Seed")]
    private void GenerateRandomSeed() { worldSeed = UnityEngine.Random.Range(0, 999999); }

    [ContextMenu("Generate New World (Editor)")]
    private void GenerateWorldForEditor() { GenerateWorld(); }

    [ContextMenu("Print Diplomatic Relations to Console")]
    private void PrintDiplomaticRelations()
    {
        if (world == null || world.Count == 0) { Debug.LogWarning("World has not been generated yet!"); return; }
        Debug.Log("--- WORLD DIPLOMATIC RELATIONS ---");
        foreach (Country country in world)
        {
            Debug.Log($"\n--- Relations for {country.countryName} (Bloc: {country.economicBlocName ?? "None"}) ---");
            foreach (var relation in country.diplomaticRelations)
            {
                Country otherCountry = world.FirstOrDefault(p => p.countryID == relation.Key);
                if (otherCountry != null) { Debug.Log($"   -> with {otherCountry.countryName}: {relation.Value}"); }
            }
        }
        Debug.Log("\n--- END OF REPORT ---");
    }

    #endregion
}