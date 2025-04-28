using System.Collections.Generic;
using System.Linq;
using DV;
using DV.ServicePenalty.UI;
using DV.ThingTypes;
using DV.Utils;
using LeasableLocos.SaveData;
using UnityEngine;

namespace LeasableLocos.MenuV2;

public class NewLease : ModularScreen
{
    public NewLease(IModularScreen? parent, ModularScreenHost? host, LeaseScreen leaseScreen) : base(parent, host)
    {
        LeaseScreen = leaseScreen;
        InfoScreen = new BasicInfoScreen(this, Host, LeaseScreen);
        PayScreen = new PayGenericScreen(this, Host, LeaseScreen);
        
        Show = OnShow;
        Hide = OnHide;
        Input = OnInput;
    }

    public ManageLease LeaseManager { get; set; }
    public LeaseScreen LeaseScreen { get; set; }
    public BasicInfoScreen InfoScreen { get; set; }
    public PayGenericScreen PayScreen { get; set; }
    public List<(string LocalizedName, List<TrainCarLivery> SpawnCars)> CachedLiveries { get; set; }
    
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Title!.text = "<u>Locally Leasable Engines</u>";
        LeaseScreen.AltSubtitle!.text = "$/DAY";
        LeaseScreen.Subtitle!.text = LeaseScreen.StationCompanyName;
        LeaseScreen.Subtitle.color = LeaseScreen.StationColor ?? LeaseScreen.Subtitle.color;

        CachedLiveries = LeaseScreen.GetLocalLiveries;
        LeaseScreen.Scroller?.SetOptions(CachedLiveries.Select<(string LocalizedName, List<TrainCarLivery> SpawnCars), 
            (LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter?)>(tuple => (
            option => option.text = tuple.LocalizedName, 
            option => option.text = $"${DayToDayLiveryCost(tuple.SpawnCars):F2}",
            () => true 
        )));
    }
    private void OnHide(IModularScreen? next)
    {
        
    }
    private void OnInput(InputAction action)
    {
        var selection = CachedLiveries[LeaseScreen.Scroller?.SelectedIndex ?? 0];
        
        switch (action)
        {
            case InputAction.Up:
                LeaseScreen.Scroller?.Up();
                break;
            case InputAction.Down:
                LeaseScreen.Scroller?.Down();
                break;
            case InputAction.Confirm:
                var notLicensedForAll = !LicensedForCars(selection.SpawnCars);
                var tooManyTermd = !SaveDataManager.NotTooManyTerminated(LeaseScreen.StationController.stationInfo.YardID);
                var someOverdue = !SaveDataManager.NoneOverdue(LeaseScreen.StationController.stationInfo.YardID);
                if (notLicensedForAll || tooManyTermd || someOverdue)
                {
                    InfoScreen.Display("Failed",
                        notLicensedForAll ? "You're not licensed to operate that locomotive. Please advance your career and then try again." :
                        tooManyTermd ? "You have too many Open Terminated Leases with our Local Office. Please pay off some of your debt." :
                        "You have Past Due leases with our Local Office. Please pay off some of your debt.", this);
                } else
                {
                    var dayToDay = DayToDayLiveryCost(selection.SpawnCars);
                    var amountToPay = Config.ApplicationPercentage / 100f * dayToDay;
                    PayScreen.Title = $"Pay ${amountToPay:F2} initial fee?";
                    PayScreen.ParagraphInfo = $"Plus a further ${dayToDay:F2} each day until termination? Termination may only occur with less than " +
                                              $"{Config.MaxTerminatedLeases} open terminated leases and the engine must be in good condition.";
                    PayScreen.Pay(Config.ApplicationPercentage / 100f * dayToDay, () =>
                    {
                        if (TrySpawnLivery(selection.SpawnCars, out var cars))
                        {
                            InfoScreen.Display("Success", $"Congratulations on your newly leased locomotive: {cars![0].ID}! It can be found in the local yard.",
                                this);
                            SavedLease.CreateLease(
                                LeaseScreen.StationController.stationInfo.YardID, cars.Select(car => car.ID).ToArray(), dayToDay);
                            return true;
                        }

                        InfoScreen.Display("Failed",
                            "Due to the rail yard being full we can't currently service your application. Sorry for the inconvenience.", this);
                        
                        LeaseScreen.FeesPayingScreen.cashReg.DepositedCash = amountToPay;
                        LeaseScreen.FeesPayingScreen.cashReg.ClearCurrentTransaction();
                        return true;
                    });
                }
                break;
            case InputAction.Cancel:
                SwitchToScreen(Parent ?? LeaseScreen);
                break;
            case InputAction.None:
            case InputAction.PrintInfo:
            default:
                break;
        }
    }

    private static bool LicensedForCars(IEnumerable<TrainCarLivery> cars) => 
        cars.All(c => LicenseManager.Instance.IsLicensedForCar(c));
    
    private bool TrySpawnLivery(List<TrainCarLivery> liveries, out List<TrainCar>? SpawnedCars)
    {
        foreach (var locoSpawner in LeaseScreen.StationLocoSpawners)
        {
            var carsOnTrack = locoSpawner.locoSpawnTrack.LogicTrack().GetCarsFullyOnTrack();
            if (carsOnTrack.Count != 0 && carsOnTrack.Any(car => CarTypes.IsLocomotive(car.carType)))
                continue;

            Physics.SyncTransforms();
            SpawnedCars = SingletonBehaviour<CarSpawner>.Instance.SpawnCarTypesOnTrack(liveries,
                null, locoSpawner.locoSpawnTrack, true, true);
            return SpawnedCars.Count > 0;
        }

        SpawnedCars = null;
        return false;
    }
    
    private double DayToDayLiveryCost(List<TrainCarLivery> liveries)
    {
        ResourceType[] Damages =
            [ ResourceType.Car_DMG, ResourceType.Wheels_DMG, ResourceType.ElectricalPowertrain_DMG, ResourceType.MechanicalPowertrain_DMG ];
        
        return Config.EnginePercentageOfFullUnitPrice * liveries.Aggregate(0d,
            (d, livery) => d + Damages.Aggregate(0d, (d1, type) =>
                d1 + ResourceTypes.GetFullUnitPriceOfResource(type, livery, gameParams: Globals.G.GameParams.ResourcesParams) * 100f)
        );
    }
}