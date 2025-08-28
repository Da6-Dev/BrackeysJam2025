using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestmentManager : MonoBehaviour
{
    public static InvestmentManager instance;

    [Header("Referências")]
    public GameManager gameManager;

    [Header("Atributos do Jogador")]
    public float playerMoney = 10000; // Exemplo

    [Header("Configurações de Investimento")]
    public float minInvestmentTime = 5f;
    public float maxInvestmentTime = 20f;

    // Estrutura para guardar um investimento em andamento
    private class Investment
    {
        public Country targetCountry;
        public float amount;
        public float duration;
        public float timeStarted;
        public Action<bool, float, Country> callback;
    }

    private List<Investment> activeInvestments = new List<Investment>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Atualizar todos os investimentos em andamento
        for (int i = activeInvestments.Count - 1; i >= 0; i--)
        {
            Investment inv = activeInvestments[i];
            if (Time.time - inv.timeStarted >= inv.duration)
            {
                bool success = InvestmentOutcome(inv.targetCountry);
                float profitOrLoss = CalculateReturn(inv.targetCountry, inv.amount, success);

                playerMoney += profitOrLoss; // Aplica o resultado ao saldo do jogador

                // Chama o callback
                inv.callback?.Invoke(success, profitOrLoss, inv.targetCountry);

                // Remove da lista de investimentos ativos
                activeInvestments.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Inicia um investimento em um país.
    /// </summary>
    public bool TryInvest(Country target, float amount, Action<bool, float, Country> onResult)
    {
        if (amount <= 0 || amount > playerMoney) return false;

        float duration = UnityEngine.Random.Range(minInvestmentTime, maxInvestmentTime);

        playerMoney -= amount;

        activeInvestments.Add(new Investment
        {
            targetCountry = target,
            amount = amount,
            duration = duration,
            timeStarted = Time.time,
            callback = onResult
        });

        return true;
    }

    /// <summary>
    /// Decide se o investimento foi bem-sucedido, baseado em fatores do país.
    /// </summary>
    private bool InvestmentOutcome(Country country)
    {
        // Exemplo
        float baseChance = 0.5f;
        baseChance += (country.politicalStability - 0.5f) * 0.6f;
        baseChance -= country.corruptionLevel * 0.4f;
        baseChance += (country.infrastructureLevel - 0.5f) * 0.2f;

        baseChance = Mathf.Clamp01(baseChance);
        return UnityEngine.Random.value <= baseChance;
    }

    /// <summary>
    /// Calcula o quanto o jogador ganhou ou perdeu.
    /// </summary>
    private float CalculateReturn(Country country, float amount, bool success)
    {
        float profitMultiplier = 1.2f + (country.infrastructureLevel * 0.4f) - (country.taxRate * 0.5f);
        float lossMultiplier = 0.5f + (country.corruptionLevel * 0.4f);

        if (success)
            return amount * (profitMultiplier - 1f); // Retorno positivo
        else
            return -amount * lossMultiplier; // Prejuízo
    }
}