using CommsRadioAPI;
using DV;
using DV.Localization;
using LocoOwnership.LocoPurchaser;
using LocoOwnership.Menus;

namespace LeasableLocos;

public class CustomTransactionPurchaseFail : AStateBehaviour
{
    public CustomTransactionPurchaseFail(int failState)
        : base(new CommsRadioState(LocalizationAPI.L("lo/radio/general/purchase"), LocalizationAPI.L(failReasons[failState]), LocalizationAPI.L("comms/confirm"), LCDArrowState.Off, LEDState.Off, ButtonBehaviourType.Override))
    {
    }

    public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
    {
        if (action != InputAction.Activate)
        {
            return this;
        }
        utility.PlaySound(VanillaSoundCommsRadio.Confirm, null);
        return new LocoPurchase();
    }
    private static readonly string[] failReasons = ["lo/radio/pfail/custom/content/0"];
}