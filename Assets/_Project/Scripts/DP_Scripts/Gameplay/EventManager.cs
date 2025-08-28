// Scripts/Gameplay/EventManager.cs
using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    private List<GameEvent> eventPool;

    void Awake()
    {
        InitializeEventPool();
    }

    // Processa todos os eventos para o mundo no semestre atual
    public void ProcessEvents(List<Country> world)
    {
        foreach (Country country in world)
        {
            CheckAndTriggerInternalEvent(country);
        }
        // Futuramente, a lógica de eventos internacionais poderia ser adicionada aqui
    }

    private void CheckAndTriggerInternalEvent(Country country)
    {
        foreach (GameEvent gameEvent in eventPool)
        {
            if (gameEvent.type != EventType.Internal) continue;

            float finalChance = gameEvent.baseTriggerChance;
            if (gameEvent.scalesWithInternalRisk)
            {
                // A chance aumenta significativamente com o risco do país
                finalChance *= (1.0f + country.internalEventsRisk * 2.0f);
            }

            if (Random.value < finalChance)
            {
                ApplyEvent(country, gameEvent);
                break; // Garante que apenas um evento ocorra por país por semestre para não sobrecarregar
            }
        }
    }

    private void ApplyEvent(Country country, GameEvent gameEvent)
    {
        Debug.Log($"<color=yellow>EVENTO!</color> Em {country.countryName}: {gameEvent.eventName}. {gameEvent.eventDescription}");

        country.politicalStability += gameEvent.politicalStabilityModifier;
        country.economicStability += gameEvent.economicStabilityModifier;
        country.populationMorale += gameEvent.populationMoraleModifier;
        country.internationalReputation += gameEvent.internationalReputationModifier;
        
        // Garante que os valores não saiam do intervalo 0-1
        country.politicalStability = Mathf.Clamp01(country.politicalStability);
        country.economicStability = Mathf.Clamp01(country.economicStability);
        country.populationMorale = Mathf.Clamp01(country.populationMorale);
        country.internationalReputation = Mathf.Clamp01(country.internationalReputation);
    }
    
    // Aqui definimos a lista de todos os eventos possíveis no jogo.
    // Isso poderia ser carregado de um arquivo (JSON, ScriptableObjects) para maior flexibilidade.
    private void InitializeEventPool()
    {
        eventPool = new List<GameEvent>
        {
            // Eventos Positivos
            new GameEvent(
                "Avanço Científico", 
                "Pesquisadores locais fizeram uma descoberta revolucionária!", 
                EventType.Internal, 0.01f, false, 
                0.05f, 0.03f, 0.05f, 0.08f
            ),
            new GameEvent(
                "Safra Recorde", 
                "Condições climáticas favoráveis e novas tecnologias resultaram em uma colheita excepcional.", 
                EventType.Internal, 0.02f, false,
                0.0f, 0.10f, 0.07f, 0.02f
            ),

            // Eventos Negativos
            new GameEvent(
                "Protestos em Massa", 
                "A insatisfação popular leva a grandes manifestações nas principais cidades.", 
                EventType.Internal, 0.03f, true,
                -0.15f, -0.05f, -0.10f, -0.05f
            ),
            new GameEvent(
                "Escândalo de Corrupção", 
                "Um grande esquema de corrupção envolvendo altos funcionários do governo foi revelado.", 
                EventType.Internal, 0.02f, true, 
                -0.20f, -0.10f, -0.15f, -0.15f
            ),
            new GameEvent(
                "Desastre Natural", 
                "Um desastre natural de grandes proporções atingiu uma região densamente povoada.", 
                EventType.Internal, 0.005f, false, 
                -0.05f, -0.15f, -0.10f, 0.01f // Reputação pode subir devido à ajuda internacional
            )
        };
    }
}