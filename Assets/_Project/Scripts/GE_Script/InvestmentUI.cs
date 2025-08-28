using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InvestmentUI : MonoBehaviour
{
    public TMP_InputField amountInput;
    public Button investButton;
    public TextMeshProUGUI messageText;
    private Country selectedCountry;

    void Start()
    {
        investButton.onClick.AddListener(OnInvestClicked);
    }

    // Chama isso quando o jogador selecionar um país
    public void SetSelectedCountry(Country country)
    {
        selectedCountry = country;
        messageText.text = $"Investir em {country.countryName}";
    }

    void OnInvestClicked()
    {
        if (selectedCountry == null)
        {
            messageText.text = "Selecione um país!";
            return;
        }

        float amount;
        if (!float.TryParse(amountInput.text, out amount))
        {
            messageText.text = "Valor inválido!";
            return;
        }

        bool ok = InvestmentManager.instance.TryInvest(selectedCountry, amount, OnInvestmentResult);
        if (ok)
            messageText.text = $"Investimento de {amount:C} iniciado em {selectedCountry.countryName}!";
        else
            messageText.text = "Saldo insuficiente!";
    }

    void OnInvestmentResult(bool success, float profitOrLoss, Country country)
    {
        string result = success ?
            $"Seu investimento em {country.countryName} teve sucesso! Lucro de {profitOrLoss:C}." :
            $"Seu investimento em {country.countryName} falhou. Prejuízo de {Mathf.Abs(profitOrLoss):C}.";

        messageText.text = result + $"\nSaldo atual: {InvestmentManager.instance.playerMoney:C}";
    }
}