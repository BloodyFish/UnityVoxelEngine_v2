using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class Generation : MonoBehaviour
{
    [SerializeField] int seed;
    [SerializeField] float frequency;
    [SerializeField] int octaves;
    [SerializeField] float lacunarity;
    [SerializeField] float gain;

    public static System.Random random;
    public static Dictionary<Vector3, Chunk> chunkDictionary = new Dictionary<Vector3, Chunk>();

    [DllImport("VoxelEngine_v2", EntryPoint = "NoiseInit")]
    public static extern void NoiseInit(int seed, float frequency, int octaves, float lacunarity, float gain);

    private void OnEnable()
    {
        // In order to get the correct texture, we need to get all the possible blocks out of the Resources folder
        // The blocks in possibleBlocks should be sorted by their blockID.
        // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
        List<Block> possibleBlocks_list = Resources.LoadAll<Block>("Blocks").ToList();
        possibleBlocks_list.Sort(new BlockComparer());
        Block.possibleBlocks = possibleBlocks_list.ToArray();

        random = new System.Random(seed);
    }

    void Start()
    {
        NoiseInit(seed, frequency / 1000, octaves, lacunarity, gain);
        StartCoroutine(GenerateChunk());
    }

    IEnumerator GenerateChunk()
    {
        for (int x = 0; x < 64; x++)
        {
            for (int z = 0; z < 64; z++)
            {
                Chunk chunk = new Chunk(16, 16, 384, new Vector3Int(x * 16, 0, z * 16));
                chunkDictionary.Add(new Vector3Int(x * 16, 0, z * 16), chunk);
                chunk.Generate();

                yield return new WaitForEndOfFrame();
            }
        }
    }

}
