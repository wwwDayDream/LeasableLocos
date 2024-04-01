using System;
using System.IO;

namespace LeasableLocos;

internal static class Config
{
    private static string ConfigFile(string dir) => Path.Combine(dir, "config.dat");

    internal static double ApplicationFee = 99.99;
    internal static double EnginePercentageOfFullUnitPrice = 0.009612;
    internal static double TerminatePrePayOffPercentage = 0.2;
    internal static double InGoodHealthPercentage = 0.9;
    internal static int DaysUnpaidToOverdue = 3;
    internal static int MaxTerminatedLeases = 5;
    // static int LeaseFeeTime = 8;

    internal static void Load(string path)
    {
        if (!File.Exists(ConfigFile(path))) return;
        var reader = new BinaryReader(File.OpenRead(ConfigFile(path)));

        try
        {
            ApplicationFee = reader.ReadDouble();
            EnginePercentageOfFullUnitPrice = reader.ReadDouble();
            TerminatePrePayOffPercentage = reader.ReadDouble();
            InGoodHealthPercentage = reader.ReadDouble();
            DaysUnpaidToOverdue = reader.ReadInt32();
            MaxTerminatedLeases = reader.ReadInt32();
            // LeaseFeeTime = reader.ReadInt32();
            reader.Dispose();
        }
        catch (Exception e)
        {
            reader.Dispose();
            Save(path);
        }
    }
    internal static void Save(string path)
    {
        using var writer = new BinaryWriter(File.OpenWrite(ConfigFile(path)));
        writer.Write(ApplicationFee);
        writer.Write(EnginePercentageOfFullUnitPrice);
        writer.Write(TerminatePrePayOffPercentage);
        writer.Write(InGoodHealthPercentage);
        writer.Write(DaysUnpaidToOverdue);
        writer.Write(MaxTerminatedLeases);
        // writer.Write(LeaseFeeTime);
    }
}