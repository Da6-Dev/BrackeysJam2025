using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using TMPro;

// We implement interfaces to detect clicks, drags, and mouse scroll events.
public class MapController : MonoBehaviour, IPointerClickHandler, IDragHandler, IScrollHandler
{
    [Header("Core References")]
    [Tooltip("The GameObject containing the MapGenerator script.")]
    public MapGenerator mapGenerator;

    [Tooltip("The Panel or GameObject that contains the map image. This will be toggled on/off.")]
    public GameObject mapContainer;

    [Tooltip("The TextMeshPro component used to display country information.")]
    public TextMeshProUGUI countryInfoText;

    [Header("Control Settings")]
    [Tooltip("The speed of zooming with the mouse wheel.")]
    public float zoomSpeed = 0.5f;
    [Tooltip("The minimum zoom level.")]
    public float minZoom = 1f; // 1 = original size
    [Tooltip("The maximum zoom level.")]
    public float maxZoom = 10f;

    // Internal reference to the map's RectTransform for manipulation.
    private RectTransform mapRectTransform;

    #region Unity Methods

    void Awake()
    {
        mapRectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Ensure the map starts closed.
        if (mapContainer != null)
        {
            mapContainer.SetActive(false);
        }

        // Clear the info text at the start.
        if (countryInfoText != null)
        {
            countryInfoText.text = "";
        }
    }

    #endregion

    #region UI Interaction

    /// <summary>
    /// Toggles the visibility of the map view. Called by a UI button.
    /// </summary>
    public void ToggleMapView()
    {
        bool isMapActive = !mapContainer.activeSelf;
        mapContainer.SetActive(isMapActive);
        
        if (isMapActive)
        {
            ResetMapView();
            if (countryInfoText != null) countryInfoText.gameObject.SetActive(true);
        }
        else
        {
            // Clean up when closing the map
            if (countryInfoText != null) countryInfoText.text = "";
        }
    }

    /// <summary>
    /// Resets the map's position and zoom to the initial view.
    /// </summary>
    private void ResetMapView()
    {
        mapRectTransform.localScale = Vector3.one * 2f; // Start with a 2x zoom
        mapRectTransform.anchoredPosition = Vector2.zero; // Center the map
    }
    
    #endregion

    #region Map Control Handlers (EventSystem Interfaces)

    /// <summary>
    /// Called when the mouse wheel is used over the map image.
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y; // Positive for up, negative for down

        // Calculate and clamp the new zoom level
        float newZoom = mapRectTransform.localScale.x + scroll * zoomSpeed;
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);

        mapRectTransform.localScale = Vector3.one * newZoom;
    }

    /// <summary>
    /// Called continuously while the mouse is dragged over the map image.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Move the map by the amount the mouse has moved since the last frame.
        mapRectTransform.anchoredPosition += eventData.delta;
    }

    /// <summary>
    /// Called when a click is detected on the map image.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ignore clicks that are part of a drag gesture.
        if (eventData.dragging) return;

        // Convert screen coordinates to local coordinates on the RectTransform
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

        // Convert local coordinates to UV coordinates (0-1 range)
        float uvX = (localPoint.x / mapRectTransform.rect.width) + mapRectTransform.pivot.x;
        float uvY = (localPoint.y / mapRectTransform.rect.height) + mapRectTransform.pivot.y;

        // Convert UV coordinates to map texture coordinates
        int mapX = (int)(uvX * mapGenerator.mapWidth);
        int mapY = (int)(uvY * mapGenerator.mapHeight);

        // Ensure the coordinates are within the map bounds
        if (mapX < 0 || mapX >= mapGenerator.mapWidth || mapY < 0 || mapY >= mapGenerator.mapHeight) return;

        int countryIndex = mapGenerator.RegionMap[mapX, mapY];

        if (countryIndex != -1 && countryIndex < mapGenerator.WorldData.Count)
        {
            Country clickedCountry = mapGenerator.WorldData[countryIndex];

            // Use a StringBuilder for efficient string construction
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>{clickedCountry.countryName}</b>");
            sb.AppendLine($"Government: {clickedCountry.governmentType}");
            sb.AppendLine($"Stability: {clickedCountry.politicalStability:P0}"); // Format as percentage
            sb.AppendLine($"Taxes: {clickedCountry.taxRate:P0}");
            sb.AppendLine($"Primary Resource: {clickedCountry.primaryNaturalResource.ToString().Replace("_", " ")}");
            sb.AppendLine($"Corruption: {clickedCountry.corruptionLevel:P0}");

            countryInfoText.text = sb.ToString();
        }
        else
        {
            countryInfoText.text = "Ocean";
        }
    }

    #endregion
}