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

    public TuiState CurrentState { get; private set; } = TuiState.Idle;

    public Action? OnToggleRecordingRequested { get; set; }
    public Action? OnAppendRecordingRequested { get; set; }
    public Action? OnResized { get; set; }
    public Func<string, string, System.Threading.Tasks.Task<long?>>? OnSaveTranscriptRequested { get; set; }

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

    private readonly Dictionary<string, string> _feedbackState;
    private object? _clockTimeout;
    private int _lastResizeWidth;
    private int _lastResizeHeight;
    private View[] _focusableViews = [];
    private IApplication? _app;

    public bool IsPaneFocusMode => _focusableViews.Any(v => v.HasFocus);

    public AITranscribeTui() : base()
    {
        Title = "AITranscribe";
        _feedbackState = FeedbackSteps.ToDictionary(s => s.Id, _ => "pending");

        CanFocus = true;

        var primaryColumn = new View()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(57),
            Height = Dim.Fill()
        };

        var sidebarColumn = new View()
        {
            X = Pos.Right(primaryColumn),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
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
                Text = $"  {FeedbackSteps[i].Label}: pending",
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

        HistoryList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        historyFrame.Add(HistoryList);

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
            Labels = ["Raw transcription", "Cleanup / Preserve", "Cleanup + English"],
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
            Text = "STT:",
            X = 0,
            Y = 0,
            Width = 5,
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
            Text = "LLM:",
            X = 0,
            Y = 1,
            Width = 5,
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
        Add(primaryColumn, sidebarColumn);

        _focusableViews = [TranscriptView, HistoryList, SourceRadioGroup, FilePathField, PreprocessRadioGroup, SttModelField, LlmModelField];

        var scheme = new Scheme
        {
            Normal = new Attr(Color.White, Color.Black),
            Focus = new Attr(Color.Black, Color.BrightCyan),
            HotNormal = new Attr(Color.BrightGreen, Color.Black),
            HotFocus = new Attr(Color.Black, Color.BrightGreen),
        };

        foreach (var view in _focusableViews)
        {
            view.SetScheme(scheme);
            view.HasFocusChanged += (_, _) => UpdateStatusDisplay();
        }

        SetupKeyBindings();
    }

    public void StartClock(IApplication? app)
    {
        _app = app;
        if (app is null) return;
        _clockTimeout = app.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            Title = $"AITranscribe  {DateTime.Now:HH:mm:ss}";
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

    private void SetupKeyBindings()
    {
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, Key e)
    {
        if (e == Key.Tab)
        {
            FocusNext();
            e.Handled = true;
        }
        else if (e == Key.Esc)
        {
            foreach (var v in _focusableViews)
                v.HasFocus = false;
            UpdateStatusDisplay();
            e.Handled = true;
        }
        else if (IsPaneFocusMode)
        {
        }
        else if (e == Key.Space)
        {
            ToggleRecording();
            e.Handled = true;
        }
        else if (e == (Key.A))
        {
            AppendRecording();
            e.Handled = true;
        }
        else if (e == Key.S)
        {
            SaveTranscript();
            e.Handled = true;
        }
        else if (e == Key.C)
        {
            CopyTranscript();
            e.Handled = true;
        }
        else if (e == Key.Q)
        {
            _app?.RequestStop();
            e.Handled = true;
        }
    }

    private void FocusNext()
    {
        if (_focusableViews.Length == 0) return;
        var current = _focusableViews.FirstOrDefault(v => v.HasFocus);
        var idx = current is null ? -1 : Array.IndexOf(_focusableViews, current);
        idx = (idx + 1) % _focusableViews.Length;
        _focusableViews[idx].SetFocus();
    }

    public void ToggleRecording()
    {
        OnToggleRecordingRequested?.Invoke();
    }

    public void AppendRecording()
    {
        OnAppendRecordingRequested?.Invoke();
    }

    public void SaveTranscript()
    {
        var text = TranscriptView.Text.ToString() ?? "";
        if (OnSaveTranscriptRequested != null)
        {
            var task = OnSaveTranscriptRequested(text, "");
            System.Threading.Tasks.Task.Run(async () =>
            {
                try { await task; } catch { }
            });
            FlashLabel.Text = "Transcript saved.";
        }
        else
        {
            FlashLabel.Text = "Transcript saved.";
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
            FeedbackStepLabels[index].Text = $"  {FeedbackSteps[index].Label}: {status}";
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
            FeedbackStepLabels[i].Text = $"  {FeedbackSteps[i].Label}: pending";
        }
    }
}
