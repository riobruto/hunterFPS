using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class RailroadTerrainBrush : MonoBehaviour
{
    private Terrain _terrain;
    private AnimationCurve _brushProfile;
    private float _brushSpacing = 1f;
    private int brushSamples = 100;
    private float _verticalOffset;
    private int[] initialPassRadii = { 15, 7, 2 };

    [HideInInspector]
    private SplineContainer splineContainer;

    private float[,] originalTerrainHeights;

    public void SaveOriginalTerrainHeights()
    {
        if (_terrain == null || splineContainer == null)
            return;
        Debug.Log("Saving original terrain data");
        TerrainData terrainData = _terrain.terrainData;
        originalTerrainHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
    }

    public void CleanUp()
    {
        originalTerrainHeights = null;
        Debug.Log("Deleting original terrain data");
    }

    private List<Vector3> _points = new List<Vector3>();

    public void ShapeTerrain()
    {
        if (_terrain == null || splineContainer == null)
            return;

        // save original terrain in case the terrain got added later
        if (originalTerrainHeights == null)
            SaveOriginalTerrainHeights();
        _points.Clear();

        float terrainMin = _terrain.transform.position.y + 0f;
        float terrainMax = _terrain.transform.position.y + _terrain.terrainData.size.y;
        float totalHeight = terrainMax - terrainMin;

        // clone the original data, the modifications along the path are based on them

        //hack: optimizando el espacio de terreno a cachear considerando solo el pedazo de terreno que este en los bounds de la spline
        //nose si sirve XDD
        //float[,] allHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);

        Vector2Int maxBoundSample = new Vector2Int(
            (int)splineContainer.Spline.GetBounds().max.x + initialPassRadii[0] / 2,
            (int)splineContainer.Spline.GetBounds().max.z + initialPassRadii[0] / 2);

        maxBoundSample.x = Mathf.Clamp(maxBoundSample.x, 0, _terrain.terrainData.heightmapResolution);
        maxBoundSample.y = Mathf.Clamp(maxBoundSample.y, 0, _terrain.terrainData.heightmapResolution);

        /*float[,] allHeights = _terrain.terrainData.GetHeights(0, 0, maxBoundSample.x,
            maxBoundSample.y);*/

        float[,] allHeights = _terrain.terrainData.GetHeights(0, 0, _terrain.terrainData.heightmapResolution, _terrain.terrainData.heightmapResolution);

        List<Vector3> distancePoints = _points;

        brushSamples = (int)splineContainer.Spline.GetLength() / initialPassRadii[0] * 2 / (int)_brushSpacing;

        for (int i = 0; i < brushSamples; i++)
        {
            float t = i / (brushSamples - 1f);
            distancePoints.Add(transform.TransformPoint(splineContainer.Spline.EvaluatePosition(t)));
            _points = distancePoints;
        }
        // sort by height reverse
        // sequential height raising would just lead to irregularities, ie when a higher point follows a lower point
        // we need to proceed from top to bottom height

        distancePoints.Sort((a, b) => -a.y.CompareTo(b.y));
        Vector3[] points = distancePoints.ToArray();

        // the blur radius values being used for the various passes
        for (int pass = 0; pass < initialPassRadii.Length; pass++)
        {
            int radius = initialPassRadii[pass];
            // equi-distant points
            foreach (var point in points)
            {
                float targetHeight = ((point.y + _verticalOffset) - _terrain.transform.position.y) / totalHeight;

                int centerX = (int)(point.z - 1);
                int centerY = (int)(point.x - 1);

                AdjustTerrain(allHeights, radius, centerX, centerY, targetHeight);
            }
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < brushSamples; i++)
        {
            float t = i / (brushSamples - 1f);
            Gizmos.DrawSphere(transform.TransformPoint(splineContainer.Spline.EvaluatePosition(t)), 15f);
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(splineContainer.Spline.GetBounds().center, splineContainer.Spline.GetBounds().size + new Vector3(+initialPassRadii[0], 0, initialPassRadii[0]));
    }

    private void AdjustTerrain(float[,] heightMap, int radius, int centerX, int centerY, float targetHeight)
    {
        Debug.DrawLine(transform.position, new Vector3(centerX, targetHeight, centerY), Color.red, 2);

        float deltaHeight = targetHeight - heightMap[centerX, centerY];

        int sqrRadius = radius * radius;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                int sqrDstFromCenter = offsetX * offsetX + offsetY * offsetY;

                // check if point is inside brush radius
                if (sqrDstFromCenter <= sqrRadius)
                {
                    // calculate brush weight with exponential falloff from center
                    float dstFromCenter = Mathf.Sqrt(sqrDstFromCenter);
                    float t = dstFromCenter / radius;
                    float brushWeight = _brushProfile.Evaluate(t);

                    // raise terrain
                    int brushX = centerX + offsetX;
                    int brushY = centerY + offsetY;

                    if (brushX >= 0 && brushY >= 0 && brushX < width && brushY < height)
                    {
                        heightMap[brushX, brushY] += deltaHeight * brushWeight;

                        // clamp the height
                        if (heightMap[brushX, brushY] > targetHeight)
                        {
                            heightMap[brushX, brushY] = targetHeight;
                        }
                    }
                }
            }
        }

        _terrain.terrainData.SetHeights(0, 0, heightMap);
    }

    public void SetProperties(Terrain terrain, AnimationCurve profile, float spacing, float verticalOffset, int[] radialPasses)
    {
        splineContainer = GetComponent<SplineContainer>();
        _terrain = terrain;
        _brushProfile = profile;
        _brushSpacing = spacing;
        _verticalOffset = verticalOffset;
        initialPassRadii = radialPasses;
    }
}