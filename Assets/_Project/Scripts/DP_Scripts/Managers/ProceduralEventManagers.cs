// Scripts/Managers/ProceduralEventManager.cs (CORRIGIDO)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProceduralEventManager : MonoBehaviour
{
    [Header("Referências")]
    public WorldGenerator worldGenerator;

    [Header("Biblioteca de Regras")]
    public List<EventComponentSO> eventComponents;

    [Header("Configurações de Geração")]
    [Tooltip("A chance de um evento interno ser gerado para um país.")]
    [Range(0f, 1f)]
    public float chanceOfInternalEvent = 0.15f;

    [Tooltip("A chance de um evento entre dois países ser verificado.")]
    [Range(0f, 1f)]
    public float chanceOfInterCountryEvent = 0.10f;

    // Constantes para os limites
    private const float VERY_LOW_THRESHOLD = 0.2f;
    private const float LOW_THRESHOLD = 0.4f;
    private const float HIGH_THRESHOLD = 0.6f;
    private const float VERY_HIGH_THRESHOLD = 0.8f;

    // REMOVIDO: As definições de Enum foram movidas para o arquivo EventEnums.cs
    // para evitar erros e melhorar a organização do código.

    void OnEnable() { TimeManager.OnSemesterTick += HandleSemesterTick; }
    void OnDisable() { TimeManager.OnSemesterTick -= HandleSemesterTick; }

    private void HandleSemesterTick()
    {
        if (worldGenerator?.world == null || !eventComponents.Any()) return;

        List<Country> countries = worldGenerator.world;

        foreach (Country country in countries)
        {
            if (Random.value <= chanceOfInternalEvent)
            {
                GenerateAndTriggerInternalEvent(country);
            }
        }

        if (countries.Count < 2) return;

        for (int i = 0; i < countries.Count; i++)
        {
            for (int j = i + 1; j < countries.Count; j++)
            {
                if (Random.value <= chanceOfInterCountryEvent)
                {
                    Country countryA = countries[i];
                    Country countryB = countries[j];
                    
                    TryTriggerInterCountryEvent(countryA, countryB);
                    TryTriggerInterCountryEvent(countryB, countryA);
                }
            }
        }
    }

    private void GenerateAndTriggerInternalEvent(Country country)
    {
        var validComponents = eventComponents
            .Where(c => !c.isInterCountryEvent && AreConditionsMet(country, c.Stat, c.Threshold))
            .ToList();

        if (validComponents.Any())
        {
            EventComponentSO chosenComponent = validComponents[Random.Range(0, validComponents.Count)];
            ApplyModifiers(country, chosenComponent.ActorEffectModifiers);
            Debug.Log($"EVENTO INTERNO em {country.countryName}: {chosenComponent.GenericEventName}! {chosenComponent.CauseDescriptionFragment}");
        }
    }

    private void TryTriggerInterCountryEvent(Country actor, Country target)
    {
        if (!actor.diplomaticRelations.TryGetValue(target.countryID, out DiplomaticStatus currentRelation))
        {
            currentRelation = DiplomaticStatus.Neutral;
        }

        var validComponents = eventComponents
            .Where(c => c.isInterCountryEvent &&
                        c.requiredRelation == currentRelation &&
                        AreConditionsMet(actor, c.Stat, c.Threshold) &&
                        AreConditionsMet(target, c.targetStat, c.targetThreshold))
            .ToList();

        if (validComponents.Any())
        {
            EventComponentSO chosenComponent = validComponents[Random.Range(0, validComponents.Count)];
            
            ApplyModifiers(actor, chosenComponent.ActorEffectModifiers);
            ApplyModifiers(target, chosenComponent.TargetEffectModifiers);

            string description = chosenComponent.CauseDescriptionFragment
                .Replace("{ACTOR}", actor.countryName)
                .Replace("{TARGET}", target.countryName);

            Debug.LogWarning($"EVENTO MUNDIAL ({chosenComponent.GenericEventName}): {description}");
        }
    }

    private void ApplyModifiers(Country country, List<StatusModifier> modifiers)
    {
        foreach (var modTemplate in modifiers)
        {
            StatusModifier newModifier = new StatusModifier(
                modTemplate.Id,
                modTemplate.TargetStat,
                modTemplate.Value,
                modTemplate.SemestersRemaining
            );
            country.activeModifiers.Add(newModifier);
        }
    }

    private bool AreConditionsMet(Country country, TriggeringStat stat, StatThreshold threshold)
    {
        float statValue = GetStatValue(country, stat);
        switch (threshold)
        {
            case StatThreshold.VeryLow: return statValue < VERY_LOW_THRESHOLD;
            case StatThreshold.Low: return statValue < LOW_THRESHOLD;
            case StatThreshold.High: return statValue > HIGH_THRESHOLD;
            case StatThreshold.VeryHigh: return statValue > VERY_HIGH_THRESHOLD;
            default: return false;
        }
    }

    private float GetStatValue(Country country, TriggeringStat stat)
    {
        switch (stat)
        {
            case TriggeringStat.PoliticalStability: return country.politicalStability;
            case TriggeringStat.PopulationMorale: return country.populationMorale;
            case TriggeringStat.TechnologyLevel: return country.technologyLevel;
            case TriggeringStat.EducationLevel: return country.educationLevel;
            case TriggeringStat.MilitaryStrength: return country.militaryStrength;
            case TriggeringStat.GDPPerCapita:
                return (country.population > 0) ? (float)country.gdp / country.population : 0;
            case TriggeringStat.NationalDebtRatio:
                return (country.gdp > 0) ? (float)country.nationalDebt / country.gdp : 0;
            case TriggeringStat.UnemploymentRate: return country.unemploymentRate;
            case TriggeringStat.InflationRate: return country.inflationRate;
            case TriggeringStat.EnvironmentalHealth: return country.environmentalHealth;
            case TriggeringStat.InternalRiskLevel: return country.internalRiskLevel;
            case TriggeringStat.InternationalReputation: return country.internationalReputation;
            case TriggeringStat.CorruptionLevel: return country.corruptionLevel;
            default:
                Debug.LogWarning($"GetStatValue não tem um caso para o TriggeringStat: {stat}");
                return 0f;
        }
    }
}