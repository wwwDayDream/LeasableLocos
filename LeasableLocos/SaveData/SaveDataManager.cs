using System;
using System.Collections.Generic;
using System.Linq;
using DV.JObjectExtstensions;
using Newtonsoft.Json.Linq;

namespace LeasableLocos.SaveData;

public static class SaveDataManager
{
    private const string LeasesKey = "LeasableLocos.ActiveLeases";

    public static event Action UpdateLeaseTimes;

    public static List<SavedLease> SavedLeases { get; private set; } = [ ];

    public static List<SavedLease> LocalSavedLeases(string stationID) => SaveDataManager.SavedLeases
        .Where(savedLease => savedLease.StationID == stationID).ToList();
    public static List<SavedLease> AntiSavedLeases(string stationID) => SaveDataManager.SavedLeases
        .Where(savedLease => savedLease.StationID != stationID).ToList();

    public static bool NotTooManyTerminated(string stationID) =>
        LocalSavedLeases(stationID).Count(lease => lease.IsTerminated && !lease.Clear) < Config.MaxTerminatedLeases;
    
    public static bool NoneOverdue(string stationID) =>
        LocalSavedLeases(stationID).Count(lease => lease.PastDue) == 0;

    public static void SaveToJObject(SaveGameData data)
    {
        data.SetJObjectArray(LeasesKey, SavedLeases.Select(lease => lease.Save()).ToArray());
    }

    public static void LoadFromSave(SaveGameData savedData)
    {
        var potentialLeases = savedData.GetJObjectArray(LeasesKey);
        SavedLeases = potentialLeases != null ? potentialLeases.Select(SavedLease.Load).ToList() : [ ];
    }

    public static void InvokeUpdateLeaseTimes()
    {
        UpdateLeaseTimes?.Invoke();
    }
}