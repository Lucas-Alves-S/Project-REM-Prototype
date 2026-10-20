using System;
using System.Globalization;
using Godot;
using ProjectREM.Characters;
using ProjectREM.Run;

namespace ProjectREM.UI;

public partial class DeathScreen : CanvasLayer
{
    [Export]
    public string[] WakeMessages { get; set; } =
        { "Good Morning", "You Woke Up", "Had a Nightmare?" };

    // A path rather than a PackedScene: main_menu.tscn already preloads the game scene, and a
    // PackedScene back to the menu would make the two scenes try to load each other.
    [Export(PropertyHint.File, "*.tscn")]
    public string MainMenuScene { get; set; } = "res://scenes/ui/main_menu.tscn";

    private const int LongMessageLength = 16;
    private const int MessageSize = 220;
    private const int LongMessageSize = 140;

    // Mockup: wake 1.2s cubic-bezier(.2,.8,.2,1) rising 24px; shafts pulse .55 <-> .85 over 7s.
    private const double WakeTime = 1.2;
    private const float WakeRise = 24.0f;
    private const double ShaftPeriod = 7.0;

    private Control _root;
    private Label _header;
    private Label _message;
    private GridContainer _statsGrid;
    private Control _wokenBy;
    private Label _wokenByName;
    private Control _kept;
    private Label _dreamDust;
    private Label _memories;
    private Control _menu;
    private MenuItem _newRunButton;

    private Player _player;
    private RunStats _stats;
    private bool _shown;
    private bool _canChoose;
    private bool _leaving;

    public override void _Ready()
    {
        _root = GetNode<Control>("%Root");
        _header = GetNode<Label>("%Header");
        _message = GetNode<Label>("%Message");
        _statsGrid = GetNode<GridContainer>("%StatsGrid");
        _wokenBy = GetNode<Control>("%WokenBy");
        _wokenByName = GetNode<Label>("%WokenByName");
        _kept = GetNode<Control>("%Kept");
        _dreamDust = GetNode<Label>("%DreamDust");
        _memories = GetNode<Label>("%Memories");
        _menu = GetNode<Control>("%Menu");
        _newRunButton = GetNode<MenuItem>("%NewRunButton");

        _newRunButton.Pressed += () => Leave(() => GetTree().ReloadCurrentScene());
        GetNode<MenuItem>("%MainMenuButton").Pressed += () =>
            Leave(() => GetTree().ChangeSceneToFile(MainMenuScene));
        GetNode<MenuItem>("%QuitButton").Pressed += () => GetTree().Quit();

        _root.Visible = false;

        _stats = GetTree().GetFirstNodeInGroup("run_stats") as RunStats;
        if (_stats == null)
            GD.PushError($"{Name}: no node in group 'run_stats'.");

        _player = GetTree().GetFirstNodeInGroup("player") as Player;
        if (_player == null)
        {
            GD.PushError($"{Name}: no node in group 'player'; the death screen will never show.");
            return;
        }
        _player.Health.Died += OnPlayerDied;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_player))
            _player.Health.Died -= OnPlayerDied;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_canChoose || !@event.IsActionPressed("new_run"))
            return;

        _newRunButton.EmitSignal(BaseButton.SignalName.Pressed);
        GetViewport().SetInputAsHandled();
    }

    private void OnPlayerDied()
    {
        // safety, HealthComponent cant say "Died" twice, but who knows
        if (_shown)
            return;
        _shown = true;

        GetTree().Paused = true;
        Fill();
        _root.Visible = true;
        PlayIntro();
    }

    private void Fill()
    {
        string message = WakeMessages[GD.RandRange(0, WakeMessages.Length - 1)];
        _message.Text = message;
        _message.AddThemeFontSizeOverride(
            "font_size",
            message.Length > LongMessageLength ? LongMessageSize : MessageSize
        );

        if (_stats == null)
            return;

        _header.Text = $"DREAM {_stats.DreamNumber():00} · ENDED ON FLOOR {_stats.FloorReached()}";

        // Add a stat by adding a RunStats method and a row here; the grid lays out 3 per line.
        (string Label, string Value)[] tiles =
        {
            ("TIME DREAMING", FormatTime(_stats.TimeDreaming())),
            ("FLOOR REACHED", _stats.FloorReached()),
            ("ROOMS CLEARED", FormatCount(_stats.RoomsCleared())),
            ("NIGHTMARES DEFEATED", FormatCount(_stats.NightmaresDefeated())),
            ("DAMAGE DEALT", FormatCount(_stats.DamageDealt())),
            ("RESONANCES", FormatCount(_stats.Resonances())),
        };
        foreach ((string label, string value) in tiles)
        {
            _statsGrid.AddChild(CreateTile(label, value));
        }

        _wokenByName.Text = _stats.WokenBy();
        _dreamDust.Text = $"{FormatCount(_stats.DreamDust())} DREAM DUST";
        int memories = _stats.Memories();
        _memories.Text = $"{FormatCount(memories)} {(memories == 1 ? "MEMORY" : "MEMORIES")}";
    }

    private static Control CreateTile(string label, string value)
    {
        var tile = new PanelContainer
        {
            ThemeTypeVariation = "WakeStatTile",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        column.AddChild(new Label { Text = label, ThemeTypeVariation = "WakeStatLabel" });
        column.AddChild(new Label { Text = value, ThemeTypeVariation = "WakeStatValue" });
        tile.AddChild(column);
        return tile;
    }

    // 24:13, or 1:24:13 once past an hour.
    private static string FormatTime(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours}:{time:mm\\:ss}" : $"{time:mm\\:ss}";
    }

    // 38,920 regardless of the OS locale (pt-BR would otherwise give 38.920).
    private static string FormatCount(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    // Mockup: blocks fade in on a stagger (header, stats .6s, woken by .9s, kept 1.1s, menu 1.3s)
    // while the message wakes: fades in and rises into place.
    private void PlayIntro()
    {
        Tween tween = CreateTween().SetParallel();
        FadeIn(tween, _header, 0.0, 0.6);
        FadeIn(tween, _statsGrid, 0.6, 0.6);
        FadeIn(tween, _wokenBy, 0.9, 0.6);
        FadeIn(tween, _kept, 1.1, 0.6);
        FadeIn(tween, _menu, 1.3, 0.5);

        float messageY = _message.Position.Y;
        _message.Modulate = Colors.Transparent;
        tween
            .TweenProperty(_message, "modulate", Colors.White, WakeTime)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(_message, "position:y", messageY, WakeTime)
            .From(messageY + WakeRise)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);

        // Focus waits for the menu, so a key still held from the fight can't pick an option.
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    _canChoose = true;
                    _newRunButton.GrabFocus();
                })
            );

        foreach (Node shaft in GetNode("%Shafts").GetChildren())
            AnimateShaft((Control)shaft);
    }

    private static void FadeIn(Tween tween, Control node, double delay, double duration)
    {
        // Hide now: a delayed tweener only applies its From value once its delay is over.
        node.Modulate = Colors.Transparent;
        tween
            .TweenProperty(node, "modulate", Colors.White, duration)
            .SetDelay(delay)
            .SetEase(Tween.EaseType.Out);
    }

    // Each shaft's own brightness is in its self_modulate; this pulses on top of it.
    // Phase offsets come from the "delay" metadata, like the title screen's ambience.
    private void AnimateShaft(Control shaft)
    {
        Tween tween = CreateTween()
            .SetLoops()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(shaft, "modulate:a", 0.85f, ShaftPeriod / 2).From(0.55f);
        tween.TweenProperty(shaft, "modulate:a", 0.55f, ShaftPeriod / 2);
        tween.CustomStep(shaft.GetMeta("delay", 0.0).AsDouble());
    }

    // Unpause first: the paused flag belongs to the SceneTree and would carry into the next scene.
    private void Leave(Action changeScene)
    {
        if (_leaving)
            return;
        _leaving = true;

        GetTree().Paused = false;
        changeScene();
    }
}
