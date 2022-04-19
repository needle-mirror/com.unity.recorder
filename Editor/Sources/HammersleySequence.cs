using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A utility class to compute samples using the Hammersley sequence.
    /// </summary>
    /// <remarks>
    /// See paper "Sampling with Hammersley and Halton Points"
    /// http://www.cse.cuhk.edu.hk/~ttwong/papers/udpoint/udpoint.pdf
    /// </remarks>
    static class HammersleySequence
    {
        /// <summary>
        /// Evaluates Hammersley points on the unit square.
        /// </summary>
        /// <param name="result">Array holding the resulting points.</param>
        public static void GetPoints(Vector2[] result)
        {
            for (var i = 0; i < result.Length; ++i)
            {
                var x = 0f;
                var p = 0.5f;

                for (var k = i; k > 0; k >>= 1)
                {
                    if ((k & 1) == 1) // mod 2 == 1
                    {
                        x += p;
                    }

                    p *= 0.5f;
                }

                var y = (i + 0.5f) / result.Length;

                result[i] = new Vector2(x, y);
            }
        }
    }
}
