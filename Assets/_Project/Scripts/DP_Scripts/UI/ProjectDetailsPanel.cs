using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

[RequireComponent(typeof(CanvasGroup))] // Ensures this object always has a CanvasGroup
public class ProjectDetailsPanel : MonoBehaviour
{
    public static ProjectDetailsPanel instance;

    [Header("UI References")]
    public TextMeshProUGUI projectNameText;
    public TextMeshProUGUI sectorText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statsText;
    public Button closeButton;

    private CanvasGroup canvasGroup; // Reference to the CanvasGroup component

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Get the CanvasGroup component on this object
        canvasGroup = GetComponent<CanvasGroup>();

        closeButton.onClick.AddListener(HidePanel);
    }

    public void DisplayOpportunity(InvestmentOpportunity opportunity)
    {
        // --- CALCULATIONS (No changes here) ---
        Country hostCountry = opportunity.hostCountry;
        float climateModifier = 1.0f + (0.5f * (0.5f - hostCountry.investmentClimate)); 
        float finalCost = opportunity.baseCost * climateModifier;
        float finalReward = opportunity.baseReward * (1 / climateModifier);
        float riskModifier = 1.0f - (hostCountry.riskLevel * 0.5f);
        float finalSuccessChance = opportunity.baseSuccessChance * riskModifier;
        bool sectorBonus = hostCountry.featuredSector == opportunity.sector;
        if (sectorBonus)
        {
            finalSuccessChance *= 1.20f;
        }

        // --- POPULATE UI TEXTS (No changes here) ---
        projectNameText.text = opportunity.projectName;
        sectorText.text = $"Sector: {opportunity.sector}";
        descriptionText.text = opportunity.description;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>Host Country:</b> {hostCountry.countryName}");
        sb.AppendLine();
        sb.AppendLine("<b>Base Stats:</b>");
        sb.AppendLine($"  Cost: ${opportunity.baseCost:F1}M");
        sb.AppendLine($"  Reward: ${opportunity.baseReward:F1}M");
        sb.AppendLine($"  Success Chance: {opportunity.baseSuccessChance:P0}");
        sb.AppendLine();
        sb.AppendLine("<b>Country Modifiers:</b>");
        sb.AppendLine($"  Investment Climate: {hostCountry.investmentClimate:P0}");
        sb.AppendLine($"  Risk Level: {hostCountry.riskLevel:P0}");
        if (sectorBonus)
        {
            sb.AppendLine($"  <color=green>Featured Sector Bonus!</color>");
        }
        sb.AppendLine();
        sb.AppendLine("<b><u>Final Calculation:</u></b>");
        sb.AppendLine($"  <b>Final Cost: ${finalCost:F1}M</b>");
        sb.AppendLine($"  <b>Final Success Chance: {Mathf.Clamp01(finalSuccessChance):P0}</b>");
        statsText.text = sb.ToString();

        // --- SHOW PANEL (This part is changed) ---
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HidePanel()
    {
        // --- HIDE PANEL (This part is changed) ---
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}