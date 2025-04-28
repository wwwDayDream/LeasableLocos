using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommsRadioAPI;
using DV;
using DV.Localization;
using DVLangHelper.Data;
using DVLangHelper.Runtime;
using HarmonyLib;
using LeasableLocos.SaveData;
using LocoOwnership.LocoPurchaser;
using LocoOwnership.Menus;

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
        __result = (NewCustomTransactionPurchaseFail(0) as AStateBehaviour)!;
        return false;
    }

    internal static void PostLocoOwnershipPurchase(TransactionPurchaseConfirm __instance, AStateBehaviour __result)
    {
        if (__result is not TransactionPurchaseSuccess) return;
        var lease = SaveDataManager.SavedLeases.FirstOrDefault(savedLease => savedLease.LocosID.Contains(__instance.selectedCar.ID));
        lease?.Terminate(false);
    }

    private static Type? customTransactionPurchaseFailType = null;

    private static object NewCustomTransactionPurchaseFail(int failState) =>
        Activator.CreateInstance(CustomTransactionPurchaseFailType, failState);
    private static Type CustomTransactionPurchaseFailType
    {
        get
        {
            if (customTransactionPurchaseFailType != null) return customTransactionPurchaseFailType;
            var runtimeAssemblyName = new AssemblyName("LeasableLocos.Runtime");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(runtimeAssemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(runtimeAssemblyName.Name);
            
            var typeBuilder = moduleBuilder.DefineType(
                "CustomTransactionPurchaseFail", 
                TypeAttributes.Public | TypeAttributes.Class, 
                typeof(AStateBehaviour));
            
            
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                [typeof(int)]
            );
            var ilCtor = constructorBuilder.GetILGenerator();
            var createStateMethod = typeof(CustomTransactionPurchaseFailHelpers).GetMethod(nameof(CustomTransactionPurchaseFailHelpers.CreateState));
            var baseCtor = typeof(AStateBehaviour).GetConstructor([typeof(CommsRadioState)]);

            ilCtor.Emit(OpCodes.Ldarg_0); // this
            ilCtor.Emit(OpCodes.Ldarg_1); // failState
            ilCtor.Emit(OpCodes.Call, createStateMethod); // call CreateState(failState), leaves CommsRadioState on stack
            ilCtor.Emit(OpCodes.Call, baseCtor); // call base constructor
            ilCtor.Emit(OpCodes.Ret);

            var onActionMethodBuilder = typeBuilder.DefineMethod(
                "OnAction",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(AStateBehaviour),
                [typeof(CommsRadioUtility), typeof(InputAction)]
            );
            var ilOnAction = onActionMethodBuilder.GetILGenerator();

            var onActionHelperMethod = typeof(CustomTransactionPurchaseFailHelpers).GetMethod(nameof(CustomTransactionPurchaseFailHelpers.OnAction));
            
            ilOnAction.Emit(OpCodes.Ldarg_0); // this
            ilOnAction.Emit(OpCodes.Ldarg_1); // utility
            ilOnAction.Emit(OpCodes.Ldarg_2); // action
            ilOnAction.Emit(OpCodes.Call, onActionHelperMethod); // call helper
            ilOnAction.Emit(OpCodes.Ret);
            
            typeBuilder.DefineMethodOverride(onActionMethodBuilder, typeof(AStateBehaviour).GetMethod("OnAction")!);

            customTransactionPurchaseFailType = typeBuilder.CreateType();

            return customTransactionPurchaseFailType;
        }
    }
}

public static class CustomTransactionPurchaseFailHelpers
{
    public static CommsRadioState CreateState(int failState)
    {
        return new CommsRadioState(
            LocalizationAPI.L("lo/radio/general/purchase"),
            LocalizationAPI.L(failReasons[failState]),
            LocalizationAPI.L("comms/confirm"),
            LCDArrowState.Off,
            LEDState.Off,
            ButtonBehaviourType.Override);
    }

    public static AStateBehaviour OnAction(AStateBehaviour self, CommsRadioUtility utility, InputAction action)
    {
        if (action != InputAction.Activate)
            return self;

        utility.PlaySound(VanillaSoundCommsRadio.Confirm, null);
        return new LocoPurchase();
    }

    private static readonly string[] failReasons = ["lo/radio/pfail/custom/content/0"];
}
