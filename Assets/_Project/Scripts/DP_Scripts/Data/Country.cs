using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public struct SectorAffinity
{
    public Sector sector;
    public float multiplier;
}

[Serializable]
public struct SectorGrowth
{
    public Sector sector;
    [Tooltip("Previsão de crescimento para o semestre, de -100% (-1.0) a +100% (1.0)")]
    [Range(-1f, 1f)]
    public float growthForecast;
}

[Serializable]
public class Country
{
    [Header("Core Attributes")]
    public string countryName;
    public GovernmentType governmentType;
    [Range(0f, 1f)]
    public float politicalStability;
    [Tooltip("O risco de acontecerem eventos internos negativos (protestos, crises), de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float internalRiskLevel;

    [Header("Economic Attributes")]
    [Tooltip("Produto Interno Bruto (PIB) - a medida principal do tamanho da economia.")]
    public long gdp;
    [Tooltip("A dívida total do governo. Afeta o orçamento através do pagamento de juros.")]
    public long nationalDebt;
    [Tooltip("O nível geral de avanço tecnológico do país, influenciando a produtividade e o exército.")]
    [Range(0f, 1f)]
    public float technologyLevel;
    public List<SectorAffinity> sectorAffinities;
    public List<SectorGrowth> sectorGrowthForecasts;
    [Tooltip("O nível geral de desenvolvimento do país, de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float developmentLevel;
    [Tooltip("A taxa de inflação, afetando a estabilidade econômica. 0.0 é ideal, 1.0 é muito alta.")]
    [Range(0f, 1f)]
    public float inflationRate;
    [Tooltip("The profit tax rate, from 0.0 (0%) to 1.0 (100%)")]
    [Range(0f, 1f)]
    public float taxRate;
    [Tooltip("The quality level of the nation's infrastructure, from 0.0 to 1.0")]
    [Range(0f, 1f)]
    public float infrastructureLevel;
    [Tooltip("The country's primary natural resource")]
    public NaturalResource primaryNaturalResource;
    [Tooltip("The level of corruption, from 0.0 (none) to 1.0 (maximum)")]
    [Range(0f, 1f)]
    public float corruptionLevel;

    [Header("Social & Demographics")]
    [Tooltip("Número total de habitantes do país.")]
    public long population;
    [Tooltip("A taxa de crescimento da população (pode ser negativa).")]
    public float populationGrowthRate;
    [Tooltip("A porcentagem da força de trabalho que está desempregada.")]
    [Range(0f, 1f)] // ADICIONADO
    public float unemploymentRate;
    [Tooltip("The educational level of the population, from 0.0 to 1.0")]
    [Range(0f, 1f)]
    public float educationLevel;
    [Tooltip("The general morale/happiness of the population, from 0.0 to 1.0")]
    [Range(0f, 1f)]
    public float populationMorale;

    [Header("Government Budget Allocation")]
    [Tooltip("Porcentagem da receita do governo alocada para Educação. Afeta o nível de educação.")]
    [Range(0.01f, 0.5f)]
    public float educationSpendingRate = 0.15f; // NOVO

    [Tooltip("Porcentagem da receita do governo alocada para Saúde. Afeta o crescimento populacional e moral.")]
    [Range(0.01f, 0.5f)]
    public float healthcareSpendingRate = 0.20f; // NOVO

    [Tooltip("Porcentagem da receita do governo alocada para Infraestrutura. Afeta o crescimento do PIB.")]
    [Range(0.01f, 0.5f)]
    public float infrastructureSpendingRate = 0.10f; // NOVO

    [Tooltip("Porcentagem da receita do governo alocada para as Forças Armadas. Afeta a força militar.")]
    [Range(0.01f, 0.5f)]
    public float militarySpendingRate = 0.08f;

    [Header("Environment")]
    [Tooltip("A saúde do meio ambiente. 1.0 é um ambiente intocado, 0.0 é devastado.")]
    [Range(0f, 1f)] // ADICIONADO
    public float environmentalHealth;

    [Header("Military")]
    [Tooltip("Uma medida geral da força e capacidade militar do país.")]
    public float militaryStrength;

    [Header("Diplomacy & Politics")]
    [Tooltip("A reputação e 'soft power' do país no cenário mundial, de 0.0 a 1.0.")]
    [Range(0f, 1f)]
    public float internationalReputation;
    [Tooltip("The national policy the government is currently prioritizing")]
    public NationalPolicy currentPolicy;
    [Tooltip("Unique ID for this country in the world.")]
    public int countryID;
    [Tooltip("Name of the economic bloc the country belongs to. Empty if none.")]
    public string economicBlocName;
    public Dictionary<int, DiplomaticStatus> diplomaticRelations;

    // A LISTA DE EVENTOS ATIVOS FOI REMOVIDA DAQUI

    [Header("Map Attributes")]
    public Color mapColor;
    public Vector2Int capitalPosition;

    [Header("Modificadores Ativos")]
    public List<StatusModifier> activeModifiers;

    public Country()
    {
        sectorAffinities = new List<SectorAffinity>();
        sectorGrowthForecasts = new List<SectorGrowth>();
        diplomaticRelations = new Dictionary<int, DiplomaticStatus>();
        activeModifiers = new List<StatusModifier>();
        // A inicialização da lista de eventos foi removida daqui
    }
}