using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using LeasableLocos.SaveData;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LeasableLocos.Patches;

[HarmonyPatch(typeof(WorldStreamingInit), nameof(WorldStreamingInit.LoadingRoutine), MethodType.Enumerator)]
[UsedImplicitly]
public static class WorldStreamingInitLoadingRoutine
{
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator generator)
    {
        var getSaveData = typeof(AStartGameData).GetMethod(nameof(AStartGameData.GetSaveGameData));
        var mySaveData = typeof(WorldStreamingInitLoadingRoutine).GetMethod(nameof(GetSaveGameData));

        foreach (var instruction in codeInstructions)
        {
            if (instruction.Calls(getSaveData))
            {
                yield return new CodeInstruction(OpCodes.Call, mySaveData);
            }
            else
                yield return instruction;
        }
    }

    public static SaveGameData GetSaveGameData(AStartGameData startGameData)
    {
        var getSaveGame = startGameData.GetSaveGameData();
        SaveDataManager.LoadFromSave(getSaveGame);
        return getSaveGame;
    }
}