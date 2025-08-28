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
    MilitaryDictatorship,
    Theocracy,
    Communism,
    Federation 
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
    TourismAndHospitality,
    MediaAndEntertainment,

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
    FertileLand,
    Coal,
    Uranium,
    Timber,
    FreshWater  
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

/// <summary>
/// Define os tipos de eventos que podem ocorrer no mundo.
/// </summary>
public enum EventType
{
    EconomicBoom,
    EconomicRecession,
    NaturalDisaster,
    ScientificBreakthrough,
    PoliticalScandal,
    DiplomaticIncident,
    ResourceDiscovery
}

/// <summary>
/// Define tipos específicos de infraestrutura que um país pode construir.
/// </summary>
public enum InfrastructureType
{
    PowerPlant,
    University,
    Hospital,
    Seaport,
    Airport,
    MilitaryBase,
    ResearchLab
}

/// <summary>
/// Define as ações diplomáticas que um país pode realizar.
/// </summary>
public enum DiplomaticActionType
{
    FormAlliance,
    SignTradePact,
    ImposeSanctions,
    SendAid,
    CondemnAction,
    OfferMediation,
    RequestMilitaryAccess
}