using Godot;

namespace ProjectREM.UI;

// Skewed title/pause menu button. Selection follows focus, and the mouse moves focus,
// so keyboard and mouse never show two selected items at once.
public partial class MenuItem : Button
{
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = "";

    // How far left the item slides when selected. Title menu: translateX(-28px).
    [Export]
    public float Nudge { get; set; } = -28.0f;

    // Mockup: scale(1.04) over 180ms cubic-bezier(.2,.9,.3,1.35).
    private const float SelectedScale = 1.04f;
    private const double SelectTime = 0.18;

    private Vector2 _restPosition;
    private Panel _shadowBlue;
    private Panel _shadowIce;
    private Tween _tween;

    public override void _Ready()
    {
        // Scenes can pick another look (the death screen uses "WakeItem").
        if (ThemeTypeVariation == "")
            ThemeTypeVariation = "MenuItem";
        _restPosition = Position;

        // StyleBoxFlat only has one, soft shadow; the mockup wants two hard ones
        // (12px REM blue down-right, 6px ice up-left), so draw them as panels behind the button.
        // A variation without one of these styles just gets no panel for it.
        _shadowIce = AddShadow("shadow_ice", new Vector2(-6, -6));
        _shadowBlue = AddShadow("shadow_blue", new Vector2(12, 12));

        FocusEntered += () => SetSelected(true);
        FocusExited += () => SetSelected(false);
        MouseEntered += () =>
        {
            if (!Disabled)
                GrabFocus();
        };
        Resized += () => PivotOffset = Size / 2;
        PivotOffset = Size / 2;
    }

    private Panel AddShadow(StringName styleName, Vector2 offset)
    {
        if (!HasThemeStylebox(styleName))
            return null;

        var panel = new Panel
        {
            ShowBehindParent = true,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        panel.AddThemeStyleboxOverride("panel", GetThemeStylebox(styleName));
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
        if (_shadowBlue != null)
            _shadowBlue.Visible = selected;
        if (_shadowIce != null)
            _shadowIce.Visible = selected;

        _tween?.Kill();
        _tween = CreateTween()
            .SetParallel()
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(
            this,
            "position:x",
            _restPosition.X + (selected ? Nudge : 0.0f),
            SelectTime
        );
        _tween.TweenProperty(
            this,
            "scale",
            Vector2.One * (selected ? SelectedScale : 1.0f),
            SelectTime
        );
    }
}
