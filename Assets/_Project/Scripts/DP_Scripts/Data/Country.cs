// Scripts/Data/Country.cs
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
public class Country
{
    [Header("Core Attributes")]
    public string countryName;
    public GovernmentType governmentType;
    [Range(0f, 1f)]
    public float politicalStability;

    [Header("Economic Attributes")]
    public List<SectorAffinity> sectorAffinities;
    [Tooltip("A estabilidade econômica geral do país, de 0.0 (hiperinflação/caos) a 1.0 (muito estável)")]
    [Range(0f, 1f)]
    public float economicStability;
    [Tooltip("O imposto sobre o lucro, de 0.0 (0%) a 1.0 (100%)")]
    [Range(0f, 1f)]
    public float taxRate;
    [Tooltip("O nível de qualidade da infraestrutura, de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float infrastructureLevel;
    [Tooltip("O recurso natural primário do país")]
    public NaturalResource primaryNaturalResource;
    [Tooltip("O nível de corrupção, de 0.0 (nenhuma) a 1.0 (máxima)")]
    [Range(0f, 1f)]
    public float corruptionLevel;

    [Header("Social Attributes")]
    [Tooltip("O nível educacional da população, de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float educationLevel;
    [Tooltip("O moral/felicidade geral da população, de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float populationMorale;

    [Header("Diplomacy & Politics")]
    [Tooltip("A política nacional que o governo está priorizando")]
    public NationalPolicy currentPolicy;
    [Tooltip("ID único do país no mundo.")]
    public int countryID;
    [Tooltip("Nome do bloco econômico ao qual o país pertence. Vazio se nenhum.")]
    public string economicBlocName;
    public Dictionary<int, DiplomaticStatus> diplomaticRelations;
    [Tooltip("A reputação do país no cenário global, de 0.0 (pária) a 1.0 (respeitado)")]
    [Range(0f, 1f)]
    public float internationalReputation; 

    [Header("Calculated Indicators")]
    [Tooltip("Nível de desenvolvimento geral, de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float developmentLevel;
    [Tooltip("O risco de eventos internos negativos (golpes, protestos), de 0.0 a 1.0")]
    [Range(0f, 1f)]
    public float internalEventsRisk;
    [Tooltip("Crescimento previsto para cada setor no próximo semestre. O valor é uma porcentagem (ex: 0.05 = +5%)")]
    public Dictionary<Sector, float> predictedSectorGrowth;

    [Header("Economic Output")]
    [Tooltip("A produção econômica real de cada setor, em bilhões.")]
    public Dictionary<Sector, float> sectorEconomicOutput;

    [Header("Map Attributes")]
    public Color mapColor;
    public Vector2Int capitalPosition;

    public Country()
    {
        sectorAffinities = new List<SectorAffinity>();
        diplomaticRelations = new Dictionary<int, DiplomaticStatus>();
        predictedSectorGrowth = new Dictionary<Sector, float>();
        sectorEconomicOutput = new Dictionary<Sector, float>(); 
    }
}