using TMPro;

namespace LeasableLocos.MenuV2;

public abstract class ModularScreen(IModularScreen? parent, ModularScreenHost? host) : IModularScreen
{
    public IModularScreen? Parent { get; } = parent;
    public ModularScreenHost? Host { get; } = host;
    
    public IModularScreen.ShowScreen? Show { get; protected set; }
    public IModularScreen.HideScreen? Hide { get; protected set; }
    public IModularScreen.ScreenInput? Input { get; protected set; }
    
    public void SwitchToScreen(IModularScreen screen) => Host?.SwitchToScreen(screen);
}