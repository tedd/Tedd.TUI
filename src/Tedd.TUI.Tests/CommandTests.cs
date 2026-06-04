using System;
using System.Windows.Input;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TestCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecuteResult { get; set; } = true;
    public bool ExecuteCalled { get; set; }
    public object? LastParameter { get; set; }

    public bool CanExecute(object? parameter) => CanExecuteResult;

    public void Execute(object? parameter)
    {
        ExecuteCalled = true;
        LastParameter = parameter;
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class CommandTests
{
    [Fact]
    public void ButtonBase_Command_ClickExecutesCommand()
    {
        var button = new Button();
        var cmd = new TestCommand();
        button.Command = cmd;
        button.CommandParameter = "test";

        button.OnMouseDown(new MouseEventArgs());
        button.OnMouseUp(new MouseEventArgs());

        Assert.True(cmd.ExecuteCalled);
        Assert.Equal("test", cmd.LastParameter);
    }

    [Fact]
    public void ButtonBase_Command_CannotExecute_DisablesButton()
    {
        var button = new Button();
        var cmd = new TestCommand { CanExecuteResult = false };
        button.Command = cmd;

        Assert.False(button.IsEnabled);
    }

    [Fact]
    public void ButtonBase_Command_RemovesDisablesWhenUnbound()
    {
        var button = new Button();
        var cmd = new TestCommand { CanExecuteResult = false };
        button.Command = cmd;

        Assert.False(button.IsEnabled);

        button.Command = null;

        // Button should revert back to Enabled = true (default)
        Assert.True(button.IsEnabled);
    }
}
