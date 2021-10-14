using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PerlinGrapher : MonoBehaviour
{
    public LineRenderer lr;
    public float heightScale = 2.0f;
    [Range(0.0f, 1.0f)]
    public float scale = 0.5f;
    public int octaves = 1;
    public float heightOffset = 1;
    [Range(0.0f, 1.0f)]
    public float probability = 1;
    
    void Graph()
    {
        int z = 11;
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 100;
        var positions = new Vector3[lr.positionCount];

        for (int x = 0; x < lr.positionCount; x++)
        {
            var y = MeshUtils.fBM(x, z, octaves, scale, heightScale, heightOffset) + heightOffset;
            positions[x] = new Vector3(x, y, z);
        }
        
        lr.SetPositions(positions);
    }

    /*private float fBM(float x, float z)
    {
        float total = 0;
        float frequency = 1;
        
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * scale * frequency, z * scale * frequency) * heightScale;
            frequency *= 2;
        }

        return total;
    }*/

    private void OnValidate()
    {
        Graph();
    }

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 100;
        Graph();
    }
}
