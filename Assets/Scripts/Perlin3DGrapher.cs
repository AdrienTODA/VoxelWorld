using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Perlin3DGrapher : MonoBehaviour
{
    private Vector3 dimensions = new Vector3(10, 10, 10);
    
    public float heightScale = 2.0f;
    [Range(0.0f, 1.0f)]
    public float scale = 0.5f;
    public int octaves = 1;
    public float heightOffset = 1;
    [Range(0.0f, 10.0f)]
    public float drawCutOff = 1.0f;

    private void CreateCubes()
    {
        for (var z = 0; z < dimensions.z; z++)
        {
            for (var y = 0; y < dimensions.y; y++)
            {
                for (var x = 0; x < dimensions.x; x++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "Perlin_Cube";
                    cube.transform.parent = transform;
                    cube.transform.position = new Vector3(x, y, z);
                }
            }
        }
    }

    private void Graph()
    {
        // Destroy existing cubes
        var cubes = GetComponentsInChildren<MeshRenderer>();

        if (cubes.Length == 0)
            CreateCubes();
        if (cubes.Length == 0) return;
        
        for (var z = 0; z < dimensions.z; z++)
        {
            for (var y = 0; y < dimensions.y; y++)
            {
                for (var x = 0; x < dimensions.x; x++)
                {
                    float p3d = MeshUtils.fBM3D(x, y, z, octaves, scale, heightScale, heightOffset);

                    if (p3d < drawCutOff)
                        cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = false;
                    else
                        cubes[x + (int)dimensions.x * (y + (int)dimensions.z * z)].enabled = true;
                }
            }
        }
    }

    private void OnValidate()
    {
        Graph();
    }
}
