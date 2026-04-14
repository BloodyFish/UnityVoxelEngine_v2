using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.V2
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