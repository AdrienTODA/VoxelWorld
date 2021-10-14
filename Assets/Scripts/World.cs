using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct PerlinSettings
{
    public float heightScale;
    public float scale;
    public int octaves;
    public float heightOffset;
    public float probability;

    public PerlinSettings(float hs, float sc, int oct, float ho, float prob)
    {
        heightScale = hs;
        scale = sc;
        octaves = oct;
        heightOffset = ho;
        probability = prob;
    }
}

public class World : MonoBehaviour
{
    public static Vector3Int worldDimensions = new Vector3Int(5, 5, 5);
    public static Vector3Int extraWorldDimensions = new Vector3Int(5, 5, 5);
    public static Vector3Int chunkDimensions = new Vector3Int(10, 10, 10);
    public bool loadFromFile = false;
    public GameObject chunkPrefab;
    public GameObject mainCamera;
    public GameObject fpc;
    public Slider loadingBar;

    public static PerlinSettings surfaceSettings;
    public PerlinGrapher surface;
    
    public static PerlinSettings stoneSettings;
    public PerlinGrapher stone;
    
    public static PerlinSettings diamondTSettings;
    public PerlinGrapher diamondT;
    
    public static PerlinSettings diamondBSettings;
    public PerlinGrapher diamondB;
    
    public static PerlinSettings caveSettings;
    public Perlin3DGrapher caves;
    
    public static PerlinSettings treeSettings;
    public Perlin3DGrapher trees;

    public HashSet<Vector3Int> chunkChecker = new HashSet<Vector3Int>();
    public HashSet<Vector2Int> chunkColumns = new HashSet<Vector2Int>();
    public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    private Vector3Int lastBuildPos;
    private int drawRadius = 3;

    private Queue<IEnumerator> buildQueue = new Queue<IEnumerator>();

    private MeshUtils.BlockType buildType = MeshUtils.BlockType.DIRT;

    // Start is called before the first frame update
    private void Start()
    {
        loadingBar.maxValue = worldDimensions.x * worldDimensions.z;

        surfaceSettings = new PerlinSettings(surface.heightScale, surface.scale, surface.octaves, 
            surface.heightOffset, surface.probability);
        
        stoneSettings = new PerlinSettings(stone.heightScale, stone.scale, stone.octaves, 
            stone.heightOffset, stone.probability);
        
        diamondTSettings = new PerlinSettings(diamondT.heightScale, diamondT.scale, diamondT.octaves, 
            diamondT.heightOffset, diamondT.probability);
        diamondBSettings = new PerlinSettings(diamondB.heightScale, diamondB.scale, diamondB.octaves, 
            diamondB.heightOffset, diamondB.probability);
        
        caveSettings = new PerlinSettings(caves.heightScale, caves.scale, caves.octaves, 
            caves.heightOffset, caves.drawCutOff);
        
        treeSettings = new PerlinSettings(trees.heightScale, trees.scale, trees.octaves, 
            trees.heightOffset, trees.drawCutOff);

        if (loadFromFile)
            StartCoroutine(LoadWorldFromFile());
        else
            StartCoroutine(BuildWorld());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            RaycastHit hitInfo;
            
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 10))
            {
                var hitBlock = Vector3.zero;

                if (Input.GetMouseButtonDown(0))
                    hitBlock = hitInfo.point - hitInfo.normal / 2.0f;
                else
                    hitBlock = hitInfo.point + hitInfo.normal / 2.0f;
                
                var thisChunk = hitInfo.collider.transform.parent.GetComponent<Chunk>();

                var bx = (int)(Mathf.Round(hitBlock.x) - thisChunk.location.x);
                var by = (int)(Mathf.Round(hitBlock.y) - thisChunk.location.y);
                var bz = (int)(Mathf.Round(hitBlock.z) - thisChunk.location.z);

                var blockNeighbour = GetWorldNeighbour(new Vector3Int(bx, by, bz), 
                    Vector3Int.CeilToInt(thisChunk.location));
                thisChunk = chunks[blockNeighbour.Item2];
                var i = ToFlat(blockNeighbour.Item1);

                if (Input.GetMouseButtonDown(0))
                {
                    if (MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[i]] == -1)
                        return;

                    if (thisChunk.healthData[i] == MeshUtils.BlockType.NOCRACK)
                        StartCoroutine(HealBlock(thisChunk, i));
                    
                    thisChunk.healthData[i]++;
                    if (thisChunk.healthData[i] == MeshUtils.BlockType.NOCRACK +
                        MeshUtils.blockTypeHealth[(int)thisChunk.chunkData[i]])
                    {
                        thisChunk.chunkData[i] = MeshUtils.BlockType.AIR;
                        var nBlock = FromFlat(i);
                        var neighbourBlock = GetWorldNeighbour(new Vector3Int(nBlock.x, nBlock.y + 1, nBlock.z),
                            Vector3Int.CeilToInt(thisChunk.location));
                        var block = neighbourBlock.Item1;
                        var neighbourBlockIndex = ToFlat(block);
                        var neighbourChunk = chunks[neighbourBlock.Item2];
                        StartCoroutine(Drop(neighbourChunk, neighbourBlockIndex));
                    }
                }
                else
                {
                    thisChunk.chunkData[i] = buildType;
                    thisChunk.healthData[i] = MeshUtils.BlockType.NOCRACK;
                    StartCoroutine(Drop(thisChunk, i));
                }

                RedrawChunk(thisChunk);
            }
        }
    }

    public static Vector3Int FromFlat(int i)
    {
        return new Vector3Int(i % chunkDimensions.x, (i / chunkDimensions.x) % chunkDimensions.y,
            i / (chunkDimensions.x * chunkDimensions.y));
    }

    public static int ToFlat(Vector3Int v)
    {
        return v.x + chunkDimensions.x * (v.y + chunkDimensions.z * v.z);
    }

    public (Vector3Int, Vector3Int) GetWorldNeighbour(Vector3Int blockIndex, Vector3Int chunkIndex)
    {
        var thisChunk = chunks[chunkIndex];
        var bx = blockIndex.x;
        var by = blockIndex.y;
        var bz = blockIndex.z;
        
        var neighbour = chunkIndex;
                
        if (bx == chunkDimensions.x)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x + chunkDimensions.x, 
                (int)thisChunk.location.y, (int)thisChunk.location.z);
            bx = 0;
        }
        else if (bx == -1)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x - chunkDimensions.x, 
                (int)thisChunk.location.y, (int)thisChunk.location.z);
            bx = chunkDimensions.x - 1;
        }
        else if (by == chunkDimensions.y)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x, 
                (int)thisChunk.location.y + chunkDimensions.y, (int)thisChunk.location.z);
            by = 0;
        }
        else if (by == -1)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x, 
                (int)thisChunk.location.y - chunkDimensions.y, (int)thisChunk.location.z);
            by = chunkDimensions.y - 1;
        }
        else if (bz == chunkDimensions.z)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x, 
                (int)thisChunk.location.y, (int)thisChunk.location.z + chunkDimensions.z);
            bz = 0;
        }
        else if (bz == -1)
        {
            neighbour = new Vector3Int((int)thisChunk.location.x, 
                (int)thisChunk.location.y, (int)thisChunk.location.z - chunkDimensions.z);
            bz = chunkDimensions.z - 1;
        }

        return (new Vector3Int(bx, by, bz), neighbour);
    }
    
    public void SetBuildType(int bType)
    {
        buildType = (MeshUtils.BlockType)bType;
    }

    public void SaveWorld()
    {
        FileSaver.Save(this);
    }

    IEnumerator LoadWorldFromFile()
    {
        var wd = FileSaver.Load();

        if (wd == null)
        {
            StartCoroutine(BuildWorld());
            yield break;
        }
        
        // Data to unpack :
        // chunkChecker : HashSet<Vector3Int>
        // chunkColumns : HashSet<Vector2Int>
        // chunks : Dictionary<Vector3Int, Chunk>
        // fpc : Vector3 (position)

        chunkChecker.Clear();
        chunkColumns.Clear();
        chunks.Clear();
        var index = 0;
        var vIndex = 0;

        for (int i = 0; i < wd.chunkCheckerValues.Length; i += 3)
        {
            chunkChecker.Add(new Vector3Int(wd.chunkCheckerValues[i], wd.chunkCheckerValues[i + 1],
                wd.chunkCheckerValues[i + 2]));
        }

        for (int i = 0; i < wd.chunkColumnValues.Length; i += 2)
        {
            chunkColumns.Add(new Vector2Int(wd.chunkColumnValues[i], wd.chunkColumnValues[i + 1]));
        }

        loadingBar.maxValue = chunkChecker.Count;

        foreach (var chunkPos in chunkChecker)
        {
            var chunk = Instantiate(chunkPrefab);
            chunk.name = $"Chunk[{chunkPos.x}, {chunkPos.y}, {chunkPos.z}]";
            var c = chunk.GetComponent<Chunk>();
            var blockCount = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
            c.chunkData = new MeshUtils.BlockType[blockCount];
            c.healthData = new MeshUtils.BlockType[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                c.chunkData[i] = (MeshUtils.BlockType)wd.allChunkData[index];
                c.healthData[i] = MeshUtils.BlockType.NOCRACK;
                index++;
            }
            
            loadingBar.value++;
            c.CreateChunk(chunkDimensions, chunkPos, false);
            chunks.Add(chunkPos, c);
            RedrawChunk(c);
            c.meshRendererSolid.enabled = wd.chunkVisibility[vIndex];
            c.meshRendererFluid.enabled = wd.chunkVisibility[vIndex];
            vIndex++;
            yield return null;
        }

        fpc.transform.position = new Vector3(wd.fpcX, wd.fpcY, wd.fpcZ);
        ResetUIAfterLoad();
        lastBuildPos = Vector3Int.CeilToInt(fpc.transform.position);
        StartCoroutine(BuildCoordinator());
        StartCoroutine(UpdateWorld());
    }

    public void ResetUIAfterLoad()
    {
        mainCamera.SetActive(false);
        fpc.SetActive(true);
        loadingBar.gameObject.SetActive(false);
    }

    private void RedrawChunk(Chunk c)
    {
        DestroyImmediate(c.GetComponent<MeshFilter>());
        DestroyImmediate(c.GetComponent<MeshRenderer>());
        DestroyImmediate(c.GetComponent<Collider>());
        c.CreateChunk(chunkDimensions, c.location, false);
    }

    WaitForSeconds threeSeconds = new WaitForSeconds(3.0f);
    IEnumerator HealBlock(Chunk c, int blockIndex)
    {
        yield return threeSeconds;

        if (c.chunkData[blockIndex] != MeshUtils.BlockType.AIR)
        {
            c.healthData[blockIndex] = MeshUtils.BlockType.NOCRACK;
            RedrawChunk(c);
        }
    }
    
    WaitForSeconds dropDelay = new WaitForSeconds(0.1f);

    IEnumerator Drop(Chunk c, int blockIndex, int strength = 6)
    {
        if (!MeshUtils.canDrop.Contains(c.chunkData[blockIndex])) yield break;

        yield return dropDelay;

        while (true)
        {
            var thisBlock = FromFlat(blockIndex);
            var neighbourBlock = GetWorldNeighbour(new Vector3Int(thisBlock.x, thisBlock.y - 1, thisBlock.z),
                Vector3Int.CeilToInt(c.location));
            var block = neighbourBlock.Item1;
            var neighbourBlockIndex = ToFlat(block);
            var neighbourChunk = chunks[neighbourBlock.Item2];

            if (neighbourChunk != null && neighbourChunk.chunkData[neighbourBlockIndex] == MeshUtils.BlockType.AIR)
            {
                neighbourChunk.chunkData[neighbourBlockIndex] = c.chunkData[blockIndex];
                neighbourChunk.healthData[neighbourBlockIndex] = MeshUtils.BlockType.NOCRACK;
                
                var nBlockAbove = GetWorldNeighbour(new Vector3Int(thisBlock.x, thisBlock.y + 1, thisBlock.z),
                    Vector3Int.CeilToInt(c.location));
                var blockAbove = nBlockAbove.Item1;
                var nBlockAboveIndex = ToFlat(blockAbove);
                var nChunkAbove = chunks[nBlockAbove.Item2];
                
                c.chunkData[blockIndex] = MeshUtils.BlockType.AIR;
                c.healthData[blockIndex] = MeshUtils.BlockType.NOCRACK;

                StartCoroutine(Drop(nChunkAbove, nBlockAboveIndex));

                yield return dropDelay;
                RedrawChunk(c);
                
                if (neighbourChunk != c)
                    RedrawChunk(neighbourChunk);

                c = neighbourChunk;
                blockIndex = neighbourBlockIndex;
            }
            else if (MeshUtils.canFlow.Contains(c.chunkData[blockIndex]))
            {
                FlowIntoNeighbour(thisBlock, Vector3Int.CeilToInt(c.location), Vector3Int.right, strength--);
                FlowIntoNeighbour(thisBlock, Vector3Int.CeilToInt(c.location), Vector3Int.left, strength--);
                FlowIntoNeighbour(thisBlock, Vector3Int.CeilToInt(c.location), Vector3Int.forward, strength--);
                FlowIntoNeighbour(thisBlock, Vector3Int.CeilToInt(c.location), Vector3Int.back, strength--);
                yield break;
            }
            else
                yield break;
        }
    }

    public void FlowIntoNeighbour(Vector3Int blockPos, Vector3Int chunkPos, Vector3Int neighbourDir, int strength)
    {
        strength--;

        if (strength <= 0) return;

        var neighbourPos = blockPos + neighbourDir;
        var neighbourBlock = GetWorldNeighbour(neighbourPos, chunkPos);
        var block = neighbourBlock.Item1;
        var neighbourBlockIndex = ToFlat(block);
        var neighbourChunk = chunks[neighbourBlock.Item2];

        if (neighbourChunk == null) return;
        
        if (neighbourChunk.chunkData[neighbourBlockIndex] == MeshUtils.BlockType.AIR)
        {
            neighbourChunk.chunkData[neighbourBlockIndex] = chunks[chunkPos].chunkData[ToFlat(blockPos)];
            neighbourChunk.healthData[neighbourBlockIndex] = MeshUtils.BlockType.NOCRACK;
            RedrawChunk(neighbourChunk);
            StartCoroutine(Drop(neighbourChunk, neighbourBlockIndex, strength--));
        }
    }

    IEnumerator BuildCoordinator()
    {
        while (true)
        {
            while (buildQueue.Count > 0)
                yield return StartCoroutine(buildQueue.Dequeue());
            yield return null;
        }
    }

    void BuildChunkByColumn(int x, int z, bool meshEnabled = true)
    {
        for (var y = 0; y < worldDimensions.y; y++)
        {
            var pos = new Vector3Int(x, y * chunkDimensions.y, z);

            if (!chunkChecker.Contains(pos))
            {
                var chunk = Instantiate(chunkPrefab);
                chunk.name = $"Chunk[{pos.x},{pos.y},{pos.z}]";
                var c = chunk.GetComponent<Chunk>();
                c.CreateChunk(chunkDimensions, pos);
                chunkChecker.Add(pos);
                chunks.Add(pos, c);
            }
            chunks[pos].meshRendererSolid.enabled = meshEnabled;
            chunks[pos].meshRendererFluid.enabled = meshEnabled;
        }

        chunkColumns.Add(new Vector2Int(x, z));
    }

    IEnumerator BuildExtraWorld()
    {
        var xEnd = worldDimensions.x + extraWorldDimensions.x;
        var xStart = worldDimensions.x;
        var zEnd = worldDimensions.z + extraWorldDimensions.z;
        var zStart = worldDimensions.z;
        
        for (var z = zStart; z < zEnd; z++)
        {
            for (var x = 0; x < xEnd; x++)
            {
                BuildChunkByColumn(x * chunkDimensions.x, z * chunkDimensions.z, false);
                yield return null;
            }
        }
        
        for (var z = 0; z < zEnd; z++)
        {
            for (var x = xStart; x < xEnd; x++)
            {
                BuildChunkByColumn(x * chunkDimensions.x, z * chunkDimensions.z, false);
                yield return null;
            }
        }
    }

    IEnumerator BuildWorld()
    {
        for (var z = 0; z < worldDimensions.z; z++)
        {
            for (var x = 0; x < worldDimensions.x; x++)
            {
                BuildChunkByColumn(x * chunkDimensions.x, z * chunkDimensions.z);
                loadingBar.value++;
                yield return null;
            }
        }

        int xPos = (worldDimensions.x / 2) * chunkDimensions.x;
        int zPos = (worldDimensions.z / 2) * chunkDimensions.z;
        int yPos = (int)MeshUtils.fBM(xPos, zPos, surfaceSettings.octaves, surfaceSettings.scale, surfaceSettings.heightScale, surfaceSettings.heightOffset) + 10;

        fpc.transform.position = new Vector3Int(xPos, yPos, zPos);
        ResetUIAfterLoad();
        lastBuildPos = Vector3Int.CeilToInt(fpc.transform.position);

        StartCoroutine(BuildCoordinator());
        StartCoroutine(UpdateWorld());
        StartCoroutine(BuildExtraWorld());
    }

    private WaitForSeconds wfs = new WaitForSeconds(0.5f);
    IEnumerator UpdateWorld()
    {
        while (true)
        {
            if ((lastBuildPos - fpc.transform.position).sqrMagnitude > Mathf.Pow(chunkDimensions.x, 2))
            {
                lastBuildPos = Vector3Int.CeilToInt(fpc.transform.position);
                int posx = (int)(fpc.transform.position.x / chunkDimensions.x) * chunkDimensions.x;
                int posz = (int)(fpc.transform.position.z / chunkDimensions.z) * chunkDimensions.z;
                buildQueue.Enqueue(BuildRecursiveWorld(posx, posz, drawRadius));
                buildQueue.Enqueue(HideColumns(posx, posz));
            }

            yield return wfs;
        }
    }

    public void HideChunkColumn(int x, int z)
    {
        for (int y = 0; y < worldDimensions.y; y++)
        {
            var pos = new Vector3Int(x, y * chunkDimensions.y, z);

            if (chunkChecker.Contains(pos))
            {
                chunks[pos].meshRendererSolid.enabled = false;
                chunks[pos].meshRendererFluid.enabled = false;
            }
        }
    }

    IEnumerator HideColumns(int x, int z)
    {
        var fpcPos = new Vector2Int(x, z);

        foreach (var cc in chunkColumns)
        {
            if ((cc - fpcPos).sqrMagnitude >= Mathf.Pow(drawRadius * chunkDimensions.x, 2))
            {
                HideChunkColumn(cc.x, cc.y);
            }
        }

        yield return null;
    }

    IEnumerator BuildRecursiveWorld(int x, int z, int rad)
    {
        int nextRadius = rad - 1;

        if (rad <= 0) yield break;
        
        BuildChunkByColumn(x, z + chunkDimensions.z);
        buildQueue.Enqueue(BuildRecursiveWorld(x, z + chunkDimensions.z, nextRadius));

        yield return null;
        
        BuildChunkByColumn(x, z - chunkDimensions.z);
        buildQueue.Enqueue(BuildRecursiveWorld(x, z - chunkDimensions.z, nextRadius));

        yield return null;
        
        BuildChunkByColumn(x + chunkDimensions.x, z);
        buildQueue.Enqueue(BuildRecursiveWorld(x + chunkDimensions.x, z, nextRadius));

        yield return null;
        
        BuildChunkByColumn(x - chunkDimensions.x, z);
        buildQueue.Enqueue(BuildRecursiveWorld(x - chunkDimensions.x, z, nextRadius));

        yield return null;
    }
}
