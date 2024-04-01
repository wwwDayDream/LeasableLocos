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
    private bool InterStationList { get; set; }
    
    public void ShowLeases(bool isInterStationList = false)
    {
        InterStationList = isInterStationList;
        SwitchToScreen(this);
    }
    
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Title.text = "<u>Active Leases</u>";
        LeaseScreen.AltSubtitle.text = "DUE";
        LeaseScreen.Subtitle.text = !InterStationList ? LeaseScreen.StationCompanyName ?? "LOCAL-STATION" : "INTER-STATION";
        LeaseScreen.Subtitle.color = !InterStationList ? LeaseScreen.StationColor ?? LeaseScreen.Subtitle.color : Color.blue * 0.7f + Color.yellow * 0.2f;

        Leases = (InterStationList ? 
                SaveDataManager.AntiSavedLeases(LeaseScreen.StationController.stationInfo.YardID) : 
                SaveDataManager.LocalSavedLeases(LeaseScreen.StationController.stationInfo.YardID))
            .Select(lease => (lease, StationController.GetStationByYardID(lease.StationID)))
            .OrderBy(tup => tup.Item2.stationInfo.Name).ToList();
        
        LeaseScreen.Scroller?.SetOptions(Leases
            .Select<(SavedLease lease, StationController station), (LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter?)>(leaseInfo => (
                tmPRO =>
                {
                    tmPRO.text = (InterStationList ? 
                        $"<color=#{ColorUtility.ToHtmlStringRGB(leaseInfo.station.stationInfo.StationColor)}>{leaseInfo.station.stationInfo.YardID,3}</color> " 
                        : string.Empty) + leaseInfo.lease.AggregatedIDs;
                },
                tmPRO =>
                {
                    var targetColor = leaseInfo.lease.PastDue ? 
                        Color.red * 0.7f + Color.cyan * 0.2f : 
                        leaseInfo.lease.IsTerminated ? 
                            Color.green * 0.7f + Color.magenta * 0.2f : Color.white * 0.7f;
                    tmPRO.text = $"${leaseInfo.lease.IncurredDebt:F2} " + $"<color=#{ColorUtility.ToHtmlStringRGB(targetColor)}>" +
                                 (leaseInfo.lease.PastDue ? "P" : leaseInfo.lease.IsTerminated ? "T" : "O") + "</color>";
                }, null)));
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