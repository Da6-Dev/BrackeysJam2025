using UnityEngine;
using TycoonGame;
using System;

[Serializable]
public class Country
{
    [Header("Identification")]
    public string countryName;
    public int countryID;
    public Color mapColor;

    [Header("Map Data")]
    public Vector2Int capitalPosition; 

    [Header("Investment Attributes (GDD)")]
    [Tooltip("Modifies the success chance of projects. Higher values = more risky.")]
    [Range(0f, 1f)]
    public float riskLevel;

    [Tooltip("Modifies the cost and reward of projects. Higher values = more favorable.")]
    [Range(0f, 1f)]
    public float investmentClimate;

    [Tooltip("Gives a bonus to projects of a specific sector in this country.")]
    public Sector featuredSector;
}