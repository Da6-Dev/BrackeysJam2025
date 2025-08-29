using UnityEngine;
using UnityEngine.UI;

public class OpportunityDisplay : MonoBehaviour
{
    private InvestmentOpportunity opportunityData;

    public void Initialize(InvestmentOpportunity data)
    {
        this.opportunityData = data;
    }

    /// <summary>
    /// This method is linked to the Button component's OnClick event.
    /// It now opens the details panel instead of just logging to the console.
    /// </summary>
    public void OnIconClicked()
    {
        if (opportunityData == null) return;

        // Use the singleton instance of the panel to display our data.
        ProjectDetailsPanel.instance.DisplayOpportunity(opportunityData);
    }
}