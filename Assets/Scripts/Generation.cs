using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Generation : MonoBehaviour
{
    [SerializeField] int seed;
    [SerializeField] float frequency;
    [SerializeField] int octaves;
    [SerializeField] float lacunarity;
    [SerializeField] float gain;

    public static Dictionary<Vector3, Chunk> chunkDictionary = new Dictionary<Vector3, Chunk>();

    [DllImport("VoxelEngine_v2", EntryPoint = "NoiseInit")]
    public static extern void NoiseInit(int seed, float frequency, int octaves, float lacunarity, float gain);

    void Start()
    {
        NoiseInit(seed, frequency, octaves, lacunarity, gain);

        for(int x = 0; x < 32; x++)
        {
            for(int z = 0; z < 32; z++)
            {
                StartCoroutine(GenerateChunk(x, z));            
            }
        }
    }

    IEnumerator GenerateChunk(int x, int z)
    {
        Chunk chunk = new Chunk(16, 16, 124, new Vector3Int(x * 16, 0, z * 16));
        chunkDictionary.Add(new Vector3Int(x * 16, 0, z * 16), chunk);

        // coroutines are not iterative, they can be executed by Unity in any order. Due to factors outside our control, we should put Generate() last
        chunk.Generate();

        yield return new WaitForEndOfFrame();
    }
}
