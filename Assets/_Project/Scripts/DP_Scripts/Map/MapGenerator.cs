// Scripts/Gameplay/MapGenerator.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 1000;
    public int mapHeight = 1000;
    public float noiseScale = 300f;

    [Header("Country Generation")]
    [Tooltip("Forces a minimum distance between capitals to ensure countries have space to grow.")]
    public float minDistanceBetweenCapitals = 30f;
    public Color borderColor = Color.black;

    [Header("Capital Markers")]
    public bool drawCapitalMarkers = true;
    public Color capitalMarkerColor = Color.white;
    [Tooltip("The 'radius' of the marker. 0 = 1x1 pixel, 1 = 3x3 pixels, 2 = 5x5 pixels.")]
    public int capitalMarkerSize = 1;

    [Header("Terrain Influence")]
    [Range(0, 20)]
    public float terrainInfluenceOnBorders = 4f;

    [Header("Border Irregularity")]
    public float irregularityScale = 80f;
    [Tooltip("Controls the intensity of the irregularity. Use LOW values (1 to 15) to start.")]
    public float irregularityStrength = 8f;

    [Header("Water Level")]
    [Range(0, 1)]
    public float seaLevel = 0.4f;
    public Color waterColor;

    [Header("Fractal Noise Settings")]
    public int octaves = 4;
    [Range(0, 1)]
    public float persistence = 0.53f;
    public float lacunarity = 2f;

    [Header("Falloff Map Settings")]
    public bool useFalloff = true;
    public float falloffCurveA = 4.3f;
    public float falloffCurveB = 3.9f;

    [Header("Map Seed")]
    public bool useRandomSeed = true;
    public int seed;
    
    [Header("UI Display")]
    public RawImage mapDisplay;

    // Public properties to hold the generated data
    public List<Country> WorldData { get; private set; }
    public int[,] RegionMap { get; private set; }

    private Vector2 offset;
    private Vector2 irregularityOffset;

    // Helper class for the region generation algorithm
    private class PixelNode : System.IComparable<PixelNode>
    {
        public Vector2Int position;
        public float cost;
        public int countryIndex;

        public PixelNode(Vector2Int p, float c, int i) { position = p; cost = c; countryIndex = i; }
        
        public int CompareTo(PixelNode other)
        {
            int compare = cost.CompareTo(other.cost);
            
            if (compare == 0)
            {
                compare = position.x.CompareTo(other.position.x);
                if (compare == 0)
                {
                    compare = position.y.CompareTo(other.position.y);
                }
            }
            return compare;
        }
    }
    
    /// <summary>
    /// The main entry point for generating the entire map from a list of Country data.
    /// </summary>
    public void GenerateMapFromData(List<Country> worldData)
    {
        this.WorldData = worldData;

        System.Random prng;
        if (useRandomSeed) { seed = Random.Range(-100000, 100000); }
        prng = new System.Random(seed);

        offset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        irregularityOffset = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));

        float[,] noiseMap = GenerateNoiseMap(offset, noiseScale);
        float[,] irregularityMap = GenerateNoiseMap(irregularityOffset, irregularityScale);
        float[,] falloffMap = useFalloff ? GenerateFalloffMap() : null;
        
        PlaceCapitals(noiseMap, falloffMap);
        RegionMap = GenerateRegionMap(noiseMap, irregularityMap);

        if (useFalloff)
        {
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
        }

        DrawMapTexture(noiseMap, RegionMap);
    }
    
    // O resto do script permanece igual... (as funções abaixo estão corretas)
    
    public void DrawMapTexture(float[,] noiseMap, int[,] regionMap)
    {
        int width = noiseMap.GetLength(0); 
        int height = noiseMap.GetLength(1);
        Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (noiseMap[x, y] < seaLevel)
                {
                    colorMap[y * width + x] = waterColor;
                }
                else
                {
                    int countryIndex = regionMap[x, y];
                    bool isBorder = false;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx; int ny = y + dy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (noiseMap[nx, ny] >= seaLevel && regionMap[nx, ny] != countryIndex)
                                {
                                    isBorder = true; break;
                                }
                            }
                        }
                        if (isBorder) break;
                    }

                    if (isBorder)
                    {
                        colorMap[y * width + x] = borderColor;
                    }
                    else if (countryIndex != -1)
                    {
                        colorMap[y * width + x] = WorldData[countryIndex].mapColor;
                    }
                }
            }
        }

        if (drawCapitalMarkers)
        {
            foreach (var country in WorldData)
            {
                Vector2Int capitalPos = country.capitalPosition;
                for (int yOffset = -capitalMarkerSize; yOffset <= capitalMarkerSize; yOffset++)
                {
                    for (int xOffset = -capitalMarkerSize; xOffset <= capitalMarkerSize; xOffset++)
                    {
                        int markerX = capitalPos.x + xOffset; 
                        int markerY = capitalPos.y + yOffset;
                        if (markerX >= 0 && markerX < width && markerY >= 0 && markerY < height)
                        {
                            colorMap[markerY * width + markerX] = capitalMarkerColor;
                        }
                    }
                }
            }
        }
        texture.SetPixels(colorMap); 
        texture.Apply();
        mapDisplay.texture = texture;
    }
    
    void PlaceCapitals(float[,] noiseMap, float[,] falloffMap)
    {
        List<Vector2Int> occupiedPositions = new List<Vector2Int>();
        foreach (var country in WorldData)
        {
            Vector2Int capitalPosition;
            int attempts = 0;
            bool validPositionFound;

            do
            {
                attempts++;
                if (attempts > 20000) 
                { 
                    Debug.LogError($"Could not find a valid capital position for {country.countryName} after {attempts} attempts."); 
                    return; 
                }

                validPositionFound = true;
                capitalPosition = new Vector2Int(Random.Range(0, mapWidth), Random.Range(0, mapHeight));

                float height = noiseMap[capitalPosition.x, capitalPosition.y];
                if (falloffMap != null) height -= falloffMap[capitalPosition.x, capitalPosition.y];
                if (height < seaLevel) { validPositionFound = false; continue; }

                foreach (var pos in occupiedPositions)
                {
                    if (Vector2.Distance(capitalPosition, pos) < minDistanceBetweenCapitals)
                    {
                        validPositionFound = false; break;
                    }
                }
            } while (!validPositionFound);
            
            country.capitalPosition = capitalPosition;
            occupiedPositions.Add(capitalPosition);
        }
    }

    private int[,] GenerateRegionMap(float[,] noiseMap, float[,] irregularityMap)
    {
        int[,] regionMap = new int[mapWidth, mapHeight]; 
        float[,] costMap = new float[mapWidth, mapHeight];
        SortedSet<PixelNode> priorityQueue = new SortedSet<PixelNode>();
        
        for (int y = 0; y < mapHeight; y++) 
        { 
            for (int x = 0; x < mapWidth; x++) 
            { 
                regionMap[x, y] = -1; 
                costMap[x, y] = float.MaxValue; 
            } 
        }

        foreach (var country in WorldData)
        {
            Vector2Int seedPos = country.capitalPosition;
            costMap[seedPos.x, seedPos.y] = 0;
            regionMap[seedPos.x, seedPos.y] = country.countryID;
            priorityQueue.Add(new PixelNode(seedPos, 0, country.countryID));
        }

        int[] dx = { 0, 0, 1, -1 }; 
        int[] dy = { 1, -1, 0, 0 }; 
        
        while (priorityQueue.Count > 0) 
        { 
            PixelNode currentNode = priorityQueue.Min; 
            priorityQueue.Remove(currentNode); 
            int currentX = currentNode.position.x; 
            int currentY = currentNode.position.y; 
            int currentCountry = currentNode.countryIndex;
            
            for (int i = 0; i < 4; i++) 
            { 
                int nx = currentX + dx[i]; 
                int ny = currentY + dy[i]; 
                
                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight) 
                { 
                    if (noiseMap[nx, ny] < seaLevel) continue; 
                    
                    float baseCost = 1f; 
                    float terrainCost = noiseMap[nx, ny] * terrainInfluenceOnBorders; 
                    float irregularityCost = irregularityMap[nx, ny] * irregularityStrength; 
                    float moveCost = baseCost + terrainCost + irregularityCost; 
                    float newCost = currentNode.cost + moveCost; 
                    
                    if (newCost < costMap[nx, ny]) 
                    { 
                        if (costMap[nx, ny] != float.MaxValue) 
                        { 
                            priorityQueue.RemoveWhere(node => node.position.x == nx && node.position.y == ny); 
                        } 
                        costMap[nx, ny] = newCost; 
                        regionMap[nx, ny] = currentCountry; 
                        priorityQueue.Add(new PixelNode(new Vector2Int(nx, ny), newCost, currentCountry)); 
                    } 
                } 
            } 
        }
        return regionMap;
    }

    public float[,] GenerateNoiseMap(Vector2 mapOffset, float scale)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        float maxNoiseHeight = float.MinValue; 
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float amplitude = 1; 
                float frequency = 1; 
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + mapOffset.x) / scale * frequency; 
                    float sampleY = (y + mapOffset.y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude; 
                    amplitude *= persistence; 
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }

    private float[,] GenerateFalloffMap()
    {
        float[,] map = new float[mapWidth, mapHeight];
        for (int i = 0; i < mapWidth; i++)
        {
            for (int j = 0; j < mapHeight; j++)
            {
                float x = i / (float)mapWidth * 2 - 1; 
                float y = j / (float)mapHeight * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                float falloffValue = Mathf.Pow(value, falloffCurveA) / (Mathf.Pow(value, falloffCurveA) + Mathf.Pow(falloffCurveB - falloffCurveB * value, falloffCurveA));
                map[i, j] = falloffValue;
            }
        }
        return map;
    }
}