using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
    public Mesh mesh;
    private Chunk parentChunk;
    
    // Start is called before the first frame update
    public Block(Vector3 offset, MeshUtils.BlockType bType, Chunk chunk, MeshUtils.BlockType hType)
    {
        if (bType == MeshUtils.BlockType.AIR) return;
        
        var quads = new List<Quad>();
        parentChunk = chunk;
        var blockLocalPos = offset - chunk.location;
        
        if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y, (int)blockLocalPos.z + 1, bType))
            quads.Add(new Quad(MeshUtils.BlockSide.FRONT, offset, bType, hType));
        if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y, (int)blockLocalPos.z - 1, bType))
            quads.Add(new Quad(MeshUtils.BlockSide.BACK, offset, bType, hType));
        if (!HasSolidNeighbour((int)blockLocalPos.x - 1, (int)blockLocalPos.y, (int)blockLocalPos.z, bType))
            quads.Add(new Quad(MeshUtils.BlockSide.LEFT, offset, bType, hType));
        if (!HasSolidNeighbour((int)blockLocalPos.x + 1, (int)blockLocalPos.y, (int)blockLocalPos.z, bType))
            quads.Add(new Quad(MeshUtils.BlockSide.RIGHT, offset, bType, hType));

        if (bType == MeshUtils.BlockType.GRASSSIDE)
        {
            if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y + 1, (int)blockLocalPos.z, bType))
                quads.Add(new Quad(MeshUtils.BlockSide.TOP, offset, MeshUtils.BlockType.GRASSTOP, hType));
            if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y - 1, (int)blockLocalPos.z, bType))
                quads.Add(new Quad(MeshUtils.BlockSide.BOTTOM, offset, MeshUtils.BlockType.DIRT, hType));
        }
        else
        {
            if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y + 1, (int)blockLocalPos.z, bType))
                quads.Add(new Quad(MeshUtils.BlockSide.TOP, offset, bType, hType));
            if (!HasSolidNeighbour((int)blockLocalPos.x, (int)blockLocalPos.y - 1, (int)blockLocalPos.z, bType))
                quads.Add(new Quad(MeshUtils.BlockSide.BOTTOM, offset, bType, hType));
        }
        
        if (quads.Count == 0) return;
        
        var sideMeshes = new Mesh[quads.Count];

        for (int i = 0; i < quads.Count; i++)
        {
            sideMeshes[i] = quads[i].mesh;
        }

        mesh = MeshUtils.MergeMeshes(sideMeshes);
        mesh.name = $"Cube_0_0_0";
    }

    public bool HasSolidNeighbour(int x, int y, int z, MeshUtils.BlockType bType)
    {
        if (x < 0 || x >= parentChunk.width ||
            y < 0 || y >= parentChunk.height ||
            z < 0 || z >= parentChunk.depth)
        {
            return false;
        }

        if (parentChunk.chunkData[x + parentChunk.width * (y + parentChunk.depth * z)] == bType)
        {
            return true;
        }
        
        if (parentChunk.chunkData[x + parentChunk.width * (y + parentChunk.depth * z)] == MeshUtils.BlockType.AIR ||
            parentChunk.chunkData[x + parentChunk.width * (y + parentChunk.depth * z)] == MeshUtils.BlockType.WATER)
        {
            return false;
        }

        return true;
    }
}
