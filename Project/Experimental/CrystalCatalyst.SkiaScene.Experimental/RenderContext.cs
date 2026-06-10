namespace CrystalCatalyst.SkiaScene.Experimental
{
    /// <summary>
    /// Provides rendering context information to elements.  This can be extended
    /// in the future to carry global render state, such as an opacity stack or
    /// transformation stack.
    /// </summary>
    public class RenderContext
    {
        /// <summary>
        /// Width of the viewport in pixels.  Some procedural elements may use this value to
        /// normalize coordinates.
        /// </summary>
        public int ViewportWidth { get; set; }

        /// <summary>
        /// Height of the viewport in pixels.  Some procedural elements may use this value to
        /// normalize coordinates.
        /// </summary>
        public int ViewportHeight { get; set; }

        /// <summary>
        /// Total elapsed time in seconds that corresponds to the current frame.  This may be
        /// derived from the <see cref="AnimationContext"/> when updating the scene.
        /// </summary>
        public double TotalSeconds { get; set; }
    }
}
