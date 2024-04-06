using System;
using System.Collections.Generic;
using System.Linq;
using DV.JObjectExtstensions;
using DV.Utils;
using Newtonsoft.Json.Linq;

namespace LeasableLocos.SaveData;

public class SavedLease
{
    public SavedLease()
    {
        SaveDataManager.UpdateLeaseTimes += UpdateLeaseTimes;
    }

    private void UpdateLeaseTimes()
    {
        if (IsTerminated || HoursUntilIncurredDebt() > 0) return;

        SetIncurredTimeToNow();
        IncurredDebt += DailyRate;
        Plugin.Logger?.Log($"{AggregatedIDs} incurred ${DailyRate:F2} lease debt.");
    }

    public int HoursUntilIncurredDebt()
    {
        var now = TimeObject.sky.Cycle.DateTime;
        var hoursSinceLast = (now - LastIncurred).TotalHours;

        return 24 - (int)hoursSinceLast;
    }

    public void SetIncurredTimeToNow() => LastIncurredOLE = TimeObject.sky.Cycle.DateTime.ToOADate();

    public string StationID = string.Empty;
    public string[] LocosID = [ ];
    public bool IsTerminated = false;
    public double DailyRate;
    public double IncurredDebt;
    public double LastIncurredOLE;
    public DateTime LastIncurred => DateTime.FromOADate(LastIncurredOLE);
    public string AggregatedIDs => LocosID.Aggregate("", (s, s1) => s + (s.Length == 0 ? "" : " & ") + s1);

    public StationController OriginStation => StationController.GetStationByYardID(StationID);

    public List<TrainCar> LocosInLivery => CarSpawner.Instance.AllCars.Where(loco => LocosID.Contains(loco.ID)).ToList();

    public bool NotTooManyTerminated => SaveDataManager.NotTooManyTerminated(StationID);
    public bool LocosInGoodHealth => LocosInLivery
        .All(loco => loco.CarDamage.EffectiveHealthPercentage > Config.InGoodHealthPercentage);

    public bool LocosInOriginYard => LocosInLivery
        .All(loco => loco.logicCar.CurrentTrack.ID.yardId == StationID);

    public bool LocosBrakeOn => LocosInLivery
        .All(loco => !loco.brakeSystem.hasHandbrake || loco.brakeSystem.handbrakePosition >= 0.75f);


    public bool PastDue => IncurredDebt > DailyRate * Config.DaysUnpaidToOverdue;
    public double TerminationFee => PastDue ? IncurredDebt * Config.TerminatePrePayOffPercentage : 0d;
    public bool Clear => IncurredDebt <= 0d && IsTerminated;

    private TOD_Time TimeObject => TOD_Sky.Instance.Components.Time;

    public static SavedLease Load(JObject savedData)
    {
        return new SavedLease() {
            StationID = savedData.GetString(nameof(StationID)),
            LocosID = savedData.GetStringArray(nameof(LocosID)),
            DailyRate = savedData.GetDouble(nameof(DailyRate)) ?? 0d,
            IncurredDebt = savedData.GetDouble(nameof(IncurredDebt)) ?? 0d,
            LastIncurredOLE = savedData.GetDouble(nameof(LastIncurredOLE)) ?? 0d,
            IsTerminated = savedData.GetBool(nameof(IsTerminated)) ?? false,
        };
    }

    public JObject Save()
    {
        var jObject = new JObject();
        jObject.SetString(nameof(StationID), StationID);
        jObject.SetStringArray(nameof(LocosID), LocosID);
        jObject.SetDouble(nameof(DailyRate), DailyRate);
        jObject.SetDouble(nameof(IncurredDebt), IncurredDebt);
        jObject.SetDouble(nameof(LastIncurredOLE), LastIncurredOLE);
        jObject.SetBool(nameof(IsTerminated), IsTerminated);
        return jObject;
    }

    public static SavedLease CreateLease(string stationID, string[] locosID, double dailyRate)
    {
        var lease = new SavedLease() {
            DailyRate = dailyRate, StationID = stationID, LocosID = locosID
        };
        lease.SetIncurredTimeToNow();
        SaveDataManager.SavedLeases.Add(lease);
        return lease;
    }

    public void Terminate()
    {
        IsTerminated = true;
        Plugin.Logger.Log(LocosInLivery.Count.ToString());
        SingletonBehaviour<CarSpawner>.Instance.DeleteTrainCars(LocosInLivery, false);
        UpdateDebt();
    }
    public void UpdateDebt(double amount = 0d)
    {
        IncurredDebt += amount;

        if (Clear)
        {
            SaveDataManager.UpdateLeaseTimes -= UpdateLeaseTimes;
            SaveDataManager.SavedLeases.Remove(this);
        }
    }
}