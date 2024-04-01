using DV.ServicePenalty.UI;

namespace LeasableLocos.MenuV2;

public class BasicInfoScreen : ModularScreen
{
    public BasicInfoScreen(IModularScreen? parent, ModularScreenHost? host, LeaseScreen leaseScreen) : base(parent, host)
    {
        LeaseScreen = leaseScreen;
        Show = OnShow;
        Input = OnInput;
    }

    private string Title { get; set; }
    private string Message { get; set; }
    private IModularScreen? After { get; set; }
    
    public void Display(string title, string message, IModularScreen modularScreen)
    {
        Title = title;
        Message = message;
        After = modularScreen;
        SwitchToScreen(this);
    }
    private void OnShow(IModularScreen? previous)
    {
        LeaseScreen.Paragraphs.ParagraphA.text = Title;
        LeaseScreen.Paragraphs.ParagraphB.text = Message;
    }
    private void OnInput(InputAction action)
    {
        SwitchToScreen(After);
        After = null;
    }
    public LeaseScreen LeaseScreen { get; }
}