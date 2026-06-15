public static class StargravePlayMode
{
    public enum Mode
    {
        Pc,
        VrQuest2
    }

    public static Mode Current { get; private set; } = Mode.Pc;
    public static bool IsVr => Current == Mode.VrQuest2;

    public static void SetMode(Mode mode)
    {
        Current = mode;
    }
}
