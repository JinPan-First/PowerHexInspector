namespace Community.PowerToys.Run.Plugin.HexInspector
{
    public enum Base
    {
        Invalid = -1,
        Bin = 2,
        Oct = 8,
        Dec = 10,
        Hex = 16,
        Fra16 = 32,
        Fra32 = 64,
        Fra64 = 128,
        Ascii = 256,
    }

    public enum Endian
    {
        LittleEndian = 0,
        BigEndian = 1,
    }
}