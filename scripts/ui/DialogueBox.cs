using Godot;
using Godot.Collections;
using ProjectREM.Characters;

namespace ProjectREM.UI;

// Autoloaded as "DialogueBox". Runs with ProcessMode = Always so it keeps working while the tree is paused.
public partial class DialogueBox : CanvasLayer
{
    // Text crawl: 28ms per character.
    [Export]
    public float CharsPerSecond { get; set; } = 1.0f / 0.028f;

    // JSON "speaker" key -> character data (display name + portraits).
    [Export]
    public Dictionary<string, CharacterData> Characters { get; set; } = new();

    private Control _root;
    private Label _speakerName;
    private Label _speakerTitle;
    private RichTextLabel _text;
    private TextureRect _portrait;
    private Control _continueShard;
    private Control _continueHint;
    private float _portraitRestX;
    private string _lastSpeaker;
    private Tween _portraitTween;

    private Array<Dictionary> _lines;
    private int _lineIndex;
    private Tween _typewriter;

    public override void _Ready()
    {
        _root = GetNode<Control>("%Root");
        _speakerName = GetNode<Label>("%SpeakerName");
        _text = GetNode<RichTextLabel>("%Text");
        _portrait = GetNode<TextureRect>("%Portrait");
        _speakerTitle = GetNode<Label>("%SpeakerTitle");
        _continueShard = GetNode<Control>("%ContinueShard");
        _continueHint = GetNode<Control>("%ContinueHint");
        _portraitRestX = _portrait.Position.X;

        GetNode<Label>("%Key").Text = KeyLabel("interact");
        StartIdleLoops();

        _root.Visible = false;
    }

    // Shows the key actually bound to the action, so the hint survives a rebind.
    private static string KeyLabel(StringName action)
    {
        foreach (InputEvent e in InputMap.ActionGetEvents(action))
        {
            if (e is InputEventKey key)
            {
                Key code =
                    key.PhysicalKeycode != Key.None
                        ? DisplayServer.KeyboardGetKeycodeFromPhysical(key.PhysicalKeycode)
                        : key.Keycode;
                return OS.GetKeycodeString(code).ToUpper();
            }
        }
        return "?";
    }

    // Continue shard bobs and spins (1.6s); the hint blinks between 1 and .35 opacity.
    private void StartIdleLoops()
    {
        float shardY = _continueShard.Position.Y;
        Tween shard = CreateTween()
            .SetLoops()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        shard.TweenProperty(_continueShard, "position:y", shardY - 8.0f, 0.8).From(shardY);
        shard.Parallel().TweenProperty(_continueShard, "rotation", Mathf.Pi, 0.8).From(0.0f);
        shard.TweenProperty(_continueShard, "position:y", shardY, 0.8);
        shard.Parallel().TweenProperty(_continueShard, "rotation", Mathf.Tau, 0.8);

        Tween blink = CreateTween()
            .SetLoops()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        blink.TweenProperty(_continueHint, "modulate:a", 0.35f, 0.8).From(1.0f);
        blink.TweenProperty(_continueHint, "modulate:a", 1.0f, 0.8);
    }

    public void StartDialogue(Json file, string conversationId)
    {
        if (file == null)
        {
            GD.PushError("DialogueBox: no dialogue file given.");
            return;
        }

        Dictionary conversations = file.Data.AsGodotDictionary();
        if (!conversations.TryGetValue(conversationId, out Variant conversation))
        {
            GD.PushError(
                $"DialogueBox: conversation '{conversationId}' not found in {file.ResourcePath}."
            );
            return;
        }

        _lines = conversation.AsGodotDictionary()["lines"].AsGodotArray<Dictionary>();
        if (_lines.Count == 0)
            return;

        _lineIndex = 0;
        _lastSpeaker = null;
        _root.Visible = true;
        CreateTween().TweenProperty(_root, "modulate:a", 1.0f, 0.2).From(0.0f);
        GetTree().Paused = true;
        ShowLine(_lines[_lineIndex]);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_root.Visible)
            return;

        bool clicked =
            @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };
        if (!clicked && !@event.IsActionPressed("interact") && !@event.IsActionPressed("ui_accept"))
            return;

        GetViewport().SetInputAsHandled();

        if (_typewriter != null && _typewriter.IsRunning())
        {
            _typewriter.Kill();
            _text.VisibleRatio = 1.0f;
            SetContinueVisible(true);
            return;
        }

        _lineIndex++;
        if (_lineIndex < _lines.Count)
        {
            ShowLine(_lines[_lineIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void ShowLine(Dictionary line)
    {
        string speaker = line["speaker"].AsString();
        string portrait = line.TryGetValue("portrait", out Variant p) ? p.AsString() : "";
        string text = line["text"].AsString();

        if (Characters.TryGetValue(speaker, out CharacterData character))
        {
            _speakerName.Text = character.DisplayName;
            _speakerTitle.Text = character.Title.ToUpper();
            _portrait.Texture = character.Portraits.TryGetValue(portrait, out Texture2D texture)
                ? texture
                : null;
            if (_portrait.Texture == null)
            {
                GD.PushWarning($"DialogueBox: '{speaker}' has no portrait '{portrait}'.");
            }
        }
        else
        {
            GD.PushWarning($"DialogueBox: unknown speaker '{speaker}'.");
            _speakerName.Text = speaker;
            _speakerTitle.Text = "";
            _portrait.Texture = null;
        }

        if (speaker != _lastSpeaker)
        {
            SlideInPortrait();
            _lastSpeaker = speaker;
        }

        // Text is BBCode, so lines can highlight words, e.g. [color=#2f7bff]finally open[/color].
        _text.Text = text;
        _text.VisibleRatio = 0.0f;
        SetContinueVisible(false);
        _typewriter?.Kill();
        _typewriter = CreateTween();
        _typewriter.TweenProperty(
            _text,
            "visible_ratio",
            1.0f,
            _text.GetTotalCharacterCount() / CharsPerSecond
        );
        _typewriter.TweenCallback(Callable.From(() => SetContinueVisible(true)));
    }

    // Dialogue portrait: slide in from 80px left + fade, 350ms cubic-bezier(.2,.9,.3,1).
    private void SlideInPortrait()
    {
        _portraitTween?.Kill();
        _portraitTween = CreateTween()
            .SetParallel()
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.Out);
        _portraitTween
            .TweenProperty(_portrait, "position:x", _portraitRestX, 0.35)
            .From(_portraitRestX - 80.0f);
        _portraitTween.TweenProperty(_portrait, "modulate:a", 1.0f, 0.35).From(0.0f);
    }

    private void SetContinueVisible(bool visible)
    {
        _continueShard.Visible = visible;
        _continueHint.Visible = visible;
    }

    private void EndDialogue()
    {
        _typewriter?.Kill();
        _root.Visible = false;
        _lines = null;
        GetTree().Paused = false;
    }
}
