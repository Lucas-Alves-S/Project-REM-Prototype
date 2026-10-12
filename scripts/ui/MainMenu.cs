using Godot;

// Title screen (docs/ui-mockups/Main.dc.html). Looping ambience is all Tweens; per-node
// phase offsets come from each node's "delay" metadata, mirroring the mockup's negative
// CSS animation-delay values.
public partial class MainMenu : Control
{
	[Export] public PackedScene GameScene;

	private Label _description;
	private Control _wipe;
	private bool _leaving;

	public override void _Ready()
	{
		_description = GetNode<Label>("%Description");
		_wipe = GetNode<Control>("%Wipe");

		foreach (Node child in GetNode("%Menu").GetChildren())
		{
			if (child is MenuItem item)
			{
				item.FocusEntered += () => _description.Text = item.Description;
			}
		}

		var start = GetNode<MenuItem>("%StartButton");
		start.Pressed += OnStartPressed;
		GetNode<MenuItem>("%QuitButton").Pressed += () => GetTree().Quit();
		start.CallDeferred(Control.MethodName.GrabFocus);

		foreach (Node mote in GetNode("%Motes").GetChildren()) AnimateMote((Control)mote);
		foreach (Node smoke in GetNode("%Smoke").GetChildren()) AnimateSmoke((Control)smoke);
		foreach (Node ripple in GetNode("%Ripples").GetChildren()) AnimateRipple((Control)ripple);
	}

	private static double Delay(Node node) => node.GetMeta("delay", 0.0).AsDouble();

	// rise: 10s linear, up 460px, scale 1 -> .3, fade in over the first 12% then out.
	private void AnimateMote(Control mote)
	{
		const double period = 10.0;
		float startY = mote.Position.Y;
		mote.PivotOffset = mote.Size / 2;

		Tween tween = CreateTween().SetLoops();
		tween.TweenProperty(mote, "position:y", startY - 460.0f, period).From(startY);
		tween.Parallel().TweenProperty(mote, "scale", Vector2.One * 0.3f, period).From(Vector2.One);
		tween.Parallel().TweenProperty(mote, "modulate:a", 1.0f, period * 0.12).From(0.0f);
		tween.Parallel().TweenProperty(mote, "modulate:a", 0.0f, period * 0.88).SetDelay(period * 0.12);
		tween.CustomStep(Delay(mote));
	}

	// drift: 20s ease-in-out, ±50px.
	private void AnimateSmoke(Control smoke)
	{
		float centerX = smoke.Position.X;
		Tween tween = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(smoke, "position:x", centerX + 50.0f, 10.0).From(centerX - 50.0f);
		tween.TweenProperty(smoke, "position:x", centerX - 50.0f, 10.0);
		tween.CustomStep(Delay(smoke));
	}

	// ripple: 4s ease-out, scale .6 -> 1.6, opacity .8 -> 0.
	private void AnimateRipple(Control ripple)
	{
		ripple.PivotOffset = ripple.Size / 2;
		Tween tween = CreateTween().SetLoops().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(ripple, "scale", Vector2.One * 1.6f, 4.0).From(Vector2.One * 0.6f);
		tween.TweenProperty(ripple, "modulate:a", 0.0f, 4.0).From(0.8f);
		tween.CustomStep(Delay(ripple));
	}

	private void OnStartPressed()
	{
		if (_leaving) return;
		_leaving = true;
		GetViewport().GuiReleaseFocus();
		_wipe.MouseFilter = MouseFilterEnum.Stop;

		// Screen change: 3 skewed bars wipe left -> right, 400ms cubic-bezier(.7,0,.2,1).
		Tween tween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.InOut);
		int i = 0;
		foreach (Node child in _wipe.GetChildren())
		{
			var bar = (Control)child;
			bar.Visible = true;
			tween.TweenProperty(bar, "position:x", -300.0f, 0.4).From(-bar.Size.X - 300.0f).SetDelay(i * 0.06);
			i++;
		}
		tween.Chain().TweenCallback(Callable.From(() => GetTree().ChangeSceneToPacked(GameScene)));
	}
}
