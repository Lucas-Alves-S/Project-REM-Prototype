using Godot;
using System;

[GlobalClass]
public partial class CharacterData : Resource
{
	[Export] public string DisplayName { get; set; } = "";
	[Export] public Godot.Collections.Dictionary<string, Texture2D> Portraits { get; set; } = new();
}
