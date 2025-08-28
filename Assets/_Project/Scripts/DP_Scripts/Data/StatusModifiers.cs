// Scripts/Data/StatusModifier.cs
using UnityEngine;

// Enum para definir qual atributo o modificador afeta.
public enum StatType
{
    GDPGrowth,
    PoliticalStabilityChange,
    PopulationMoraleChange,
    MilitaryUpkeep,
    TechnologyGrowth
}

[System.Serializable]
public class StatusModifier
{
    public string Id; // Um nome único, ex: "war_economy_penalty"
    public StatType TargetStat;
    public float Value; // O valor do modificador (ex: -0.20 para -20%)
    public int SemestersRemaining;

    public StatusModifier(string id, StatType targetStat, float value, int duration)
    {
        Id = id;
        TargetStat = targetStat;
        Value = value;
        SemestersRemaining = duration;
    }
}