// Scripts/Managers/CountrySimulationManager.cs
// Versão: 1.0.0 - "Balanced Fortress"
// Objetivo: Simulação macro de países extremamente robusta e equilibrada.
// Autor: ChatGPT (ajustes por Davi)
// Observações: Presume existência de classes/structs: Country, Modifier, StatType, WorldGenerator, TimeManager.
// Ajuste parâmetros públicos no inspector para tuning fino.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#region Helpers
public static class MathHelpers
{
    public static long SafeAddLong(long a, long b)
    {
        try { checked { return a + b; } }
        catch { return (b > 0) ? long.MaxValue : long.MinValue; }
    }

    public static long SafeMulLong(long a, double b)
    {
        try { checked { return (long)(a * b); } }
        catch { return (a >= 0) ? long.MaxValue : long.MinValue; }
    }

    public static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
}
#endregion

public class CountrySimulationManager : MonoBehaviour
{
    #region References
    [Header("Referências")]
    public WorldGenerator worldGenerator; // precisa preencher no inspector
    #endregion

    #region Global Cycle
    [Header("Ciclo Mundial")]
    [Tooltip("Velocidade das flutuações macro (Perlin).")]
    [Range(0f, 0.5f)] public float worldCycleSpeed = 0.04f;
    [Tooltip("Magnitude das flutuações macro.")]
    [Range(0f, 0.3f)] public float worldCycleMagnitude = 0.08f;
    private float perlinNoiseOffset;
    private float worldEconomicHealth = 1f; // multiplicador global
    #endregion

    #region Basic Inertia & Sensitivity
    [Header("Inércia e Sensibilidade")]
    [Tooltip("Taxa de suavização das mudanças (menor = mais inércia).")]
    [Range(0.01f, 0.5f)] public float stateLerp = 0.14f;
    [Tooltip("Sensibilidade geral da estabilidade a choques.")]
    [Range(0.01f, 1f)] public float stabilitySensitivity = 0.35f;
    #endregion

    #region Economic Tuning
    [Header("Ajustes Econômicos")]
    [Tooltip("Penalidade máxima aplicada na taxa de juros por dívida.")]
    [Range(0.0f, 0.25f)] public float maxDebtInterestPenalty = 0.12f;
    [Tooltip("Inflation generated from budget deficit (scale).")]
    [Range(0.0f, 0.12f)] public float inflationFromDeficit = 0.02f;
    [Tooltip("Taxa natural de retorno dos investimentos (semestres).")]
    [Range(0.0f, 0.2f)] public float baseInvestmentReturn = 0.015f;
    #endregion

    #region Soft Cap / Potential GDP
    [Header("Soft Cap / Potencial PIB")]
    [Tooltip("PIB per capita potencial base (moeda por habitante).")]
    public float baseGdpPerCapitaPotential = 25000f;
    [Tooltip("Quanto a tecnologia amplia o potencial per capita.")]
    [Range(0f, 5f)] public float techMultiplier = 1.6f;
    [Tooltip("Quanto a educação amplia o potencial per capita.")]
    [Range(0f, 5f)] public float educationMultiplier = 1.2f;
    [Tooltip("Agressividade da penalização quando próximo ao soft cap (0-1).")]
    [Range(0.0f, 1.0f)] public float softCapAggression = 0.55f;
    [Tooltip("Percentual acima do soft cap que ativa reversão gradual (ex: 0.05 = 5%).")]
    [Range(0.01f, 0.2f)] public float softCapOverdriveThreshold = 0.05f;
    [Tooltip("Velocidade da reversão quando em overdrive.")]
    [Range(0.0001f, 0.05f)] public float overdriveDecayRate = 0.006f;
    #endregion

    #region Hard Limits & Safety
    [Header("Hard Limits / Segurança")]
    public long MIN_GDP = 1_000_000L;          // 1 milhão
    public long MAX_GDP_HARD = 100_000_000_000_000L; // 100 trilhões (proteção)
    public long MIN_POP = 1_000L;
    public long MAX_POP = 1_500_000_000L;     // 1.5 bilhões
    public float MAX_INFLATION = 0.35f;
    public float MIN_INFLATION = -0.03f;
    public float MAX_UNEMPLOYMENT = 0.35f;
    public float NATURAL_UNEMPLOYMENT = 0.05f;
    public int MAX_DEBT_MULTIPLIER = 20;
    #endregion

    #region Shocks & Events
    [Header("Eventos / Shocks")]
    [Tooltip("Probabilidade semestral média de um choque moderado por país.")]
    [Range(0f, 0.5f)] public float shockProbability = 0.02f;
    [Tooltip("Máxima intensidade de um choque (0..1) relativo ao PIB).")]
    [Range(0f, 0.5f)] public float maxShockImpact = 0.08f;
    #endregion

    #region Fiscal Rules & Automatic Stabilizers
    [Header("Regras Fiscais")]
    [Tooltip("Se a dívida exceder (dívida/PIB) > this, aplicar ajuste fiscal gradual.")]
    [Range(0.5f, 5f)] public float debtToGdpWarning = 3f;
    [Tooltip("Percentual semestral de ajuste fiscal quando em zona de risco (0..0.1).")]
    [Range(0.0f, 0.05f)] public float fiscalAdjustmentSpeed = 0.01f;
    #endregion

    #region Debug / Logging
    [Header("Debug")]
    public bool enableLogging = false;
    public int logEveryNSteps = 4; // a cada N semestres
    private int stepCounter = 0;
    #endregion

    void Start()
    {
        perlinNoiseOffset = UnityEngine.Random.Range(0f, 10000f);
    }

    void OnEnable()
    {
        TimeManager.OnSemesterTick += HandleSemesterTick;
    }

    void OnDisable()
    {
        TimeManager.OnSemesterTick -= HandleSemesterTick;
    }

    private void HandleSemesterTick()
    {
        if (worldGenerator == null || worldGenerator.world == null || worldGenerator.world.Count == 0) return;
        UpdateWorldEconomicHealth();

        stepCounter++;
        foreach (var country in worldGenerator.world)
        {
            // Sanitize base values
            if (country.population <= 0) country.population = MIN_POP;
            country.population = Math.Clamp(country.population, MIN_POP, MAX_POP);
            if (country.gdp <= 0) country.gdp = MIN_GDP;
            country.gdp = Math.Clamp(country.gdp, MIN_GDP, MAX_GDP_HARD);
            if (country.taxRate < 0f) country.taxRate = 0f;

            // Collect modifiers safely
            var mods = CollectModifiers(country);

            // Compute resilience metric
            float resilience = ComputeResilience(country);

            // Apply modules (order chosen to be logical)
            PoliticalModule(country, mods, resilience);
            EconomicModule(country, mods, resilience);
            DemographicModule(country, mods, resilience);
            ExternalShocksModule(country);

            // Fiscal prudence: automatic adjustments if debt is too high
            FiscalPrudence(country);

            // Ensure final clamps and invariants
            FinalClamps(country);
        }

        if (enableLogging && stepCounter % Math.Max(1, logEveryNSteps) == 0)
        {
            LogTopEconomies(10);
        }
    }

    #region Modules Implementation

    private void UpdateWorldEconomicHealth()
    {
        float noise = Mathf.PerlinNoise(Time.time * worldCycleSpeed, perlinNoiseOffset);
        worldEconomicHealth = 1.0f + (noise - 0.5f) * 2f * worldCycleMagnitude;
        worldEconomicHealth = Mathf.Clamp(worldEconomicHealth, 0.75f, 1.3f);
    }

    private (float gdp, float stability, float morale, float military, float tech) CollectModifiers(Country country)
    {
        var collected = (gdp: 0f, stability: 0f, morale: 0f, military: 0f, tech: 0f);
        // Decrement safely and remove expired
        if (country.activeModifiers != null)
        {
            country.activeModifiers.RemoveAll(mod =>
            {
                mod.SemestersRemaining = Math.Max(0, mod.SemestersRemaining - 1);
                return mod.SemestersRemaining <= 0;
            });
            foreach (var mod in country.activeModifiers)
            {
                switch (mod.TargetStat)
                {
                    case StatType.GDPGrowth: collected.gdp += mod.Value; break;
                    case StatType.PoliticalStabilityChange: collected.stability += mod.Value; break;
                    case StatType.PopulationMoraleChange: collected.morale += mod.Value; break;
                    case StatType.MilitaryUpkeep: collected.military += mod.Value; break;
                    case StatType.TechnologyGrowth: collected.tech += mod.Value; break;
                }
            }
        }
        return collected;
    }

    private float ComputeResilience(Country c)
    {
        // Resiliência composta por desenvolvimento, infraestrutura, instituições e saúde ambiental
        float res = c.developmentLevel * 0.4f + c.infrastructureLevel * 0.35f + (1f - c.corruptionLevel) * 0.15f + c.environmentalHealth * 0.1f;
        return Mathf.Clamp01(res);
    }

    private void PoliticalModule(Country country, (float gdp, float stability, float morale, float military, float tech) mods, float resilience)
    {
        // Estabilidade política muda MUITO lentamente; política influencia corrupção e risco interno
        float stabilityChange = 0f;
        float debtRatio = country.gdp > 0 ? (float)country.nationalDebt / (float)country.gdp : 0f;

        stabilityChange -= debtRatio * 0.0055f;
        stabilityChange -= (country.unemploymentRate - NATURAL_UNEMPLOYMENT) * 0.012f;
        stabilityChange += (country.populationMorale - 0.5f) * 0.009f;
        stabilityChange += mods.stability * 0.5f;

        float targetStability = country.politicalStability + stabilityChange * stabilitySensitivity;
        float lerp = Mathf.Clamp(stateLerp * (stabilityChange < 0f ? (1f - resilience * 0.45f) : 1f), 0.001f, 0.45f);
        country.politicalStability = Mathf.Lerp(country.politicalStability, targetStability, lerp);

        // Corrupção afeta longamente e retroalimenta
        float corruptionDrift = (0.5f - country.politicalStability) * 0.0035f - country.internationalReputation * 0.0008f;
        country.corruptionLevel = Mathf.Clamp01(Mathf.Lerp(country.corruptionLevel, country.corruptionLevel + corruptionDrift, stateLerp * 0.4f));

        // Risco interno aumenta se estabilidade baixa e moral baixa
        country.internalRiskLevel = Mathf.Clamp01((1f - country.politicalStability) * 0.55f + (1f - country.populationMorale) * 0.3f);
    }

    private void EconomicModule(Country country, (float gdp, float stability, float morale, float military, float tech) mods, float resilience)
    {
        // ---- SAFE GDP and revenues ----
        long safeGdp = Math.Max(country.gdp, 1L);
        long gdpSem = Math.Max(1L, safeGdp / 2L);

        // Tax leakage moderated by corruption but with floor
        float taxLeakage = Mathf.Clamp01(1f - country.corruptionLevel * 0.22f);
        taxLeakage = Mathf.Max(0.5f, taxLeakage); // garante alguma arrecadação
        long receita = (long)(gdpSem * country.taxRate * taxLeakage);

        // Automatic stabilizer: imposto progressivo simples (mais riqueza => aumento marginal de taxa)
        float progressiveTax = 0f;
        float gdpPerCapita = (country.population > 0) ? (float)country.gdp / country.population : 1000f;
        progressiveTax = Mathf.Clamp01(Mathf.Log10(1f + gdpPerCapita) / 10f) * 0.05f; // até +5% em países muito ricos
        float effectiveTaxRate = Mathf.Clamp01(country.taxRate + progressiveTax);

        receita = (long)(gdpSem * effectiveTaxRate * taxLeakage);

        // ---- Expenditures ----
        long educationCost = (long)(gdpSem * country.educationSpendingRate * 0.2f);
        long healthCost = (long)(gdpSem * country.healthcareSpendingRate * 0.2f);
        long infraCost = (long)(gdpSem * country.infrastructureSpendingRate * 0.2f);

        // Administração pública cresce com população mas é parcialmente eficiente com desenvolvimento
        long adminCost = (long)(country.population * (80 + 300 * country.developmentLevel));

        // Defesa razoável
        long militaryCost = (long)(gdpSem * country.militarySpendingRate * 0.13f * (1 + mods.military));

        // ---- Debt interest and monetary stance ----
        float debtRatio = safeGdp > 0 ? (float)country.nationalDebt / (float)safeGdp : 0f;
        float debtInterestComponent = Mathf.Min(maxDebtInterestPenalty, debtRatio * 0.0175f);
        // Basic monetary rule: juros aumentam com inflação and internal risk
        float monetaryPenalty = Mathf.Clamp((country.inflationRate - 0.02f) * 0.3f + country.internalRiskLevel * 0.02f, 0f, 0.1f);
        float realInterestAnnual = 0.008f + debtInterestComponent + monetaryPenalty + country.corruptionLevel * 0.007f;
        realInterestAnnual = Mathf.Clamp(realInterestAnnual, 0.0f, 0.25f);
        long interestPayment = (long)Math.Min((double)country.nationalDebt, (double)(country.nationalDebt * (realInterestAnnual / 2f)));

        // ---- Budget and debt update ----
        long totalExpenses = educationCost + healthCost + infraCost + adminCost + militaryCost + interestPayment;

        long fiscalBalance = receita - totalExpenses; // positive = surplus

        // Debt evolves: if surplus -> reduce debt; if deficit -> increase debt
        long newDebt = country.nationalDebt - fiscalBalance;
        long maxDebt = Math.Min((long)((double)safeGdp * MAX_DEBT_MULTIPLIER), long.MaxValue);
        country.nationalDebt = Math.Max(0L, Math.Min(newDebt, maxDebt));

        // ---- Inflation dynamics (soft) ----
        float deficitRatio = (fiscalBalance < 0) ? (float)(-fiscalBalance) / Mathf.Max(1f, (float)gdpSem) : 0f;
        float unemploymentPush = (NATURAL_UNEMPLOYMENT - country.unemploymentRate) * 0.008f;
        // Soften impact: only a portion of deficit translates to inflation, plus mean-reversion
        country.inflationRate += deficitRatio * (inflationFromDeficit * 0.45f) - (country.inflationRate * 0.09f) + unemploymentPush;
        country.inflationRate = Mathf.Clamp(country.inflationRate, MIN_INFLATION, MAX_INFLATION);

        // ---- GDP growth model ----
        float techEffect = country.technologyLevel * techMultiplier;
        float eduEffect = country.educationLevel * educationMultiplier;
        float infraEffect = country.infrastructureLevel * 0.9f;

        float baseGrowth = (techEffect * 0.006f) + (eduEffect * 0.005f) + (infraEffect * 0.0035f);
        float stabilityBonus = (country.politicalStability - 0.5f) * 0.0035f;
        float corruptionPenalty = -country.corruptionLevel * 0.0085f;
        float debtPenalty = -debtRatio * 0.008f;
        float unemploymentPenalty = -(country.unemploymentRate - NATURAL_UNEMPLOYMENT) * 0.017f;

        float modifiersFromEvents = mods.gdp * 0.35f;

        // diminishing returns calibrated so países ricos ainda crescem, mas lento
        float diminishing = Mathf.Max(0.25f, 1.0f - (gdpPerCapita / 300000f));

        float rawGrowth = (baseGrowth + stabilityBonus + corruptionPenalty + debtPenalty + unemploymentPenalty + modifiersFromEvents) * worldEconomicHealth * diminishing * (1f - resilience * 0.55f);

        // Soft cap calculation (PIB potencial)
        double potentialPerCapita = baseGdpPerCapitaPotential * (1.0 + techEffect + eduEffect * 0.6);
        double softCapD = (double)country.population * potentialPerCapita;
        long softCap = (long)Math.Clamp(softCapD, MIN_GDP, MAX_GDP_HARD);

        float ratioToSoft = softCap > 0 ? Mathf.Clamp01((float)country.gdp / softCap) : 0f;
        float softPenalty = Mathf.Pow(ratioToSoft, 2f) * softCapAggression;
        float finalGrowth = rawGrowth * (1f - softPenalty);

        // Overdrive reversal
        if (ratioToSoft > 1f + softCapOverdriveThreshold)
        {
            float excess = ratioToSoft - 1f;
            float revert = Mathf.Clamp(excess * (float)overdriveDecayRate * 12f, 0f, 0.04f);
            finalGrowth -= revert;
        }

        // Bound growth semestral (negative allowed)
        finalGrowth = Mathf.Clamp(finalGrowth, -0.12f, 0.12f);

        long deltaGdp = (long)((double)country.gdp * finalGrowth);
        long tentativeGdp = country.gdp + deltaGdp;
        // Soft approach to hard cap
        if (tentativeGdp > MAX_GDP_HARD)
        {
            tentativeGdp = country.gdp + (long)((double)(MAX_GDP_HARD - country.gdp) * 0.1);
        }
        country.gdp = (long)Math.Clamp(tentativeGdp, MIN_GDP, MAX_GDP_HARD);

        // ---- Unemployment dynamics ----
        float naturalTarget = Mathf.Clamp(NATURAL_UNEMPLOYMENT - (country.educationLevel * 0.01f) + (country.developmentLevel * 0.005f), 0.02f, 0.08f);
        float unemploymentAdjustment = -finalGrowth * 0.4f; // crescimento reduz desemprego
        float unemploymentTarget = Mathf.Clamp(country.unemploymentRate + unemploymentAdjustment, 0f, MAX_UNEMPLOYMENT);
        // Blend towards both naturalTarget and unemploymentTarget slowly
        country.unemploymentRate = Mathf.Clamp(Mathf.Lerp(country.unemploymentRate, Mathf.Lerp(unemploymentTarget, naturalTarget, 0.02f), 0.06f), 0f, MAX_UNEMPLOYMENT);

        // ---- Investment & technology growth (endógeno e por modificadores) ----
        float investmentDrive = Mathf.Clamp01((float)(Math.Abs(fiscalBalance) / (double)gdpSem) * 0.5f) + Mathf.Clamp01(country.internationalReputation) * 0.1f;
        float techGrowth = Mathf.Clamp((country.technologyLevel * 0.002f) + (investmentDrive * 0.0015f) + mods.tech * 0.5f, -0.01f, 0.02f);
        country.technologyLevel = Mathf.Clamp01(country.technologyLevel + techGrowth * stateLerp);

        // Education improvements slowly
        country.educationLevel = Mathf.Clamp01(country.educationLevel + ((country.developmentLevel * 0.001f) + mods.tech * 0.0005f) * stateLerp);

        // Infrastructure depreciation & small investments
        country.infrastructureLevel = Mathf.Clamp01(country.infrastructureLevel + (country.infrastructureSpendingRate * 0.0008f - 0.0003f) * stateLerp);
    }

    private void DemographicModule(Country country, (float gdp, float stability, float morale, float military, float tech) mods, float resilience)
    {
        // Morale dynamics (lento)
        float moraleChange = 0f;
        moraleChange -= (country.unemploymentRate - NATURAL_UNEMPLOYMENT) * 0.018f;
        moraleChange -= (country.inflationRate - 0.02f) * 0.04f;
        moraleChange += (country.politicalStability - 0.5f) * 0.01f;
        moraleChange += mods.morale * 0.5f;
        float lerpM = Mathf.Clamp(stateLerp * (moraleChange < 0 ? (1f - resilience * 0.4f) : 1f), 0.001f, 0.45f);
        country.populationMorale = Mathf.Clamp01(Mathf.Lerp(country.populationMorale, country.populationMorale + moraleChange, lerpM));

        // Population growth (semestre) controlado
        float baseGrowth = country.populationGrowthRate * (1f - country.developmentLevel * 0.75f);
        float moraleBonus = (country.populationMorale - 0.5f) * 0.0016f;
        float popRate = Mathf.Clamp((baseGrowth + moraleBonus) / 2f, -0.02f, 0.02f); // -2% a +2% semestral
        long deltaPop = (long)(country.population * popRate);
        long newPop = country.population + deltaPop;
        country.population = (long)Math.Clamp(newPop, MIN_POP, MAX_POP);

        // Education & health interplay already handled in economic module tendencies
    }

    private void ExternalShocksModule(Country country)
    {
        // Probabilistic small shocks (ex.: desastre natural, crise financeira local)
        if (UnityEngine.Random.value < shockProbability)
        {
            // Magnitude uniforme, mas limitada pelo PIB
            float impactShare = UnityEngine.Random.Range(0.01f, maxShockImpact); // 1%..maxShock
            long shockLoss = (long)(country.gdp * impactShare);
            // Apply as negative modifier to GDP and morale
            country.gdp = Math.Max(MIN_GDP, country.gdp - shockLoss);
            country.populationMorale = Mathf.Clamp01(country.populationMorale - (0.03f + impactShare * 0.4f));
            // Increase debt slightly as response spending
            country.nationalDebt = Math.Min(country.nationalDebt + shockLoss / 4, country.gdp * MAX_DEBT_MULTIPLIER);
            // Slight inflation due to supply disruption
            country.inflationRate = Mathf.Clamp(country.inflationRate + 0.01f + impactShare * 0.05f, MIN_INFLATION, MAX_INFLATION);
        }
    }

    private void FiscalPrudence(Country country)
    {
        // Se dívida/PIB muito alta -> ajuste fiscal gradual (aumenta imposto efetivo e reduz gastos automáticos)
        float debtRatio = country.gdp > 0 ? (float)country.nationalDebt / (float)country.gdp : 0f;
        if (debtRatio > debtToGdpWarning)
        {
            // Aumenta taxRate lentamente (até um limite) e reduz alguns gastos muito lentamente
            float taxIncrease = fiscalAdjustmentSpeed * 0.5f;
            country.taxRate = Mathf.Clamp01(country.taxRate + taxIncrease * stateLerp);
            // Reduz gastos discricionários
            country.infrastructureSpendingRate = Mathf.Max(0.01f, country.infrastructureSpendingRate - fiscalAdjustmentSpeed * 0.3f * stateLerp);
            country.militarySpendingRate = Mathf.Max(0.01f, country.militarySpendingRate - fiscalAdjustmentSpeed * 0.25f * stateLerp);
        }
        else
        {
            // Pequena tendência a restaurar gasto público se dívida sob controle (flexibilidade)
            country.infrastructureSpendingRate = Mathf.Clamp(country.infrastructureSpendingRate + fiscalAdjustmentSpeed * 0.05f * stateLerp, 0.0f, 0.5f);
            country.militarySpendingRate = Mathf.Clamp(country.militarySpendingRate + fiscalAdjustmentSpeed * 0.02f * stateLerp, 0.0f, 0.5f);
        }
    }

    private void FinalClamps(Country country)
    {
        country.politicalStability = Mathf.Clamp01(country.politicalStability);
        country.internalRiskLevel = Mathf.Clamp01(country.internalRiskLevel);
        country.technologyLevel = Mathf.Clamp01(country.technologyLevel);
        country.developmentLevel = Mathf.Clamp01(country.developmentLevel);
        country.educationLevel = Mathf.Clamp01(country.educationLevel);
        country.infrastructureLevel = Mathf.Clamp01(country.infrastructureLevel);
        country.populationMorale = Mathf.Clamp01(country.populationMorale);
        country.environmentalHealth = Mathf.Clamp01(country.environmentalHealth);
        country.militaryStrength = Mathf.Clamp01(country.militaryStrength);
        country.internationalReputation = Mathf.Clamp01(country.internationalReputation);

        country.inflationRate = Mathf.Clamp(country.inflationRate, MIN_INFLATION, MAX_INFLATION);
        country.unemploymentRate = Mathf.Clamp(country.unemploymentRate, 0f, MAX_UNEMPLOYMENT);

        // Hard bounds on population / gdp / debt
        country.population = (long)Math.Clamp(country.population, MIN_POP, MAX_POP);
        country.gdp = (long)Math.Clamp(country.gdp, MIN_GDP, MAX_GDP_HARD);
        country.nationalDebt = (long)Math.Clamp(country.nationalDebt, 0L, country.gdp * MAX_DEBT_MULTIPLIER);
    }

    #endregion

    #region Utilities & Logging
    private void LogTopEconomies(int topN)
    {
        try
        {
            var top = worldGenerator.world.OrderByDescending(c => c.gdp).Take(topN).ToList();
            Debug.Log($"[CountrySim] Top {topN} economies at step {stepCounter}:");
            for (int i = 0; i < top.Count; i++)
            {
                var c = top[i];
                Debug.Log($"{i + 1}. {c.countryName} - GDP: {FormatLong(c.gdp)} | Pop: {FormatLong(c.population)} | Debt/PIB: {(c.gdp > 0 ? ((double)c.nationalDebt / c.gdp).ToString("P2") : "N/A")} | Infl: {c.inflationRate:P2} | Unemp: {c.unemploymentRate:P2}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CountrySim] LogTopEconomies falhou: " + e.Message);
        }
    }

    private string FormatLong(long v)
    {
        if (v >= 1_000_000_000_000L) return (v / 1_000_000_000_000.0).ToString("0.00") + "T";
        if (v >= 1_000_000_000L) return (v / 1_000_000_000.0).ToString("0.00") + "B";
        if (v >= 1_000_000L) return (v / 1_000_000.0).ToString("0.00") + "M";
        return v.ToString();
    }
    #endregion
}
