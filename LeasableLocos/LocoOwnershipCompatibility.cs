using System.Linq;
using CommsRadioAPI;
using DVLangHelper.Data;
using DVLangHelper.Runtime;
using HarmonyLib;
using LeasableLocos.SaveData;
using LocoOwnership.LocoPurchaser;

namespace LeasableLocos;

internal static class LocoOwnershipCompatibility
{

    internal static void HandleLocoOwnershipCompatibility()
    {
        Plugin.Logger?.Log("LocoOwnership is installed; Proceeding with patches...");

        var translationInjector = new TranslationInjector("wwwDayDream.LeasableLocos");
        translationInjector.AddTranslation("lo/radio/pfail/custom/content/0", DVLanguage.English, "You must pay your lease dues at the Career Manager.");

        Plugin.Patcher?.Patch(typeof(TransactionPurchaseConfirm).GetMethod(nameof(TransactionPurchaseConfirm.OnAction)),
            prefix: new HarmonyMethod(LocoOwnershipCompatibility.PreLocoOwnershipPurchase), 
            postfix: new HarmonyMethod(LocoOwnershipCompatibility.PostLocoOwnershipPurchase));
    }
    internal static bool PreLocoOwnershipPurchase(TransactionPurchaseConfirm __instance, CommsRadioUtility utility, InputAction action, ref AStateBehaviour __result)
    {
        if (action != InputAction.Activate) return true;
        if (!__instance.highlighterState) return true;
        var lease = SaveDataManager.SavedLeases.FirstOrDefault(savedLease => savedLease.LocosID.Contains(__instance.selectedCar.ID));
        if (lease == null) return true;
        if (lease.IncurredDebt <= 0d) return true;
        utility.PlaySound(VanillaSoundCommsRadio.Warning);
        __result = new CustomTransactionPurchaseFail(0);
        return false;
    }

    internal static void PostLocoOwnershipPurchase(TransactionPurchaseConfirm __instance, AStateBehaviour __result)
    {
        if (__result is not TransactionPurchaseSuccess) return;
        var lease = SaveDataManager.SavedLeases.FirstOrDefault(savedLease => savedLease.LocosID.Contains(__instance.selectedCar.ID));
        lease?.Terminate(false);
    }
}