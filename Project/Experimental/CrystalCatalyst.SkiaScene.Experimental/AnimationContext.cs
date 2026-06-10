namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Provides animation context information to elements.  This can be extended in the future
    /// to carry global animation state or input events.
    /// </summary>
    public class AnimationContext
    {
        /// <summary>
        /// Total elapsed time in seconds since the start of the animation.  This value is
        /// incremented by <see cref="Advance"/> each frame.
        /// </summary>
        public double TotalSeconds { get; private set; } = 0.0;

        /// <summary>
        /// The number of frames that have been processed.  This value is incremented each time
        /// <see cref="Advance"/> is called.
        /// </summary>
        public long FrameIndex { get; private set; } = 0;

        /// <summary>
        /// Advance the animation context by the given time delta.  This updates
        /// <see cref="TotalSeconds"/> and increments <see cref="FrameIndex"/>.
        /// </summary>
        /// <param name="dt">The time delta in seconds.</param>
        public void Advance(double dt)
        {
            if (dt < 0)
            {
                return;
            }
            TotalSeconds += dt;
            FrameIndex++;
        }
    }
}
