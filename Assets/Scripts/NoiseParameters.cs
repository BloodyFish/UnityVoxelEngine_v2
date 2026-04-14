using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    [CreateAssetMenu]
    public class NoiseParameters : ScriptableObject
    {
        public float frequency;
        public int octaves;
        public float lacunarity;
        public float gain;
    }
}