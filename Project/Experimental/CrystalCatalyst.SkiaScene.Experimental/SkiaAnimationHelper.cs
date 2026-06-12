using System;

namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Utility methods for common animation tasks in the experimental Skia scene graph.
    /// </summary>
    public static class SkiaAnimationHelper
    {
        /// <summary>
        /// Calculates a sine wave value for a given time.
        /// </summary>
        /// <param name="time">Current time in seconds.</param>
        /// <param name="frequency">Frequency in Hz (cycles per second).</param>
        /// <param name="amplitude">The peak deviation from the center.</param>
        /// <param name="phase">Phase shift in radians.</param>
        /// <returns>The sine value.</returns>
        public static float SineWave(double time, float frequency, float amplitude, float phase = 0f)
        {
            return amplitude * (float)Math.Sin(2.0 * Math.PI * frequency * time + phase);
        }

        /// <summary>
        /// Returns a value that loops between 0 and 1 over a given duration.
        /// </summary>
        /// <param name="time">Current time in seconds.</param>
        /// <param name="duration">Duration of one loop in seconds.</param>
        public static float Loop(double time, float duration)
        {
            if (duration <= 0) return 0f;
            return (float)((time % duration) / duration);
        }

        /// <summary>
        /// Ping-pongs a value between 0 and 1 over a given duration.
        /// </summary>
        /// <param name="time">Current time in seconds.</param>
        /// <param name="duration">Duration of one full cycle (0 to 1 and back to 0) in seconds.</param>
        public static float PingPong(double time, float duration)
        {
            if (duration <= 0) return 0f;
            float t = (float)((time % duration) / (duration / 2.0));
            return t > 1.0f ? 2.0f - t : t;
        }
    }
}
