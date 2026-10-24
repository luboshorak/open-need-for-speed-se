namespace GameEngine
{
    /// <summary>
    /// Display projection shared across rendering and the independent engine
    /// input assembly, without coupling GameEngine to OpenNFS1.GameConfig.
    /// </summary>
    public static class DisplaySettings
    {
        public static bool IntegerScaling { get; set; }
    }
}
