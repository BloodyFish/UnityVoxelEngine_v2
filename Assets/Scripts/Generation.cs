using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class Generation : MonoBehaviour
    {
        [SerializeField] NoiseParameters noise2DParam;
        [SerializeField] NoiseParameters noise3DParam;
        public static int WATER_LEVEL = 63;
        public static int BEACH_HEIGHT = 3;
        public const int SCALE = 320;

        [SerializeField] bool randomizeSeed;
        [SerializeField] int seedInput;
        public static int seed;

        public static ConcurrentDictionary<Vector3, Chunk> chunkDictionary = new ConcurrentDictionary<Vector3, Chunk>();
        public static ConcurrentDictionary<Vector3, int[]> bufferDictionary = new ConcurrentDictionary<Vector3, int[]>();

        public static SemaphoreSlim semaphore;


        // FOR TESTING
        public static Tree defaultTree;

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

            if (randomizeSeed) seed = (int)(Random.value * int.MaxValue);
            else seed = seedInput;
        }

        void Start()
        {
            // Restrict the amount of threads to be between 4 and 8
            semaphore = new SemaphoreSlim(Mathf.Clamp(SystemInfo.processorCount / 2, 4, 8));

            print("Seed: " + seed);

            Chunk.Width = 16;
            Chunk.Length = 16;
            Chunk.Height = 384;

            Noise.Init2D(seed, noise2DParam.frequency / 1000, noise2DParam.octaves, noise2DParam.lacunarity, noise2DParam.gain);
            Noise.Init3D(seed, noise3DParam.frequency / 1000, noise3DParam.octaves, noise3DParam.lacunarity, noise3DParam.gain);

            StartCoroutine(GenerateChunk());
            StartCoroutine(MeshChunks());

            defaultTree = Resources.Load<Tree>("Trees/BasicTree");
        }

        IEnumerator GenerateChunk()
        {
            for (int x = 0; x < 64; x++)
            {
                for (int z = 0; z < 64; z++)
                {
                    Vector3Int pos = new Vector3Int(x * Chunk.Width, 0, z * Chunk.Length);
                    Chunk chunk = new Chunk(pos);

                    semaphore.Wait();

                    Task.Run(() =>
                    {
                        try
                        {
                            chunk.Generate();
                        }
                        finally
                        {
                            semaphore.Release();
                        }
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
                    if (chunk.generationPhase == Chunk.GenerationPhase.OPEN_FOR_MESH_GEN)
                    {
                        chunk.Meshify();
                        yield return null;
                    }
                }
                yield return null;

            }
        }
    }
}