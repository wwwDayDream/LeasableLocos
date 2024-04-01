using HarmonyLib;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(StationLocoSpawner), nameof(StationLocoSpawner.Update))]
public static class StationLocoSpawnerUpdate
{
    public static bool Prefix() => false;
}