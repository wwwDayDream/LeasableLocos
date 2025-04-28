using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityModManagerNet;
using DV.ServicePenalty.UI;
using LeasableLocos.MenuV2;
using UnityEngine;

namespace LeasableLocos;

[UsedImplicitly]
internal static class Plugin
{
    internal static UnityModManager.ModEntry.ModLogger? Logger { get; private set; }
    internal static Harmony? Patcher { get; set; }

    [UsedImplicitly]
    internal static bool Load(UnityModManager.ModEntry modEntry)
    {
        Logger = modEntry.Logger;
        Patcher = new Harmony(modEntry.Info.Id);

        Config.Load(modEntry.Path);

        modEntry.OnGUI += OnGUI;
        modEntry.OnSaveGUI += OnSaveGUI;

        Patcher.PatchAll();

        CareerManagerAPI.CareerManagerAPI.StationCareerManagerAwake += (screen, name) =>
        {
            screen.TryAddToMainScreen<LeaseScreen>("Loco Leasing", CareerManagerAPI.CareerManagerAPI.CareerModeOnly,
                after: CareerManagerLocalization.LICENSES);
        };

        if (!IsLocoOwnershipVersionEnabled()) return true;

        Task.Run(async () =>
        {
            if (!TryGetLocoOwnership(out var locoOwnership)) return;
            while (!locoOwnership!.Loaded)
                await Task.Delay(1000);
            
            LocoOwnershipCompatibility.HandleLocoOwnershipCompatibility();
        });

        return true;
    }

    private static bool IsLocoOwnershipVersionEnabled() =>
        TryGetLocoOwnership(out _);

    private static bool TryGetLocoOwnership(out UnityModManager.ModEntry? locoOwnership) =>
        (locoOwnership = UnityModManager.modEntries.FirstOrDefault(entry => entry.Info.Id == "LocoOwnership" && entry.Info.Version == "1.4.1")) !=
        null;
    
    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Config.Save(modEntry.Path);
    }
    private static void OnGUI(UnityModManager.ModEntry obj)
    {
        GUILayout.Label("Lease Percentage of Full Repair Cost (0.0000-100.0000)");
        if (double.TryParse(GUILayout.TextField((Config.EnginePercentageOfFullUnitPrice * 100d).ToString("F4"), 20), out var newDayToDa))
            Config.EnginePercentageOfFullUnitPrice = newDayToDa / 100d > 1d ? 1d : newDayToDa / 100d;
        GUILayout.Label("Termination With Past Due Fees. Percentage of Fees (0-100)");
        if (double.TryParse(GUILayout.TextField((Config.TerminatePrePayOffPercentage * 100d).ToString("F0"), 20), out var newTerm))
            Config.TerminatePrePayOffPercentage = newTerm / 100d > 1d ? 1d : newTerm / 100d;
        GUILayout.Label("Percentage of Health to Allow Termination (0.00-100.00)");
        if (double.TryParse(GUILayout.TextField((Config.InGoodHealthPercentage * 100d).ToString("F2"), 20), out var inGoodHealth))
            Config.InGoodHealthPercentage = inGoodHealth / 100d > 1d ? 1d : inGoodHealth / 100d;
        // GUILayout.Label("Hour of Day to Issue Lease Fees (01-24)");
        // if (int.TryParse(GUILayout.TextField(Config.LeaseFeeTime.ToString(), 10), out var newFeeTime))
        //     Config.LeaseFeeTime = newFeeTime;
        GUILayout.Label("Days Lease is Unpaid before Overdue (00)");
        if (int.TryParse(GUILayout.TextField(Config.DaysUnpaidToOverdue.ToString(), 10), out var newOverdue))
            Config.DaysUnpaidToOverdue = newOverdue;
        GUILayout.Label("Percentage of DayToDay for Application Cost (00.00)");
        if (double.TryParse(GUILayout.TextField(Config.ApplicationPercentage.ToString("F2"), 10), out var newFee))
            Config.ApplicationPercentage = newFee;
        GUILayout.Label("Maximum Terminated Leases (0)");
        if (int.TryParse(GUILayout.TextField(Config.MaxTerminatedLeases.ToString("F0"), 10), out var newMaxTerm))
            Config.MaxTerminatedLeases = newMaxTerm;
    }
}