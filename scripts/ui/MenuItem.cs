using Godot;

namespace ProjectREM.UI;

// Skewed title/pause menu button. Selection follows focus, and the mouse moves focus,
// so keyboard and mouse never show two selected items at once.
public partial class MenuItem : Button
{
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

	// Mockup: translateX(-28px) scale(1.04) over 180ms cubic-bezier(.2,.9,.3,1.35).
	private const float Nudge = -28.0f;
	private const float SelectedScale = 1.04f;
	private const double SelectTime = 0.18;

	private Vector2 _restPosition;
	private Panel _shadowTrauma;
	private Panel _shadowMagic;
	private Tween _tween;

	public override void _Ready()
	{
		ThemeTypeVariation = "MenuItem";
		_restPosition = Position;

		// StyleBoxFlat only has one, soft shadow; the mockup wants two hard ones
		// (12px magenta down-right, 6px cyan up-left), so draw them as panels behind the button.
		_shadowMagic = AddShadow("shadow_magic", new Vector2(-6, -6));
		_shadowTrauma = AddShadow("shadow_trauma", new Vector2(12, 12));

		FocusEntered += () => SetSelected(true);
		FocusExited += () => SetSelected(false);
		MouseEntered += () =>
		{
			if (!Disabled) GrabFocus();
		};
		Resized += () => PivotOffset = Size / 2;
		PivotOffset = Size / 2;
	}

	private Panel AddShadow(StringName styleName, Vector2 offset)
	{
		var panel = new Panel
		{
			ShowBehindParent = true,
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		panel.AddThemeStyleboxOverride("panel", GetThemeStylebox(styleName, "MenuItem"));
		AddChild(panel, false, InternalMode.Front);
		panel.SetAnchorsPreset(LayoutPreset.FullRect);
		panel.OffsetLeft = offset.X;
		panel.OffsetTop = offset.Y;
		panel.OffsetRight = offset.X;
		panel.OffsetBottom = offset.Y;
		return panel;
	}

	private void SetSelected(bool selected)
	{
		_shadowTrauma.Visible = selected;
		_shadowMagic.Visible = selected;

		_tween?.Kill();
		_tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_tween.TweenProperty(this, "position:x", _restPosition.X + (selected ? Nudge : 0.0f), SelectTime);
		_tween.TweenProperty(this, "scale", Vector2.One * (selected ? SelectedScale : 1.0f), SelectTime);
	}
}
