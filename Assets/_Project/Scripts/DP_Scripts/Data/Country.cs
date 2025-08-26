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

    [Header("Social Attributes")]
    [Tooltip("The educational level of the population, from 0.0 to 1.0")]
    [Range(0f, 1f)]
    public float educationLevel;
    [Tooltip("The general morale/happiness of the population, from 0.0 to 1.0")]
    [Range(0f, 1f)]
    public float populationMorale;

    [Header("Diplomacy & Politics")]
    [Tooltip("The national policy the government is currently prioritizing")]
    public NationalPolicy currentPolicy;
    [Tooltip("Unique ID for this country in the world.")]
    public int countryID;
    [Tooltip("Name of the economic bloc the country belongs to. Empty if none.")]
    public string economicBlocName;
    public Dictionary<int, DiplomaticStatus> diplomaticRelations;

    [Header("Map Attributes")]
    public Color mapColor;
    public Vector2Int capitalPosition;

    public Country()
    {
        sectorAffinities = new List<SectorAffinity>();
        diplomaticRelations = new Dictionary<int, DiplomaticStatus>();
    }
}