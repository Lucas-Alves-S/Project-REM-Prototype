using Godot;
using System;

namespace ProjectREM.Characters;

[GlobalClass]
public partial class CharacterData : Resource
{
	[Export] public string DisplayName { get; set; } = "";
	// Optional epithet shown next to the name tag in dialogue, e.g. "THE DREAMER".
	[Export] public string Title { get; set; } = "";
	[Export] public Godot.Collections.Dictionary<string, Texture2D> Portraits { get; set; } = new();
}
