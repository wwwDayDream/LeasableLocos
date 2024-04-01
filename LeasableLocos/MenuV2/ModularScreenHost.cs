using System;
using DV.ServicePenalty.UI;
using UnityEngine;

namespace LeasableLocos.MenuV2;

public abstract class ModularScreenHost(IModularScreen? parent = null) : MonoBehaviour, IDisplayScreen, IModularScreen
{
    public IModularScreen? Parent { get; } = parent;
    public ModularScreenHost? Host { get; set; }
    public IModularScreen? Active { get; set; }

    public IModularScreen.ShowScreen? Show { get; protected set; }
    public IModularScreen.HideScreen? Hide { get; protected set; }
    public IModularScreen.ScreenInput? Input { get; protected set; }
    public event Action? Clear;


    public void Activate(IDisplayScreen previousScreen)
    {
        Show?.Invoke(Active);
        Active = this;
    }
    public void Disable()
    {
        Active?.Hide?.Invoke();

        Clear?.Invoke();
    }
    public void HandleInputAction(InputAction input)
    {
        Active?.Input?.Invoke(input);
    }

    public void SwitchToScreen(IModularScreen screen)
    {
        Active?.Hide?.Invoke(screen);

        Clear?.Invoke();

        screen.Show?.Invoke(Active);
        Active = screen;
    }
}

public static class Test
{
    public static void Testies()
    {
        var gameObject = new GameObject();
        var leaseScreen = gameObject.AddComponent<LeaseScreen>();
    }
}