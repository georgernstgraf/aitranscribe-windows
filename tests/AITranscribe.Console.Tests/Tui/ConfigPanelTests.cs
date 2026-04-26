using AITranscribe.Console.Tui;
using FluentAssertions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AITranscribe.Console.Tests.Tui;

public class ConfigPanelTests
{
    private readonly AITranscribeTui _tui;

    public ConfigPanelTests()
    {
        _tui = new AITranscribeTui();
    }

    [Fact]
    public void ConfigPanel_HasSourceModeOptionSelector()
    {
        _tui.SourceRadioGroup.Should().NotBeNull();
        _tui.SourceRadioGroup.Should().BeOfType<OptionSelector>();
    }

    [Fact]
    public void ConfigPanel_HasPreProcessModeOptionSelector()
    {
        _tui.PreprocessRadioGroup.Should().NotBeNull();
        _tui.PreprocessRadioGroup.Should().BeOfType<OptionSelector>();
    }

    [Fact]
    public void ConfigPanel_HasFilePathInput()
    {
        _tui.FilePathField.Should().NotBeNull();
        _tui.FilePathField.Should().BeOfType<TextField>();
    }
}
