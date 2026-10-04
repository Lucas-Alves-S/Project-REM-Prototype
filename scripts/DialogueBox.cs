using Godot;
using Godot.Collections;

// Autoloaded as "DialogueBox". Runs with ProcessMode = Always so it keeps working while the tree is paused.
public partial class DialogueBox : CanvasLayer
{
	[Export] public float CharsPerSecond { get; set; } = 40.0f;

	// JSON "speaker" key -> character data (display name + portraits).
	[Export] public Dictionary<string, CharacterData> Characters { get; set; } = new();

	private Control _root;
	private Label _speakerName;
	private RichTextLabel _text;
	private TextureRect _portrait;

	private Array<Dictionary> _lines;
	private int _lineIndex;
	private Tween _typewriter;

	public override void _Ready()
	{
		_root = GetNode<Control>("%Root");
		_speakerName = GetNode<Label>("%SpeakerName");
		_text = GetNode<RichTextLabel>("%Text");
		_portrait = GetNode<TextureRect>("%Portrait");

		_root.Visible = false;
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
			GD.PushError($"DialogueBox: conversation '{conversationId}' not found in {file.ResourcePath}.");
			return;
		}

		_lines = conversation.AsGodotDictionary()["lines"].AsGodotArray<Dictionary>();
		if (_lines.Count == 0) return;

		_lineIndex = 0;
		_root.Visible = true;
		GetTree().Paused = true;
		ShowLine(_lines[_lineIndex]);
	}

	public override void _Input(InputEvent @event)
	{
		if (!_root.Visible) return;

		bool clicked = @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };
		if (!clicked && !@event.IsActionPressed("interact") && !@event.IsActionPressed("ui_accept")) return;

		GetViewport().SetInputAsHandled();

		if (_typewriter != null && _typewriter.IsRunning())
		{
			_typewriter.Kill();
			_text.VisibleRatio = 1.0f;
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
			_portrait.Texture = character.Portraits.TryGetValue(portrait, out Texture2D texture) ? texture : null;
			if (_portrait.Texture == null)
			{
				GD.PushWarning($"DialogueBox: '{speaker}' has no portrait '{portrait}'.");
			}
		}
		else
		{
			GD.PushWarning($"DialogueBox: unknown speaker '{speaker}'.");
			_speakerName.Text = speaker;
			_portrait.Texture = null;
		}

		_text.Text = text;
		_text.VisibleRatio = 0.0f;
		_typewriter?.Kill();
		_typewriter = CreateTween();
		_typewriter.TweenProperty(_text, "visible_ratio", 1.0f, text.Length / CharsPerSecond);
	}

	private void EndDialogue()
	{
		_typewriter?.Kill();
		_root.Visible = false;
		_lines = null;
		GetTree().Paused = false;
	}
}
