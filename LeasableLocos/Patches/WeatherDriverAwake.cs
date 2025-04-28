using DV.WeatherSystem;
using HarmonyLib;
using LeasableLocos.SaveData;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(WeatherDriver), nameof(WeatherDriver.Start))]
public static class WeatherDriverStart
{
    public static void Postfix(WeatherDriver __instance)
    {
        __instance.manager.TimeJump += SaveDataManager.InvokeUpdateLeaseTimes;
    }
}