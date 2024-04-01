using System.Collections.Generic;
using DV.InventorySystem;
using DV.ServicePenalty.UI;
using DV.Utils;
using LeasableLocos.SaveData;
using TMPro;
using UnityEngine;

namespace LeasableLocos.MenuV2;

public class ManageLease : ModularScreen
{
    public ManageLease(IModularScreen? parent, ModularScreenHost? host, LeaseScreen leaseScreen) : base(parent, host)
    {
        LeaseScreen = leaseScreen;
        PayScreen = new PayGenericScreen(this, Host, LeaseScreen);
        InfoScreen = new BasicInfoScreen(this, Host, LeaseScreen);
        
        Show = OnShow;
        Hide = OnHide;
        Input = OnInput;
    }

    public LeaseScreen LeaseScreen { get; }
    public PayGenericScreen PayScreen { get; }
    public BasicInfoScreen InfoScreen { get; set; }
    public SavedLease Lease { get; set; }
    
    public static readonly string[] LeaseOptions = [
        "TERMINATE",
        "PAY DEBT"
    ];

    public void Manage(SavedLease lease)
    {
        Lease = lease;
        SwitchToScreen(this);
    }
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Title!.text = Lease.StationID + " " + Lease.AggregatedIDs;
        LeaseScreen.Title.color = Lease.OriginStation.stationInfo.StationColor;

        if (!Lease.IsTerminated)
        {
            LeaseScreen.Paragraphs.ParagraphA.horizontalAlignment = HorizontalAlignmentOptions.Right;
            LeaseScreen.Paragraphs.ParagraphA.text = $"DAILY IN {Lease.HoursUntilIncurredDebt()} HRS";
            LeaseScreen.AltSubtitle!.text = $"${Lease.DailyRate:F2}";
        }
        
        LeaseScreen.Subtitle!.color = Lease.PastDue ? 
            Color.red * 0.8f + Color.cyan * 0.2f : 
            Lease.IsTerminated ? 
                Color.green * 0.8f + Color.magenta * 0.2f : Color.white * 0.7f;
        LeaseScreen.Subtitle!.text = Lease.PastDue ? "PAST-DUE" : Lease.IsTerminated ? "TERMINATED" : "OPEN";
        
        LeaseScreen.Scroller?.SetOptions(new List<(LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter? canEnter)>() {
            (
                option => option.text = "TERMINATE", 
                option => option.text = Lease is { IsTerminated: false, TerminationFee: > 0 } ? $"%{Config.TerminatePrePayOffPercentage * 100:F0}" : string.Empty,
                () => !Lease.IsTerminated
            ),(
                option => option.text = "PAY DEBT", 
                option => option.text = $"${Lease.IncurredDebt:F2}",
                () => Lease.IncurredDebt > 0d
            ),
        });
    }
    private void OnHide(IModularScreen? next)
    {
        
    }
    private void OnInput(InputAction action)
    {
        var playerMoney = SingletonBehaviour<Inventory>.Instance.PlayerMoney;
        switch (action)
        {
            case InputAction.Confirm when LeaseScreen.Scroller is { SelectedIndex: 0 } && !Lease.IsTerminated:
                if (!Lease.NotTooManyTerminated || !Lease.LocosInOriginYard || !Lease.LocosInGoodHealth || !Lease.LocosBrakeOn)
                {
                    InfoScreen.Display("Failed",
                    !Lease.NotTooManyTerminated ? "You have too many Terminated leases with debt remaining. Please clear some terminated leases." :
                            !Lease.LocosInOriginYard ? "The engine must must be in the origin yard in order to terminate the lease." :
                            !Lease.LocosInGoodHealth ? "The engine must be in good health in order to terminate the lease. Please pay your fees or take it in for maintenance." :
                            "The engine must have a handbrake engaged.", this);
                } else if (Lease.TerminationFee > 0)
                {
                    PayScreen.Title = $"Pay ${Lease.TerminationFee:F2} Termination Fee?";
                    PayScreen.Pay(Lease.TerminationFee, () =>
                    {
                        Lease.Terminate();
                        return false;
                    });
                }
                else
                {
                    Lease.Terminate();
                    InfoScreen.Display("Terminated", "Thanks for your business!", Lease.Clear ? Parent ?? LeaseScreen : this);
                }
                break;
            case InputAction.Confirm when LeaseScreen.Scroller is { SelectedIndex: 1 } && Lease.IncurredDebt > 0 && playerMoney > 0d:
                var whicheversMore = playerMoney > Lease.IncurredDebt ? Lease.IncurredDebt : playerMoney;
                PayScreen.Title = $"Pay ${whicheversMore:F2}?";
                PayScreen.AfterFail = this;
                PayScreen.AfterSuccess = Parent;
                PayScreen.Pay(whicheversMore, () =>
                {
                    Lease.UpdateDebt(-whicheversMore);
                    return false;
                });
                break;
            case InputAction.Up:
                LeaseScreen.Scroller?.Up();
                break;
            case InputAction.Down:
                LeaseScreen.Scroller?.Down();
                break;
            case InputAction.Cancel when Parent is not null:
                SwitchToScreen(Parent);
                break;
        }
    }
}