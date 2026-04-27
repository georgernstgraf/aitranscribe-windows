using AITranscribe.Core.Configuration;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attr = Terminal.Gui.Drawing.Attribute;

namespace AITranscribe.Console.Tui;

public class AITranscribeTui : Window
{
    public static readonly (string Id, string Label)[] FeedbackSteps =
    [
        ("compress", "Compressing Message"),
        ("transcribe", "Transcribing Raw Message"),
        ("post_process", "Post-Processing Message"),
        ("summary", "Creating Summary"),
    ];

    private const Command CmdTranslateGerman = (Command)100;
    private const Command CmdTranslateEnglish = (Command)101;
    private const Command CmdWriteIssue = (Command)102;
    private const Command CmdDelete = (Command)103;
    private const Command CmdAppend = (Command)104;
    private const Command CmdCopy = (Command)105;

    public TuiState CurrentState { get; private set; } = TuiState.Idle;

    public Action? OnToggleRecordingRequested { get; set; }
    public Action? OnAppendRecordingRequested { get; set; }
    public Action? OnResized { get; set; }
    public Func<string, string, System.Threading.Tasks.Task<long?>>? OnSaveTranscriptRequested { get; set; }
    public Action<string>? OnTranslateRequested { get; set; }
    public Action? OnWriteIssueRequested { get; set; }
    public Action? OnDeleteRequested { get; set; }
    public Action<AppConfig>? OnConfigChanged { get; set; }

    public TextView TranscriptView { get; }
    public ListView HistoryList { get; }
    public Label StatusLabel { get; }
    public Label FlashLabel { get; }
    public Label[] FeedbackStepLabels { get; }
    public OptionSelector SourceRadioGroup { get; }
    public OptionSelector PreprocessRadioGroup { get; }
    public TextField FilePathField { get; }
    public TextField SttModelField { get; }
    public TextField LlmModelField { get; }
    public Label HelpBar { get; }
    public Label HistorySubtitleLabel { get; }

    private readonly Dictionary<string, string> _feedbackState;
    private object? _clockTimeout;
    private int _lastResizeWidth;
    private int _lastResizeHeight;
    private View[] _focusableViews = [];
    private IApplication? _app;
    private readonly View _commandModeSink;

    public bool IsPaneFocusMode => _focusableViews.Any(v => v.HasFocus);

    public AITranscribeTui() : base()
    {
        Title = "AITranscribe";
        _feedbackState = FeedbackSteps.ToDictionary(s => s.Id, _ => "pending");

        var primaryColumn = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(57),
            Height = Dim.Fill(1),
            TabStop = TabBehavior.NoStop,
            CanFocus = true
        };

        var sidebarColumn = new View()
        {
            X = Pos.Right(primaryColumn),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            TabStop = TabBehavior.NoStop,
            CanFocus = true
        };

        var statusFrame = new FrameView()
        {
            Title = " Status ",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 4
        };

        StatusLabel = new Label()
        {
            Text = "Command Mode | Ready",
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = 1
        };

        FlashLabel = new Label()
        {
            Text = "Press Space to Start Recording",
            X = Pos.Right(StatusLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        statusFrame.Add(StatusLabel, FlashLabel);

        var feedbackFrame = new FrameView()
        {
            Title = " Feedback Log ",
            X = 0,
            Y = Pos.AnchorEnd(6),
            Width = Dim.Fill(),
            Height = 6
        };

        FeedbackStepLabels = new Label[FeedbackSteps.Length];
        for (int i = 0; i < FeedbackSteps.Length; i++)
        {
            FeedbackStepLabels[i] = new Label()
            {
                Text = $"[ ] {FeedbackSteps[i].Label}",
                X = 0,
                Y = i,
                Width = Dim.Fill(),
                Height = 1
            };
            feedbackFrame.Add(FeedbackStepLabels[i]);
        }

        var transcriptFrame = new FrameView()
        {
            Title = "Transcript (editable, Ctrl+S to save) ",
            X = 0,
            Y = Pos.Bottom(statusFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(6)
        };

        TranscriptView = new TextView()
        {
            Text = "No transcript yet.",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            WordWrap = true,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        transcriptFrame.Add(TranscriptView);
        primaryColumn.Add(statusFrame, transcriptFrame, feedbackFrame);

        var historyFrame = new FrameView()
        {
            Title = " Transcriptions ",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50)
        };

        HistorySubtitleLabel = new Label()
        {
            Text = "Stored: 0 | Arrows to preview",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        HistoryList = new ListView()
        {
            X = 0,
            Y = Pos.Bottom(HistorySubtitleLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        historyFrame.Add(HistorySubtitleLabel, HistoryList);

        var configFrame = new FrameView()
        {
            Title = " Recording Mode ",
            X = 0,
            Y = Pos.Bottom(historyFrame),
            Width = Dim.Fill(),
            Height = 10
        };

        SourceRadioGroup = new OptionSelector()
        {
            Labels = ["Microphone", "Filesystem file"],
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Value = 0,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var fileLabel = new Label()
        {
            Text = "File:",
            X = 0,
            Y = 2,
            Width = 5,
            Height = 1
        };

        FilePathField = new TextField()
        {
            Text = "",
            X = Pos.Right(fileLabel),
            Y = 2,
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        PreprocessRadioGroup = new OptionSelector()
        {
            Labels = ["Raw transcription", "Cleanup Text / Preserve Language", "Cleanup + Translate to English"],
            X = 0,
            Y = 4,
            Width = Dim.Fill(),
            Value = 2,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        configFrame.Add(SourceRadioGroup, fileLabel, FilePathField, PreprocessRadioGroup);

        var extraFrame = new FrameView()
        {
            Title = " Configuration ",
            X = 0,
            Y = Pos.Bottom(configFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var sttLabel = new Label()
        {
            Text = "STT-Model",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1
        };

        SttModelField = new TextField()
        {
            Text = "",
            X = Pos.Right(sttLabel),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var llmLabel = new Label()
        {
            Text = "LLM-Model",
            X = 0,
            Y = 1,
            Width = 10,
            Height = 1
        };

        LlmModelField = new TextField()
        {
            Text = "",
            X = Pos.Right(llmLabel),
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        extraFrame.Add(sttLabel, SttModelField, llmLabel, LlmModelField);
        sidebarColumn.Add(historyFrame, configFrame, extraFrame);

        HelpBar = new Label()
        {
            Text = "space Record/Stop  a Append  ^s Save  c Copy  d German  e English  w Issue  del Delete  esc Command  q Quit",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1
        };

        _commandModeSink = new View()
        {
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            CanFocus = true,
            TabStop = TabBehavior.NoStop
        };

        Add(primaryColumn, sidebarColumn, HelpBar, _commandModeSink);

        _focusableViews = [TranscriptView, HistoryList, SourceRadioGroup, FilePathField, PreprocessRadioGroup, SttModelField, LlmModelField];

        var navy = new Color(15, 23, 42);
        var cyan = Color.Cyan;
        var brightBlue = Color.BrightBlue;

        var scheme = new Scheme
        {
            Normal = new Attr(cyan, navy),
            Focus = new Attr(navy, brightBlue),
            HotNormal = new Attr(Color.BrightGreen, navy),
            HotFocus = new Attr(navy, Color.BrightGreen),
        };

        foreach (var view in _focusableViews)
        {
            view.SetScheme(scheme);
            view.HasFocusChanged += (_, _) => UpdateStatusDisplay();
            view.MouseHighlightStates = MouseState.In;
        }

        HelpBar.SetScheme(scheme);
        SetScheme(scheme);

        RegisterCommands();
    }

    public void WireConfigPersistence(AppConfig initialConfig)
    {
        SttModelField.ValueChanged += (_, _) => SaveConfig(initialConfig);
        LlmModelField.ValueChanged += (_, _) => SaveConfig(initialConfig);
        FilePathField.ValueChanged += (_, _) => SaveConfig(initialConfig);
        SourceRadioGroup.ValueChanged += (_, _) => SaveConfig(initialConfig);
        PreprocessRadioGroup.ValueChanged += (_, _) => SaveConfig(initialConfig);
    }

    private void SaveConfig(AppConfig baseConfig)
    {
        if (OnConfigChanged is null)
            return;

        var sttModel = SttModelField.Text.ToString() ?? baseConfig.Groq.SttModel;
        var llmModelOverride = LlmModelField.Text.ToString() ?? "";
        var filePath = FilePathField.Text.ToString() ?? "";
        var inputSource = (SourceRadioGroup.Value ?? 0) == 0 ? "microphone" : "file";
        var preProcessMode = (PreprocessRadioGroup.Value ?? 2) switch
        {
            0 => Core.Models.PreProcessMode.Raw,
            1 => Core.Models.PreProcessMode.Cleanup,
            _ => Core.Models.PreProcessMode.English,
        };

        var llmModel = string.IsNullOrWhiteSpace(llmModelOverride)
            ? baseConfig.Llm.Model
            : llmModelOverride;

        var newConfig = baseConfig with
        {
            Groq = baseConfig.Groq with { SttModel = sttModel },
            Llm = baseConfig.Llm with { Model = llmModel },
            InputSource = inputSource,
            PreProcessMode = preProcessMode,
            LastFilePath = filePath,
        };

        OnConfigChanged(newConfig);
    }

    public void StartClock(IApplication? app)
    {
        _app = app;
        if (app is null) return;
        _clockTimeout = app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            var timeStr = DateTime.Now.ToString("HH:mm:ss");
            var titleStr = "AITranscribe";
            var totalWidth = Frame.Width;
            if (totalWidth > titleStr.Length + timeStr.Length)
            {
                var pad = (totalWidth - titleStr.Length - timeStr.Length) / 2;
                Title = $"{new string(' ', pad)}{titleStr}{new string(' ', totalWidth - pad - titleStr.Length - timeStr.Length)}{timeStr}";
            }
            else
            {
                Title = $"{titleStr} {timeStr}";
            }

            if (Frame.Width != _lastResizeWidth || Frame.Height != _lastResizeHeight)
            {
                _lastResizeWidth = Frame.Width;
                _lastResizeHeight = Frame.Height;
                OnResized?.Invoke();
            }
            return true;
        });
    }

    public void StopClock()
    {
        if (_clockTimeout != null)
        {
            _app?.RemoveTimeout(_clockTimeout);
            _clockTimeout = null;
        }
    }

    private void RegisterCommands()
    {
        AddCommand(Command.Accept, () =>
        {
            ToggleRecording();
            return true;
        });

        AddCommand(Command.Save, () =>
        {
            SaveTranscript();
            return true;
        });

        AddCommand(Command.Quit, () =>
        {
            _app?.RequestStop();
            return true;
        });

        AddCommand(CmdAppend, () =>
        {
            AppendRecording();
            return true;
        });

        AddCommand(CmdCopy, () =>
        {
            CopyTranscript();
            return true;
        });

        AddCommand(CmdTranslateGerman, () =>
        {
            Translate("german");
            return true;
        });

        AddCommand(CmdTranslateEnglish, () =>
        {
            Translate("english");
            return true;
        });

        AddCommand(CmdWriteIssue, () =>
        {
            WriteIssueFile();
            return true;
        });

        AddCommand(CmdDelete, () =>
        {
            DeleteSelected();
            return true;
        });
    }

    protected override bool OnKeyDownNotHandled(Key key)
    {
        if (key == Key.Esc && IsPaneFocusMode)
        {
            _commandModeSink.SetFocus();
            return true;
        }

        if (key == Key.Tab)
        {
            if (IsPaneFocusMode)
            {
                var currentIdx = Array.FindIndex(_focusableViews, v => v.HasFocus);
                var nextIdx = currentIdx >= 0 ? (currentIdx + 1) % _focusableViews.Length : 0;
                _focusableViews[nextIdx].SetFocus();
            }
            else
            {
                _focusableViews[0].SetFocus();
            }
            return true;
        }

        if (key == Key.Tab.WithShift)
        {
            if (IsPaneFocusMode)
            {
                var currentIdx = Array.FindIndex(_focusableViews, v => v.HasFocus);
                var prevIdx = currentIdx > 0 ? currentIdx - 1 : _focusableViews.Length - 1;
                _focusableViews[prevIdx].SetFocus();
            }
            else
            {
                _focusableViews[0].SetFocus();
            }
            return true;
        }

        if (key == Key.S.WithCtrl)
        {
            _ = SaveTranscriptAsync();
            return true;
        }

        if (!IsPaneFocusMode)
        {
            if (key == Key.Space) { ToggleRecording(); return true; }
            if (key == Key.A) { AppendRecording(); return true; }
            if (key == Key.C) { CopyTranscript(); return true; }
            if (key == Key.D) { Translate("german"); return true; }
            if (key == Key.E) { Translate("english"); return true; }
            if (key == Key.W) { WriteIssueFile(); return true; }
            if (key == Key.Delete) { DeleteSelected(); return true; }
            if (key == Key.Q) { _app?.RequestStop(); return true; }
        }
        return base.OnKeyDownNotHandled(key);
    }

    public void ToggleRecording()
    {
        OnToggleRecordingRequested?.Invoke();
    }

    public void AppendRecording()
    {
        OnAppendRecordingRequested?.Invoke();
    }

    private void SaveTranscript()
    {
        _ = SaveTranscriptAsync();
    }

    public async System.Threading.Tasks.Task SaveTranscriptAsync()
    {
        var text = TranscriptView.Text.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text) || text == "No transcript yet.")
        {
            FlashLabel.Text = "Nothing to save.";
            return;
        }

        if (OnSaveTranscriptRequested != null)
        {
            try
            {
                var result = await OnSaveTranscriptRequested(text, "");
                if (result.HasValue)
                {
                    FlashLabel.Text = $"Transcript saved (ID: {result.Value}).";
                }
                else
                {
                    FlashLabel.Text = "Save failed.";
                }
            }
            catch (Exception ex)
            {
                FlashLabel.Text = $"Save failed: {ex.Message}";
            }
        }
        else
        {
            FlashLabel.Text = "Save not configured.";
        }
    }

    public void CopyTranscript()
    {
        try
        {
            var text = TranscriptView.Text.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                _app?.Clipboard?.SetClipboardData(text);
                FlashLabel.Text = "Copied to clipboard.";
            }
        }
        catch
        {
            FlashLabel.Text = "Clipboard not available.";
        }
    }

    public void Translate(string language)
    {
        OnTranslateRequested?.Invoke(language);
    }

    public void WriteIssueFile()
    {
        OnWriteIssueRequested?.Invoke();
    }

    public void DeleteSelected()
    {
        OnDeleteRequested?.Invoke();
    }

    public void SetState(TuiState state)
    {
        CurrentState = state;
        UpdateStatusDisplay();
    }

    public void UpdateStatusDisplay()
    {
        var mode = IsPaneFocusMode ? "Pane Focus Mode" : "Command Mode";
        var activityLabel = CurrentState switch
        {
            TuiState.Idle => "Ready",
            TuiState.Recording => "Recording: Press Space to Finish",
            TuiState.Processing => "Processing",
            _ => "Ready"
        };
        StatusLabel.Text = $"{mode} | {activityLabel}";

        FlashLabel.Text = IsPaneFocusMode
            ? "Escape to return to Command Mode"
            : CurrentState switch
            {
                TuiState.Idle => "Press Space to Start Recording",
                TuiState.Recording => "",
                TuiState.Processing => "Processing recording...",
                _ => ""
            };
    }

    public void SetFeedbackStep(string stepId, string status)
    {
        _feedbackState[stepId] = status;
        var index = Array.FindIndex(FeedbackSteps, s => s.Id == stepId);
        if (index >= 0 && index < FeedbackStepLabels.Length)
        {
            var prefix = status switch
            {
                "done" => "[x]",
                "active" => "[>]",
                "failed" => "[!]",
                _ => "[ ]"
            };
            FeedbackStepLabels[index].Text = $"  {prefix} {FeedbackSteps[index].Label}";
        }
    }

    public void ResetFeedbackSteps()
    {
        foreach (var step in FeedbackSteps)
        {
            _feedbackState[step.Id] = "pending";
        }

        for (int i = 0; i < FeedbackStepLabels.Length; i++)
        {
            FeedbackStepLabels[i].Text = $"[ ] {FeedbackSteps[i].Label}";
        }
    }

    public void UpdateHistorySubtitle(int count)
    {
        HistorySubtitleLabel.Text = $"Stored: {count} | Arrows to preview";
    }
}
