using HarmonyLib;
using JetBrains.Annotations;
using LeasableLocos.SaveData;
using Newtonsoft.Json.Linq;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.UpdateInternalData))]
[UsedImplicitly]
public static class SaveGameManagerUpdateInternalData
{
    [UsedImplicitly]
    public static void Postfix(SaveGameManager __instance)
    {
        SaveDataManager.SaveToJObject(__instance.data);
    }
}