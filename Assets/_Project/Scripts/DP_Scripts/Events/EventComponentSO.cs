using UnityEngine;
using System.Collections.Generic;

// Enum para o estado de um atributo (alto, baixo, etc.)
public enum StatThreshold
{
    VeryLow,  // < 20%
    Low,      // < 40%
    High,     // > 60%
    VeryHigh  // > 80%
}

/// <summary>
/// Define todos os atributos de um país que podem ser verificados para disparar um evento.
/// </summary>
public enum TriggeringStat
{
    PoliticalStability,
    PopulationMorale,
    TechnologyLevel,
    EducationLevel,
    MilitaryStrength,
    GDPPerCapita,
    NationalDebtRatio,
    UnemploymentRate,
    InflationRate,
    EnvironmentalHealth,
    InternalRiskLevel,
    InternationalReputation,
    CorruptionLevel
}


[CreateAssetMenu(fileName = "NewEventComponent", menuName = "Strategy Game/Procedural Event Component", order = 1)]
public class EventComponentSO : ScriptableObject
{
    [Header("Event Type")]
    [Tooltip("Marque se este evento envolve dois países (um Ator e um Alvo).")]
    public bool isInterCountryEvent;

    [Header("Actor/Single Country Conditions")]
    [Tooltip("O atributo que o país Ator (ou único país) deve ter.")]
    public TriggeringStat Stat;
    [Tooltip("O limite (alto/baixo) para o atributo do Ator.")]
    public StatThreshold Threshold;

    [Header("Target Country Conditions (Inter-Country Only)")]
    [Tooltip("A relação diplomática necessária entre os dois países.")]
    public DiplomaticStatus requiredRelation;
    [Tooltip("O atributo que o país Alvo deve ter.")]
    public TriggeringStat targetStat;
    [Tooltip("O limite (alto/baixo) para o atributo do Alvo.")]
    public StatThreshold targetThreshold;

    [Header("Consequence")]
    public string GenericEventName;
    
    [Tooltip("Use {ACTOR} e {TARGET} para eventos inter-nações. Ex: '{ACTOR} fechou um acordo com {TARGET}.'")]
    [TextArea(2, 4)]
    public string CauseDescriptionFragment;

    [Header("Effects (Modifiers)")]
    [Tooltip("Modificadores aplicados ao Ator (ou único país).")]
    public List<StatusModifier> ActorEffectModifiers;
    
    [Tooltip("Modificadores aplicados ao Alvo (apenas para eventos inter-nações).")]
    public List<StatusModifier> TargetEffectModifiers;
}