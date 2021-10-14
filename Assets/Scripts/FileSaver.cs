using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[Serializable]
public class WorldData
{
    /*private HashSet<Vector3Int> chunkChecker = new HashSet<Vector3Int>();
    private HashSet<Vector2Int> chunkColumns = new HashSet<Vector2Int>();
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();*/

    public int[] chunkCheckerValues;
    public int[] chunkColumnValues;
    public int[] allChunkData;
    public bool[] chunkVisibility;

    public int fpcX;
    public int fpcY;
    public int fpcZ;

    public WorldData() { }

    public WorldData(HashSet<Vector3Int> cc, HashSet<Vector2Int> cCols, Dictionary<Vector3Int, Chunk> chks, Vector3 fpc)
    {
        chunkCheckerValues = new int[cc.Count * 3];
        var index = 0;
        var vIndex = 0;

        foreach (var v in cc)
        {
            chunkCheckerValues[index] = v.x;
            chunkCheckerValues[index + 1] = v.y;
            chunkCheckerValues[index + 2] = v.z;
            index += 3;
        }

        index = 0;
        chunkColumnValues = new int[cCols.Count * 2];

        foreach (var v in cCols)
        {
            chunkColumnValues[index] = v.x;
            chunkColumnValues[index + 1] = v.y;
            index += 2;
        }

        index = 0;
        allChunkData = new int[chks.Count * World.chunkDimensions.x * World.chunkDimensions.y * World.chunkDimensions.z];
        chunkVisibility = new bool[chks.Count];

        foreach (var ch in chks)
        {
            foreach (var bt in ch.Value.chunkData)
            {
                allChunkData[index] = (int)bt;
                index++;
            }

            chunkVisibility[vIndex] = ch.Value.meshRendererSolid.enabled;
            vIndex++;
        }

        fpcX = (int)fpc.x;
        fpcY = (int)fpc.y;
        fpcZ = (int)fpc.z;
    }
}

public static class FileSaver
{
    private static WorldData wd;

    private static string BuildFileName()
    {
        return $"{Application.persistentDataPath}/savedata/World_{World.chunkDimensions.x}_" +
               $"{World.chunkDimensions.y}_{World.chunkDimensions.z}_{World.worldDimensions.x}_" +
               $"{World.worldDimensions.y}_{World.worldDimensions.z}.dat";
    }

    public static void Save(World world)
    {
        var fileName = BuildFileName();

        if (!File.Exists(fileName))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? throw new InvalidOperationException());
        }

        var bf = new BinaryFormatter();
        using var fs = File.Open(fileName, FileMode.OpenOrCreate);
        wd = new WorldData(world.chunkChecker, world.chunkColumns, world.chunks, world.fpc.transform.position);
        bf.Serialize(fs, wd);
        Debug.Log($"Saving World to file : {fileName}");
    }

    public static WorldData Load()
    {
        var fileName = BuildFileName();

        if (!File.Exists(fileName)) return null;
        
        var bf = new BinaryFormatter();

        using var fs = File.Open(fileName, FileMode.Open);
        wd = new WorldData();
        wd = (WorldData)bf.Deserialize(fs);
        Debug.Log($"Loading World to file : {fileName}");

        return wd;
    }
}
