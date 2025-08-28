// Scripts/Gameplay/WorldUpdater.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldUpdater : MonoBehaviour
{
    private WorldEconomicState currentEconomicState;

    public void AdvanceSemester(List<Country> world)
    {
        // 1. DETERMINAR O ESTADO DA ECONOMIA GLOBAL (CHANCES MAIS EQUILIBRADAS)
        float roll = Random.value;
        if (roll < 0.15f) currentEconomicState = WorldEconomicState.Recession;  // 15% de chance
        else if (roll < 0.35f) currentEconomicState = WorldEconomicState.Stagnation; // 20% de chance
        else if (roll < 0.85f) currentEconomicState = WorldEconomicState.Growth;     // 50% de chance
        else currentEconomicState = WorldEconomicState.Boom;       // 15% de chance

        Debug.Log($"--- GLOBAL ECONOMIC STATE FOR THE SEMESTER: {currentEconomicState} ---");

        // 2. ATUALIZAR OS PAÍSES
        foreach (Country country in world)
        {
            UpdateBaseAttributes(country);
        }

        foreach (Country country in world)
        {
            UpdateCalculatedIndicators(country);
        }

        Debug.Log("SEMESTER ADVANCED: World state updated with RESILIENCE logic.");
    }

    private void UpdateBaseAttributes(Country country)
    {
        // Esta lógica base continua a mesma
        float stabilityChange = Random.Range(-0.05f, 0.05f);
        if (country.politicalStability > 0.8f) { stabilityChange -= 0.015f; }
        else if (country.politicalStability < 0.1f) { stabilityChange += 0.025f; }
        else if (country.politicalStability < 0.3f) { stabilityChange -= 0.02f; }
        country.politicalStability += stabilityChange;

        float moraleChange = Random.Range(-0.07f, 0.07f);
        if (country.populationMorale > 0.85f) { moraleChange -= 0.02f; }
        else if (country.populationMorale < 0.1f) { moraleChange += 0.03f; }
        else if (country.populationMorale < 0.25f) { moraleChange -= 0.03f; }
        country.populationMorale += moraleChange;

        float infraChange = Random.Range(-0.04f, 0.04f);
        if (country.infrastructureLevel > 0.75f) { infraChange -= (country.infrastructureLevel - 0.75f) * 0.05f; }
        country.infrastructureLevel += infraChange;

        country.educationLevel += Random.Range(-0.02f, 0.02f);
        country.corruptionLevel += Random.Range(-0.04f, 0.04f);

        country.politicalStability = Mathf.Clamp01(country.politicalStability);
        country.populationMorale = Mathf.Clamp01(country.populationMorale);
        country.infrastructureLevel = Mathf.Clamp01(country.infrastructureLevel);
        country.educationLevel = Mathf.Clamp01(country.educationLevel);
        country.corruptionLevel = Mathf.Clamp01(country.corruptionLevel);
    }

    private void UpdateCalculatedIndicators(Country country)
    {
        // 1. Nível de Desenvolvimento (Inalterado)
        country.developmentLevel = (country.infrastructureLevel + country.educationLevel) / 2.0f;

        // 2. Fator de Resiliência (Inalterado)
        // Este fator se torna AINDA MAIS IMPORTANTE com a nova lógica.
        float stabilityComponent = country.politicalStability;
        float corruptionComponent = 1.0f - country.corruptionLevel;
        float developmentComponent = country.developmentLevel;
        float resilienceFactor = (stabilityComponent + corruptionComponent + developmentComponent) / 3.0f;
        resilienceFactor = 0.5f + resilienceFactor; // Mapeia para o intervalo (0.5 a 1.5)
                                                    // Garantir que a resiliência não seja zero para evitar divisão por zero.
        resilienceFactor = Mathf.Max(0.1f, resilienceFactor);


        // 3. Estabilidade Econômica (LÓGICA REVISADA)
        // Fatores internos do país
        float internalEconChange = Random.Range(-0.06f, 0.06f); // Variação interna um pouco reduzida
        internalEconChange -= country.corruptionLevel * 0.1f;
        if (country.politicalStability < 0.4f)
        {
            internalEconChange -= (0.4f - country.politicalStability) * 0.2f;
        }

        // Fator da economia global
        float globalEconModifier = 0f;
        switch (currentEconomicState)
        {
            case WorldEconomicState.Boom: globalEconModifier = 0.07f; break;
            case WorldEconomicState.Growth: globalEconModifier = 0.035f; break;
            case WorldEconomicState.Stagnation: globalEconModifier = -0.035f; break;
            case WorldEconomicState.Recession: globalEconModifier = -0.07f; break;
        }

        // << MUDANÇA PRINCIPAL 1: IMPACTO GLOBAL INVERSO À RESILIÊNCIA >>
        // Em vez de multiplicar, agora DIVIDIMOS o impacto global pela resiliência.
        // Países fortes (resiliência > 1) reduzem o impacto global.
        // Países fracos (resiliência < 1) amplificam o impacto global.
        float finalGlobalImpact = globalEconModifier / resilienceFactor;

        country.economicStability += internalEconChange + finalGlobalImpact;
        country.economicStability = Mathf.Clamp01(country.economicStability);


        // 4. Risco e Reputação (Inalterado)
        float invertedStability = 1.0f - country.politicalStability;
        float invertedMorale = 1.0f - country.populationMorale;
        country.internalEventsRisk = Mathf.Clamp01((invertedStability + invertedMorale) / 1.5f + country.corruptionLevel * 0.2f);
        float reputationChange = Random.Range(-0.04f, 0.04f);
        if (country.politicalStability < 0.3f) { reputationChange -= 0.05f; }
        country.internationalReputation += reputationChange;
        country.internationalReputation = Mathf.Clamp01(country.internationalReputation);

        // 5. Crescimento dos Setores (LÓGICA REVISADA)
        List<Sector> sectorsToUpdate = country.predictedSectorGrowth.Keys.ToList();
        foreach (Sector sector in sectorsToUpdate)
        {
            var affinity = country.sectorAffinities.Find(a => a.sector == sector);

            // Fatores internos do setor
            float baseGrowth = (affinity.multiplier - 1.0f) * 0.05f;
            float economicFactor = (country.economicStability - 0.5f) * 0.20f;
            float randomFactor = Random.Range(-0.05f, 0.05f);

            // Fator global
            float globalFactor = 0f;
            switch (currentEconomicState)
            {
                case WorldEconomicState.Boom: globalFactor = 0.05f; break;
                case WorldEconomicState.Growth: globalFactor = 0.02f; break;
                case WorldEconomicState.Stagnation: globalFactor = -0.02f; break;
                case WorldEconomicState.Recession: globalFactor = -0.05f; break;
            }

            // << MUDANÇA PRINCIPAL 2: A MESMA LÓGICA APLICADA AO CRESCIMENTO DOS SETORES >>
            float modifiedGlobalFactor = globalFactor / resilienceFactor;

            // 5A: Previsão de Crescimento
            float finalPredictedGrowth = baseGrowth + economicFactor + modifiedGlobalFactor + randomFactor;
            country.predictedSectorGrowth[sector] = finalPredictedGrowth;

            // 5B: Crescimento Real
            float uncertainty = 0.04f;
            float modifiedUncertainty = uncertainty / resilienceFactor;
            float realityShock = Random.Range(-modifiedUncertainty, modifiedUncertainty);
            float actualGrowth = finalPredictedGrowth + realityShock;

            if (country.sectorEconomicOutput.ContainsKey(sector))
            {
                float currentOutput = country.sectorEconomicOutput[sector];
                currentOutput *= (1.0f + actualGrowth);
                country.sectorEconomicOutput[sector] = Mathf.Max(0.1f, currentOutput);
            }
        }
    }
}