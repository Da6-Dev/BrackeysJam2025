using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using TMPro;

public class MapController : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
{
    [Header("Essential References")]
    [Tooltip("Reference to the MapGenerator script to get map data.")]
    public MapGenerator mapGenerator;

    [Tooltip("The RectTransform of the RawImage that displays the map. This is the object that will be moved and scaled.")]
    public RectTransform mapDisplayImage;

    [Tooltip("The GameObject that serves as the 'viewport' for the map. It will be enabled and disabled.")]
    public GameObject mapContainer;

    [Tooltip("The TextMeshPro component to display information of the clicked country.")]
    public TextMeshProUGUI countryInfoText;

    [Header("Control Settings")]
    [Tooltip("The speed of the zoom applied with the mouse wheel.")]
    public float zoomSpeed = 0.5f;
    [Tooltip("Minimum allowed zoom level (1 = 100% of original size).")]
    public float minZoom = 1f;
    [Tooltip("Maximum allowed zoom level.")]
    public float maxZoom = 10f;
    [Tooltip("Initial zoom when the map is opened.")]
    public float initialZoom = 2f;

    private RectTransform viewportRectTransform;
    private bool isMapVisible = false;

    #region Unity Lifecycle
    void Awake()
    {
        if (mapDisplayImage == null || mapContainer == null || mapGenerator == null || countryInfoText == null)
        {
            Debug.LogError("One or more essential references are not set in the MapController's Inspector! The script will be disabled.");
            this.enabled = false;
            return;
        }
        viewportRectTransform = mapContainer.GetComponent<RectTransform>();
    }

    void Start()
    {
        mapContainer.SetActive(false);
        countryInfoText.text = "";
        isMapVisible = false;
    }

    void Update()
    {
        if (isMapVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMapView();
        }
    }

    void LateUpdate()
    {
        if (isMapVisible)
        {
            ClampMapPosition();
        }
    }
    #endregion

    #region UI Interaction
    public void ToggleMapView()
    {
        isMapVisible = !isMapVisible;
        mapContainer.SetActive(isMapVisible);

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
            countryInfoText.text = "";
        }
    }

    private void ResetMapView()
    {
        mapDisplayImage.localScale = Vector3.one * initialZoom;
        mapDisplayImage.anchoredPosition = Vector2.zero;
        countryInfoText.text = "";
        ClampMapPosition();
    }
    #endregion

    #region Mouse Event Handlers
    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;
        float currentZoom = mapDisplayImage.localScale.x;
        float newZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);

        if (Mathf.Approximately(newZoom, currentZoom)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);

        Vector2 pivotToMouse = (mapDisplayImage.anchoredPosition - localMousePosition);
        float zoomRatio = newZoom / currentZoom;
        Vector2 newAnchoredPosition = localMousePosition + pivotToMouse * zoomRatio;
        
        mapDisplayImage.localScale = Vector3.one * newZoom;
        mapDisplayImage.anchoredPosition = newAnchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        mapDisplayImage.anchoredPosition += eventData.delta;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        
        float uvX = (localPoint.x / mapDisplayImage.rect.width) + mapDisplayImage.pivot.x;
        float uvY = (localPoint.y / mapDisplayImage.rect.height) + mapDisplayImage.pivot.y;

        int mapX = (int)(uvX * mapGenerator.mapWidth);
        int mapY = (int)(uvY * mapGenerator.mapHeight);

        if (mapX < 0 || mapX >= mapGenerator.mapWidth || mapY < 0 || mapY >= mapGenerator.mapHeight) return;

        int countryIndex = mapGenerator.RegionMap[mapX, mapY];
        UpdateCountryInfoText(countryIndex);
    }
    #endregion

    #region Helper Methods
    private void ClampMapPosition()
    {
        Vector2 viewportSize = viewportRectTransform.rect.size;
        Vector2 scaledMapSize = mapDisplayImage.rect.size * mapDisplayImage.localScale.x;
        
        float maxX = Mathf.Max(0, (scaledMapSize.x - viewportSize.x) / 2f);
        float maxY = Mathf.Max(0, (scaledMapSize.y - viewportSize.y) / 2f);
        
        Vector2 currentPos = mapDisplayImage.anchoredPosition;
        
        float clampedX = Mathf.Clamp(currentPos.x, -maxX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, -maxY, maxY);
        
        mapDisplayImage.anchoredPosition = new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Updates the text panel with the information of the selected country, according to the GDD.
    /// </summary>
    private void UpdateCountryInfoText(int countryIndex)
    {
        if (countryIndex != -1 && countryIndex < mapGenerator.WorldData.Count)
        {
            Country clickedCountry = mapGenerator.WorldData[countryIndex];

            // Using StringBuilder is more efficient for constructing strings.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>{clickedCountry.countryName}</b>");
            sb.AppendLine($""); // Blank line for spacing
            // Display the 3 new key attributes of the country.
            sb.AppendLine($"Risk Level: {clickedCountry.riskLevel:P0}");
            sb.AppendLine($"Investment Climate: {clickedCountry.investmentClimate:P0}");
            sb.AppendLine($"Featured Sector: {clickedCountry.featuredSector}");

            countryInfoText.text = sb.ToString();
        }
        else
        {
            // If the player clicked on water.
            countryInfoText.text = "Ocean";
        }
    }
    #endregion
}