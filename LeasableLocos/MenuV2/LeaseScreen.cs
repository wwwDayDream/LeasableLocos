using System;
using System.Collections.Generic;
using System.Linq;
using DV.Localization;
using DV.ServicePenalty.UI;
using DV.Teleporters;
using DV.ThingTypes;
using JetBrains.Annotations;
using LeasableLocos.SaveData;
using TMPro;
using UnityEngine;
// ReSharper disable MemberCanBePrivate.Global

namespace LeasableLocos.MenuV2;

public class LeaseScreen : ModularScreenHost
{
    public LeaseScreen()
    {
        LeasesScreen = new DisplayLeases(this, this, this);
        NewLeaseScreen = new NewLease(this, this, this);
        
        Show = OnShow;
        Hide = OnHide;
        Input = OnInput;
        Clear += OnClear;
    }

    public StationFastTravelDestination StationTeleporter { get; set; } = null!;
    public CareerManagerMainScreen MainScreen { get; private set; } = null!;
    public CareerManagerFeePayingScreen FeesPayingScreen { [UsedImplicitly] get; private set; } = null!;
    public (TextMeshPro lhs, TextMeshPro rhs)[] Lines { get; private set; } = null!;
    public (TextMeshPro ParagraphA, TextMeshPro ParagraphB) Paragraphs { get; private set; }
    public LinesScrollerScreen? Scroller { get; private set; }
    public DisplayLeases LeasesScreen { get; private set; } = null!;
    public NewLease NewLeaseScreen { get; set; }

    private (string lhsState, string rhsState)[] LineStates { get; set; } = null!;
    private (string stateA, string stateB) ParagraphStates { get; set; }

    private void Start()
    {
        MainScreen = transform.parent.gameObject.GetComponentInChildren<CareerManagerMainScreen>();
        FeesPayingScreen = transform.parent.gameObject.GetComponentInChildren<CareerManagerFeePayingScreen>();
        StationTeleporter =
            StationFastTravelDestination.GetClosestStationTeleporter(FindObjectsOfType<StationFastTravelDestination>().ToList(), gameObject.transform.position);

        SetupTMPros();
        
        Scroller = new LinesScrollerScreen(Lines.Skip(2).ToArray(), RegularTextColor, HighlightedTextColor);
    }

    public Color RegularTextColor => MainScreen.screenSwitcher.REGULAR_COLOR;
    public Color HighlightedTextColor => MainScreen.screenSwitcher.HIGHLIGHTED_COLOR;
    public StationLocoSpawner[] StationLocoSpawners => StationTeleporter.transform.parent.GetComponentsInChildren<StationLocoSpawner>();
    public StationController StationController => StationTeleporter.StationController;
    public string? StationName => StationController.stationInfo.Name;
    public Color? StationColor => StationController.stationInfo.StationColor;
    public string? StationCompanyName => StationName == null
        ? null
        : StationName + (StationName.Length > 10 ? " Loco " : " Locomotive ") + "Leaser";
    public List<(string LocalizedName, List<TrainCarLivery> spawnCars)> GetLocalLiveries {
        get 
        {
            var liveries = StationLocoSpawners
                .SelectMany(spawner => spawner.locoTypeGroupsToSpawn
                    .Select(group => group.liveries)).ToList();
            return liveries
                .Where((livery, i) => liveries.FindIndex(liv => liv[0].id == livery[0].id) == i)
                .Select(liv => (LocalizationAPI.L(liv[0].localizationKey, []), liv))
                .ToList();
        }
    }
    public TextMeshPro? Title => Lines.Length > 0 ? Lines[0].lhs : null; 
    public TextMeshPro? AltTitle => Lines.Length > 0 ? Lines[0].rhs : null;
    public TextMeshPro? Subtitle => Lines.Length > 1 ? Lines[1].lhs : null; 
    public TextMeshPro? AltSubtitle => Lines.Length > 1 ? Lines[1].rhs : null; 
    


    public static readonly string[] MainOptions = [
        "LEASES",
        "NEW LEASE"
    ];
    
    private void OnShow(IModularScreen? previous)
    {
        if (Title == null)
        {
            Exit();
            return;
        }
        Title.text = "<u>Loco Leasing</u>";
        
        Subtitle!.text = StationCompanyName ?? string.Empty;
        Subtitle.color = StationColor ?? Subtitle.color;
        
        Scroller?.SetOptions(MainOptions.Select<string, (LinesScrollerScreen.OptionParser?, LinesScrollerScreen.OptionParser?, LinesScrollerScreen.CanEnter?)>(s => (tmPro =>
        {
            tmPro.text = s;
        }, null, null)));

    }
    private void OnClear()
    {
        RestoreTMProStates();
    }
    private void OnHide(IModularScreen? next)
    {
    }
    private void OnInput(InputAction action)
    {
        switch (action)
        {
            case InputAction.Up:
                Scroller?.Up();
                break;
            case InputAction.Down:
                Scroller?.Down();
                break;
            case InputAction.Confirm when Scroller is { SelectedIndex: 0 }:
                LeasesScreen.ShowLeases();
                break;
            case InputAction.Confirm when Scroller is {SelectedIndex: 1}:
                SwitchToScreen(NewLeaseScreen);
                break;
            case InputAction.Cancel:
                Exit();
                break;
        }
    }
    private void Exit()
    {
        MainScreen.screenSwitcher.SetActiveDisplay(MainScreen);
    }
    private void SetupTMPros()
    {
        List<(int, TextMeshPro)> TMProLHSs = [];
        List<(int, TextMeshPro)> TMProRHSs = [];
        
        TextMeshPro? lowerParagraph = null;
        TextMeshPro? upperParagraph = null;
        var lastParagraphIdx = 69;
        foreach (var comp in MainScreen.title.transform.parent.GetComponentsInChildren<TextMeshPro>())
            if (comp.gameObject.name.StartsWith("line") && int.TryParse(comp.gameObject.name.Substring("line".Length), out var idx))
                TMProLHSs.Add((idx - 1, comp));
            else if (comp.gameObject.name.StartsWith("value") && int.TryParse(comp.gameObject.name.Substring("value".Length), out idx))
                TMProRHSs.Add((idx - 1, comp));
            else if (comp.gameObject.name.StartsWith("paragraph-line") && int.TryParse(comp.gameObject.name.Substring("paragraph-line".Length), out idx))
            {
                var oldLower = lowerParagraph;
                lowerParagraph = idx > lastParagraphIdx ? lowerParagraph : comp;
                upperParagraph = idx > lastParagraphIdx ? comp : oldLower;
                lastParagraphIdx = idx;
            }

        TMProLHSs = TMProLHSs.OrderBy((tuple => tuple.Item1)).ToList();
        TMProRHSs = TMProRHSs.OrderBy((tuple => tuple.Item1)).ToList();

        var lineCount = Math.Min(TMProLHSs.Count, TMProRHSs.Count);
        Plugin.Logger?.Log($"Initialized {lineCount} line(s) & " +
                           $"{(lowerParagraph != null && lowerParagraph ? 1 : 0) + (upperParagraph != null && upperParagraph ? 1 : 0)} paragraph(s).");

        Lines = new (TextMeshPro, TextMeshPro)[lineCount];

        for (var idx = 0; idx < lineCount; idx++)
        {
            Lines[idx] = (TMProLHSs[idx].Item2, TMProRHSs[idx].Item2);
            Lines[idx].lhs.text = string.Empty;
            Lines[idx].rhs.text = string.Empty;
        }

        if (lowerParagraph is null || upperParagraph is null)
        {
            Plugin.Logger?.Log("Failed to locate both paragraph elements during LeaseScreen setup!");
            Exit();
            return;
        }

        Paragraphs = (lowerParagraph, upperParagraph);
        Paragraphs.ParagraphA.text = string.Empty;
        Paragraphs.ParagraphB.text = string.Empty;
        
        SaveTMProStates();
    }
    private void SaveTMProStates()
    {
        LineStates = new (string, string)[Lines.Length];
        for (var idx = 0; idx < Lines.Length; idx++)
            LineStates[idx] = (JsonUtility.ToJson(Lines[idx].lhs), JsonUtility.ToJson(Lines[idx].rhs));

        ParagraphStates = (JsonUtility.ToJson(Paragraphs.ParagraphA), JsonUtility.ToJson(Paragraphs.ParagraphB));
    }
    private void RestoreTMProStates()
    {
        for (var idx = 0; idx < LineStates.Length; idx++)
        {
            JsonUtility.FromJsonOverwrite(LineStates[idx].lhsState, Lines[idx].lhs);
            JsonUtility.FromJsonOverwrite(LineStates[idx].rhsState, Lines[idx].rhs);
            Lines[idx].lhs.SetAllDirty();
            Lines[idx].rhs.SetAllDirty();
        }
        JsonUtility.FromJsonOverwrite(ParagraphStates.stateA, Paragraphs.ParagraphA);
        JsonUtility.FromJsonOverwrite(ParagraphStates.stateB, Paragraphs.ParagraphB);
        Paragraphs.ParagraphA.SetAllDirty();
        Paragraphs.ParagraphB.SetAllDirty();
    }
}