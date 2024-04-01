using HarmonyLib;
using LeasableLocos.SaveData;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(TOD_Time), nameof(TOD_Time.Awake))]
public static class TOD_TimeAwake
{
    public static void Postfix(TOD_Time __instance)
    {
        __instance.OnHour += SaveDataManager.InvokeUpdateLeaseTimes;
    }
}