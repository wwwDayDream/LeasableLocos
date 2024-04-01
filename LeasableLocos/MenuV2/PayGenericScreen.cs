using System;
using System.Linq;
using DV.ServicePenalty.UI;
using TMPro;

namespace LeasableLocos.MenuV2;

public class PayGenericScreen : ModularScreen
{
    public PayGenericScreen(IModularScreen? parent, ModularScreenHost? host, LeaseScreen leaseScreen) : base(parent, host)
    {
        LeaseScreen = leaseScreen;
        Show = OnShow;
        Hide = OnHide;
        Input = OnInput;
    }

    public string Title { get; set; } = string.Empty;
    public string ParagraphInfo { get; set; } = string.Empty;
    public double Cost { get; set; }
    public Func<bool> OnBuy { get; set; } = null!;
    public IModularScreen? AfterFail { get; set; }
    public IModularScreen? AfterSuccess { get; set; }

    public void Pay(double cost, Func<bool> onComplete)
    {
        Cost = cost;
        OnBuy = onComplete;
        SwitchToScreen(this);
    }
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Title!.text = Title;
        Title = string.Empty;
        LeaseScreen.Subtitle!.text = CareerManagerLocalization.INSERT_WALLET_TO_PAY;

        LeaseScreen.Paragraphs.ParagraphB.text = ParagraphInfo;
        ParagraphInfo = string.Empty;

        LeaseScreen.FeesPayingScreen.cashReg.SetTotalCost(Cost);
        LeaseScreen.FeesPayingScreen.cashReg.CashAdded += UpdateDepositedCash;
        
        UpdateDepositedCash();
    }
    private void UpdateDepositedCash()
    {
        LeaseScreen.Lines.Last().lhs.horizontalAlignment = HorizontalAlignmentOptions.Right;
        LeaseScreen.Lines.Last().lhs.text = CareerManagerLocalization.DEPOSITED;
        LeaseScreen.Lines.Last().rhs.text = $"${LeaseScreen.FeesPayingScreen.cashReg.DepositedCash:F2}";
    }
    private void OnHide(IModularScreen? next)
    {
        LeaseScreen.FeesPayingScreen.cashReg.CashAdded -= UpdateDepositedCash;
        
        if (LeaseScreen.FeesPayingScreen.cashReg.DepositedCash >= 0d)
            LeaseScreen.FeesPayingScreen.cashReg.ClearCurrentTransaction();
    }
    private void OnInput(InputAction action)
    {
        var success = false;
        switch (action)
        {
            case InputAction.Confirm:
                if (LeaseScreen.FeesPayingScreen.cashReg.Buy())
                {
                    success = true;
                    if (OnBuy?.Invoke() ?? false) return;
                }
                break;
            case InputAction.Cancel:
            case InputAction.None:
            case InputAction.Up:
            case InputAction.Down:
            case InputAction.PrintInfo:
            default:
                break;
        }

        SwitchToScreen((!success ? AfterFail : AfterSuccess) ?? Parent ?? LeaseScreen);
        AfterFail = null;
        AfterSuccess = null;
    }

    public LeaseScreen LeaseScreen { get; }
}