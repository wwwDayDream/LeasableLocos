using System;
using System.Collections.Generic;
using System.Linq;
using DV.ServicePenalty.UI;
using LeasableLocos.SaveData;
using TMPro;
using UnityEngine;

namespace LeasableLocos.MenuV2;

public class DisplayLeases : ModularScreen
{
    public DisplayLeases(IModularScreen? parent, ModularScreenHost? host, LeaseScreen leaseScreen) : base(parent, host)
    {
        LeaseScreen = leaseScreen;
        LeaseManager = new ManageLease(this, Host, LeaseScreen);
        
        Show = OnShow;
        Hide = OnHide;
        Input = OnInput;
    }
    
    private LeaseScreen LeaseScreen { get; }
    private ManageLease LeaseManager { get; set; } = null!;
    private List<(SavedLease lease, StationController)> Leases { get; set; } = null!;
    
    public void ShowLeases()
    {
        SwitchToScreen(this);
    }
    
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Title!.text = "<u>Active Leases</u>";
        LeaseScreen.AltSubtitle!.text = "DUE";
        LeaseScreen.Subtitle!.text = $"[<color=#{ColorUtility.ToHtmlStringRGB(LeaseScreen.StationColor!.Value)}>" +
            $"{LeaseScreen.StationController.stationInfo.YardID,3}</color>] = {LeaseScreen.StationName}";

        Leases = [];
        var options = new List<(LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter?)>();
        foreach (var localSavedLease in SaveDataManager.LocalSavedLeases(LeaseScreen.StationController.stationInfo.YardID))
        {
            var stationController = localSavedLease.OriginStation;
            Leases.Add((localSavedLease, stationController));
            options.Add(LeaseToOption(stationController, localSavedLease));
        }
        foreach (var otherLease in SaveDataManager.AntiSavedLeases(LeaseScreen.StationController.stationInfo.YardID).OrderBy(l => l.OriginStation.stationInfo.Name))
        {
            var stationController = StationController.GetStationByYardID(otherLease.StationID);
            Leases.Add((otherLease, stationController));
            options.Add(LeaseToOption(stationController, otherLease));
        }
        
        LeaseScreen.Scroller?.SetOptions(options);

        return;
        (LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter?) LeaseToOption(StationController stationController, SavedLease lease)
        {
            return (option =>
                {
                    option.text =
                        $"[<color=#{ColorUtility.ToHtmlStringRGB(stationController.stationInfo.StationColor)}>" +
                        $"{stationController.stationInfo.YardID}</color>] {lease.AggregatedIDs}";
                },
                option =>
                {
                    var targetColor = lease.PastDue ? 
                        Color.red * 0.7f + Color.cyan * 0.2f : 
                        lease.IsTerminated ? 
                            Color.green * 0.7f + Color.magenta * 0.2f : Color.white * 0.7f;
                    option.text = $"${lease.IncurredDebt:F2} " + $"<color=#{ColorUtility.ToHtmlStringRGB(targetColor)}>" +
                                  (lease.PastDue ? "P" : lease.IsTerminated ? "T" : "O") + "</color>";
                }, null);
        }
    }
    private void OnHide(IModularScreen? next)
    {
    }
    private void OnInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.Up:
                LeaseScreen.Scroller?.Up();
                break;
            case InputAction.Down:
                LeaseScreen.Scroller?.Down();
                break;
            case InputAction.Confirm when LeaseScreen.Scroller?.SelectedIndex > -1 && Leases.Count > LeaseScreen.Scroller.SelectedIndex:
                LeaseManager.Manage(Leases[LeaseScreen.Scroller?.SelectedIndex ?? 0].lease);
                break;
            case InputAction.Cancel when Parent is not null:
                SwitchToScreen(Parent);
                break;
        }
    }
}