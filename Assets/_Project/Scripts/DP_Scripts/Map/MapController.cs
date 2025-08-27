using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using TMPro;

// Implementamos interfaces para detectar cliques, arrastos e scroll do mouse na área do mapa.
public class MapController : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
{
    [Header("Referências Essenciais")]
    [Tooltip("Referência ao script MapGenerator para obter dados do mapa.")]
    public MapGenerator mapGenerator;

    [Tooltip("O RectTransform da imagem RawImage que exibe o mapa. Este é o objeto que será movido e escalado.")]
    public RectTransform mapDisplayImage;

    [Tooltip("O GameObject que serve como 'viewport' para o mapa. Ele será ativado e desativado.")]
    public GameObject mapContainer;

    [Tooltip("O componente TextMeshPro para exibir informações do país clicado.")]
    public TextMeshProUGUI countryInfoText;

    [Header("Configurações de Controle")]
    [Tooltip("Velocidade do zoom aplicado com a roda do mouse.")]
    public float zoomSpeed = 0.5f;
    [Tooltip("Nível mínimo de zoom permitido (1 = 100% do tamanho original).")]
    public float minZoom = 1f;
    [Tooltip("Nível máximo de zoom permitido.")]
    public float maxZoom = 10f;
    [Tooltip("Zoom inicial quando o mapa é aberto.")]
    public float initialZoom = 2f;

    // Referência privada ao RectTransform do contêiner para calcular os limites de movimento.
    private RectTransform viewportRectTransform;
    private bool isMapVisible = false;

    #region Ciclo de Vida Unity

    void Awake()
    {
        // Validação crucial para garantir que as referências foram atribuídas no Inspector.
        if (mapDisplayImage == null || mapContainer == null || mapGenerator == null || countryInfoText == null)
        {
            Debug.LogError("Uma ou mais referências essenciais não foram definidas no Inspector do MapController! O script será desativado.");
            this.enabled = false;
            return;
        }

        viewportRectTransform = mapContainer.GetComponent<RectTransform>();
    }

    void Start()
    {
        // Garante que o mapa comece escondido e o texto de informação esteja limpo.
        mapContainer.SetActive(false);
        countryInfoText.text = "";
        isMapVisible = false;
    }

    void Update()
    {
        // Adiciona um atalho (ESC) para fechar o mapa se ele estiver visível.
        if (isMapVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMapView();
        }
    }

    // Usamos LateUpdate para garantir que a posição do mapa seja corrigida após
    // todos os cálculos de zoom e arrasto do frame.
    void LateUpdate()
    {
        if (isMapVisible)
        {
            ClampMapPosition();
        }
    }

    #endregion

    #region Interação com UI

    /// <summary>
    /// Alterna a visibilidade do mapa. Esta função deve ser chamada pelo botão "Abrir Mapa".
    /// </summary>
    public void ToggleMapView()
    {
        isMapVisible = !isMapVisible;
        mapContainer.SetActive(isMapVisible);

        // ADIÇÃO CRÍTICA AQUI:
        // Ativa ou desativa o objeto de texto junto com o mapa.
        if (countryInfoText != null)
        {
            countryInfoText.gameObject.SetActive(isMapVisible);
        }

        if (isMapVisible)
        {
            ResetMapView();
        }
        else
        {
            // O texto já é desativado pela linha acima,
            // mas limpamos o conteúdo por segurança.
            countryInfoText.text = "";
        }
    }

    /// <summary>
    /// Reseta a posição, escala e informações do mapa para o estado padrão.
    /// </summary>
    private void ResetMapView()
    {
        mapDisplayImage.localScale = Vector3.one * initialZoom;
        mapDisplayImage.anchoredPosition = Vector2.zero;
        countryInfoText.text = "";
        ClampMapPosition(); // Garante que a posição inicial já respeite os limites.
    }

    #endregion

    #region Handlers de Eventos do Mouse

    /// <summary>
    /// Chamado quando a roda do mouse é rolada sobre o mapa.
    /// Aplica zoom na direção do cursor do mouse.
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;
        float currentZoom = mapDisplayImage.localScale.x;
        float newZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);

        // Se o zoom não mudou (atingiu o limite), não faz nada.
        if (Mathf.Approximately(newZoom, currentZoom)) return;

        // Calcula a posição do mouse relativa ao viewport para centralizar o zoom.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);

        // Calcula o deslocamento necessário para manter o ponto sob o mouse fixo.
        Vector2 pivotToMouse = (mapDisplayImage.anchoredPosition - localMousePosition);
        float zoomRatio = newZoom / currentZoom;
        Vector2 newAnchoredPosition = localMousePosition + pivotToMouse * zoomRatio;

        // Aplica a nova escala e a posição corrigida.
        mapDisplayImage.localScale = Vector3.one * newZoom;
        mapDisplayImage.anchoredPosition = newAnchoredPosition;
    }

    /// <summary>
    /// Chamado quando o mouse é arrastado sobre o mapa.
    /// Move (pan) a imagem do mapa.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // O `eventData.delta` já nos dá o deslocamento do mouse desde o último frame.
        mapDisplayImage.anchoredPosition += eventData.delta;
    }

    /// <summary>
    /// Chamado quando um clique é detectado no mapa.
    /// Converte a posição do clique para coordenadas do mapa e exibe as informações do país.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignora o clique se foi parte de um arrasto.
        if (eventData.dragging) return;

        // Converte a posição da tela para um ponto local no RectTransform da imagem do mapa.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        // Converte o ponto local para coordenadas UV (0 a 1).
        float uvX = (localPoint.x / mapDisplayImage.rect.width) + mapDisplayImage.pivot.x;
        float uvY = (localPoint.y / mapDisplayImage.rect.height) + mapDisplayImage.pivot.y;

        // Converte as coordenadas UV para as coordenadas da matriz do mapa.
        int mapX = (int)(uvX * mapGenerator.mapWidth);
        int mapY = (int)(uvY * mapGenerator.mapHeight);

        // Valida se as coordenadas estão dentro dos limites do mapa.
        if (mapX < 0 || mapX >= mapGenerator.mapWidth || mapY < 0 || mapY >= mapGenerator.mapHeight) return;

        // Obtém o índice do país e atualiza o texto.
        int countryIndex = mapGenerator.RegionMap[mapX, mapY];
        UpdateCountryInfoText(countryIndex);
    }

    #endregion

    #region Métodos Auxiliares

    /// <summary>
    /// Garante que a imagem do mapa não seja arrastada para fora da área visível (viewport).
    /// </summary>
    private void ClampMapPosition()
    {
        Vector2 viewportSize = viewportRectTransform.rect.size;
        Vector2 scaledMapSize = mapDisplayImage.rect.size * mapDisplayImage.localScale.x;

        // Calcula a área máxima que o mapa pode se mover em X e Y.
        // Se o mapa for menor que o viewport, o limite é 0 (centralizado).
        float maxX = Mathf.Max(0, (scaledMapSize.x - viewportSize.x) / 2f);
        float maxY = Mathf.Max(0, (scaledMapSize.y - viewportSize.y) / 2f);

        Vector2 currentPos = mapDisplayImage.anchoredPosition;

        // Restringe a posição atual dentro dos limites calculados.
        float clampedX = Mathf.Clamp(currentPos.x, -maxX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, -maxY, maxY);

        mapDisplayImage.anchoredPosition = new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Atualiza o painel de texto com as informações do país selecionado.
    /// </summary>
    private void UpdateCountryInfoText(int countryIndex)
    {
        if (countryIndex != -1 && countryIndex < mapGenerator.WorldData.Count)
        {
            Country clickedCountry = mapGenerator.WorldData[countryIndex];

            // Usar StringBuilder é mais eficiente para montar strings.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>{clickedCountry.countryName}</b>");
            sb.AppendLine($"Governo: {clickedCountry.governmentType}");
            sb.AppendLine($"Estabilidade: {clickedCountry.politicalStability:P0}");
            sb.AppendLine($"Impostos: {clickedCountry.taxRate:P0}");
            sb.AppendLine($"Corrupção: {clickedCountry.corruptionLevel:P0}");

            countryInfoText.text = sb.ToString();
        }
        else
        {
            // Se clicou na água.
            countryInfoText.text = "Oceano";
        }
    }

    #endregion
}