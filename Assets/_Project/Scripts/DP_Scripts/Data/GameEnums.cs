// Scripts/Data/GameEnums.cs

/// <summary>
/// Defines the possible government types for a country in the game.
/// </summary>
public enum GovernmentType
{
    Democracy,
    Autocracy,
    Technocracy,
    Monarchy,
    ParliamentaryRepublic,
    MilitaryDictatorship
}

/// <summary>
/// Defines the different economic sectors a company can belong to.
/// </summary>
public enum Sector 
{
    // Technology & Science
    SoftwareAndServices,
    HardwareAndSemiconductors,
    Biotechnology,

    // Industry & Manufacturing
    HeavyIndustry,
    ConsumerGoods,
    Automotive,

    // Resources & Agriculture
    Agriculture,
    FossilFuels,
    RenewableEnergy,

    // Services & Commerce
    Finance,
    Health,
    Retail,
    LogisticsAndTransport,
    RealEstate,
    
    // Governmental
    Military
}

/// <summary>
/// Defines the primary natural resources a country can possess.
/// </summary>
public enum NaturalResource
{
    None,
    Oil,
    Gold,
    RareEarthMetals,
    NaturalGas,
    FertileLand
}

/// <summary>
/// Defines the national policy a government is currently focused on.
/// </summary>
public enum NationalPolicy
{
    Neutrality,
    Militarism,
    Environmentalism,
    FreeMarket,
    Isolationism,
    ScientificFocus
}

/// <summary>
/// Defines the state of diplomatic relations between two countries.
/// </summary>
public enum DiplomaticStatus
{
    Ally,
    Friendly,
    Neutral,
    Tense,
    Rival,
    War
}