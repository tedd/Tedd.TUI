using Microsoft.AspNetCore.Components;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Platform.Blazor.Components;

public class TuiDialogBox : TuiComponentBase
{
    private DialogBox _dialogBox = new DialogBox();
    public override UIElement Element => _dialogBox;

    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public bool IsModal { get; set; } = true;
    [Parameter] public ConsoleColor BorderColor { get; set; } = ConsoleColor.White;
    [Parameter] public ConsoleColor TitleColor { get; set; } = ConsoleColor.Yellow;
    [Parameter] public ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
    [Parameter] public BoxStyle BoxStyle { get; set; } = BoxStyle.Double;

    protected override void ApplyProperties()
    {
        base.ApplyProperties();
        _dialogBox.Title = Title;
        _dialogBox.IsModal = IsModal;
        _dialogBox.BorderColor = BorderColor;
        _dialogBox.TitleColor = TitleColor;
        _dialogBox.BackgroundColor = BackgroundColor;
        _dialogBox.BoxStyle = BoxStyle;
    }

    public override void AddChild(UIElement child)
    {
        // DialogBox only supports one child (Content)
        if (_dialogBox.Content != null)
        {
             throw new InvalidOperationException("DialogBox can only have one child.");
        }
        _dialogBox.Content = child;
    }
}
