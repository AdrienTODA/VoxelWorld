using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quad
{
    public Mesh mesh;
    
    // Start is called before the first frame update
    public Quad(MeshUtils.BlockSide side, Vector3 offset, MeshUtils.BlockType bType, MeshUtils.BlockType hType)
    {
        mesh = new Mesh();
        mesh.name = $"ScriptedQuad";

        var vertices = new Vector3[4];
        var normals = new Vector3[4];
        var uvs = new Vector2[4];
        var triangles = new int[] { 3, 1, 0, 3, 2, 1 };

        /* Dirt block =
            (0.125, 0.9375)
            (0.1875, 0.9375)
            (0.125, 1)
            (0.1875, 1)
         */

        var suvs = new List<Vector2>();
        suvs.Add(MeshUtils.blockUVs[(int)hType, 3]);
        suvs.Add(MeshUtils.blockUVs[(int)hType, 2]);
        suvs.Add(MeshUtils.blockUVs[(int)hType, 0]);
        suvs.Add(MeshUtils.blockUVs[(int)hType, 1]);

        var uv00 = MeshUtils.blockUVs[(int)bType, 0];
        var uv10 = MeshUtils.blockUVs[(int)bType, 1];
        var uv01 = MeshUtils.blockUVs[(int)bType, 2];
        var uv11 = MeshUtils.blockUVs[(int)bType, 3];

        var p0 = new Vector3(-.5f, -.5f, .5f) + offset;
        var p1 = new Vector3(.5f, -.5f, .5f) + offset;
        var p2 = new Vector3(.5f, -.5f, -.5f) + offset;
        var p3 = new Vector3(-.5f, -.5f, -.5f) + offset;
        var p4 = new Vector3(-.5f, .5f, .5f) + offset;
        var p5 = new Vector3(.5f, .5f, .5f) + offset;
        var p6 = new Vector3(.5f, .5f, -.5f) + offset;
        var p7 = new Vector3(-.5f, .5f, -.5f) + offset;

        switch (side)
        {
            case MeshUtils.BlockSide.FRONT:
            {
                vertices = new Vector3[] { p4, p5, p1, p0 };
                normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
            case MeshUtils.BlockSide.BACK:
            {
                vertices = new Vector3[] { p6, p7, p3, p2 };
                normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
            case MeshUtils.BlockSide.LEFT:
            {
                vertices = new Vector3[] { p7, p4, p0, p3 };
                normals = new Vector3[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
            case MeshUtils.BlockSide.RIGHT:
            {
                vertices = new Vector3[] { p5, p6, p2, p1 };
                normals = new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
            case MeshUtils.BlockSide.TOP:
            {
                vertices = new Vector3[] { p7, p6, p5, p4 };
                normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
            case MeshUtils.BlockSide.BOTTOM:
            {
                vertices = new Vector3[] { p0, p1, p2, p3 };
                normals = new Vector3[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
                uvs = new Vector2[] { uv11, uv01, uv00, uv10 };
                break;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.SetUVs(1, suvs);
        
        mesh.RecalculateBounds();
    }
}
