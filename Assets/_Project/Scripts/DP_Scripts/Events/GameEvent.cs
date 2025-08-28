// Scripts/Events/GameEvent.cs
using UnityEngine;
using System.Collections.Generic;

// Define os tipos de eventos possíveis
public enum EventType
{
    Internal,       // Afeta apenas 1 país
    International   // Afeta 2 países
}

// Classe que define um evento e seus efeitos
[System.Serializable]
public class GameEvent
{
    public string eventName;
    [TextArea(3,5)]
    public string eventDescription;
    public EventType type;

    [Header("Trigger Conditions")]
    [Tooltip("Chance base de ocorrer a cada semestre se as condições forem atendidas (0.0 a 1.0)")]
    public float baseTriggerChance;

    [Tooltip("A chance do evento aumenta com o risco de eventos internos do país.")]
    public bool scalesWithInternalRisk;

    [Header("Effects on Target Country")]
    public float politicalStabilityModifier;
    public float economicStabilityModifier;
    public float populationMoraleModifier;
    public float internationalReputationModifier;

    // Construtor para facilitar a criação de eventos via código
    public GameEvent(string name, string desc, EventType eventType, float chance, bool scales, float polStab, float econStab, float morale, float reputation)
    {
        eventName = name;
        eventDescription = desc;
        type = eventType;
        baseTriggerChance = chance;
        scalesWithInternalRisk = scales;
        politicalStabilityModifier = polStab;
        economicStabilityModifier = econStab;
        populationMoraleModifier = morale;
        internationalReputationModifier = reputation;
    }
}