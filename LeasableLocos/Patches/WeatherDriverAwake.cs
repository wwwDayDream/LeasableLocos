using DV.WeatherSystem;
using HarmonyLib;
using LeasableLocos.SaveData;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(WeatherDriver), nameof(WeatherDriver.Awake))]
public static class WeatherDriverAwake
{
    public static void Postfix(WeatherDriver __instance)
    {
        __instance.manager.TimeJump += SaveDataManager.InvokeUpdateLeaseTimes;
    }
}