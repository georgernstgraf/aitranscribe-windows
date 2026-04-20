using AITranscribe.Console.Tui;
using FluentAssertions;
using Terminal.Gui;

namespace AITranscribe.Console.Tests.Tui;

public class ConfigPanelTests
{
    private readonly AITranscribeTui _tui;

    public ConfigPanelTests()
    {
        _tui = new AITranscribeTui();
    }

    [Fact]
    public void ConfigPanel_HasSourceModeRadioGroup()
    {
        _tui.SourceRadioGroup.Should().NotBeNull();
        _tui.SourceRadioGroup.Should().BeOfType<RadioGroup>();
    }

    [Fact]
    public void ConfigPanel_HasPreProcessModeRadioGroup()
    {
        _tui.PreprocessRadioGroup.Should().NotBeNull();
        _tui.PreprocessRadioGroup.Should().BeOfType<RadioGroup>();
    }

    [Fact]
    public void ConfigPanel_HasFilePathInput()
    {
        _tui.FilePathField.Should().NotBeNull();
        _tui.FilePathField.Should().BeOfType<TextField>();
    }
}
