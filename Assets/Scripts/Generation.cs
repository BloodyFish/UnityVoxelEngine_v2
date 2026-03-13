using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class Generation : MonoBehaviour
{
    [SerializeField] NoiseParameters noise2DParam;
    [SerializeField] NoiseParameters noise3DParam;
    public static int WATER_LEVEL = 63;
    public static int BEACH_HEIGHT = 3;
    public const int SCALE = 320;

    public static int seed = 77777777;

    public static Dictionary<Vector3, Chunk> chunkDictionary = new Dictionary<Vector3, Chunk>();

    [DllImport("VoxelEngine_v2", EntryPoint = "NoiseInit_2D")]
    public static extern void NoiseInit_2D(int seed, float frequency, int octaves, float lacunarity, float gain);

    [DllImport("VoxelEngine_v2", EntryPoint = "NoiseInit_3D")]
    public static extern void NoiseInit_3D(int seed, float frequency, int octaves, float lacunarity, float gain);


    private static Dictionary<float, float> continentalnessToHeight = new Dictionary<float, float>()
    {
        // Negative continentalness values will map to ocean and beach heights
        {-1f, 0f },
        {-0.5f, 5f},
        {-0.2f, WATER_LEVEL},
        {-0.1f, 65},

        // Positive continentalness will map to mountain and hilly heights
        {0f, 70f},
        {0.3f, 100},
        {1f, SCALE }

    };

    public static float[] continentalness = continentalnessToHeight.Keys.ToArray();
    public static float[] heightFromContinentalness = continentalnessToHeight.Values.ToArray();

    private void OnEnable()
    {
        // In order to get the correct texture, we need to get all the possible blocks out of the Resources folder
        // The blocks in possibleBlocks should be sorted by their blockID.
        // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
        List<Block> possibleBlocks_list = Resources.LoadAll<Block>("Blocks").ToList();
        possibleBlocks_list.Sort(new BlockComparer());
        Block.possibleBlocks = possibleBlocks_list.ToArray();
    }

    void Start()
    {
        Chunk.Width = 16;
        Chunk.Length = 16;
        Chunk.Height = 384;

        NoiseInit_2D(seed, noise2DParam.frequency / 1000, noise2DParam.octaves, noise2DParam.lacunarity, noise2DParam.gain);
        NoiseInit_3D(seed, noise3DParam.frequency / 1000, noise3DParam.octaves, noise3DParam.lacunarity, noise3DParam.gain);

        StartCoroutine(GenerateChunk());
        StartCoroutine(MeshChunks());
    }


    IEnumerator GenerateChunk()
    {
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                Chunk chunk = new Chunk(new Vector3Int(x * 16, 0, z * 16));
                chunkDictionary.Add(new Vector3Int(x * 16, 0, z * 16), chunk);

                Task.Run(() =>
                {
                    chunk.Generate();
                });

                yield return null;
            }
        }
    }

    IEnumerator MeshChunks()
    {
        while (true)
        {
            foreach (Chunk chunk in Chunk.busyChunks.ToList())
            {
                if (!chunk.isGenerating && !chunk.isMeshing)
                {
                    chunk.Meshify();
                    yield return null;
                }
            }

            yield return null;

        }
    }
}
