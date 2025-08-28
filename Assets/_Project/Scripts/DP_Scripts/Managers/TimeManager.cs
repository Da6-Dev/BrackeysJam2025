using UnityEngine;
using System;

/// <summary>
/// Controla o avanço do tempo de simulação em semestres.
/// Não possui nenhuma lógica de UI ou de estado de jogo.
/// </summary>
public class TimeManager : MonoBehaviour
{
    /// <summary>
    /// O semestre/turno atual da simulação.
    /// </summary>
    public int currentSemester { get; private set; }

    /// <summary>
    /// Evento estático que é disparado quando um semestre avança.
    /// Outros sistemas (como WorldEventManager) devem "ouvir" este evento.
    /// </summary>
    public static event Action OnSemesterTick;

    void Start()
    {
        // A simulação começa no semestre 1.
        currentSemester = 1;
    }

    /// <summary>
    /// Avança a simulação em um semestre e notifica todos os sistemas.
    /// Esta função deve ser chamada por uma fonte externa (como um botão de UI ou o GameManager).
    /// </summary>
    public void AdvanceSemester()
    {
        currentSemester++;
        
        Debug.Log($"--- TICK DE SIMULAÇÃO --- Semestre: {currentSemester}");
        
        // Dispara o evento para notificar os outros sistemas que o tempo passou.
        OnSemesterTick?.Invoke();
    }
}