# ProjectREM

A 2.5D roguelike built as a TCC (undergraduate thesis) project. The world and physics are fully 3D, while characters are billboarded 2D sprites viewed through an orthographic camera.

## Tech stack

| | |
| --- | --- |
| Engine | [Godot 4.7](https://godotengine.org/) — **.NET / C# edition** |
| Language | C# |
| Runtime | .NET 10 (`net10.0`) |
| Physics | Jolt |
| Formatter | [CSharpier](https://csharpier.com/) |

> **Important:** you need the **.NET version** of Godot (named "Godot Engine - .NET" on the download page). The standard build only supports GDScript and will not open this project's scripts.

## Requirements

- [Godot 4.7 .NET](https://godotengine.org/download)
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- A C# code editor (VS Code, JetBrains Rider or Visual Studio)

## Getting started

```bash
git clone <repo-url>
cd project-rem
dotnet tool restore   # installs CSharpier locally (see below)
dotnet build          # optional, Godot also builds on first run
```

Then open Godot, click **Import**, and select the `project.godot` file.

## Setting up the code editor in Godot

Godot's built-in script editor does not support C#, so you need to point it to an external editor. This way, double-clicking a script in Godot opens it in your editor.

1. In Godot, open **Editor → Editor Settings**.
2. Go to **Dotnet → Editor**.
3. Set **External Editor** to the editor you use (Visual Studio Code, JetBrains Rider, Visual Studio, etc.).
4. If Godot can't find it, set **Custom Exec Path** to the editor's executable.

### Recommended extensions

**VS Code**
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (or the [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) extension) — IntelliSense and debugging
- [godot-tools](https://marketplace.visualstudio.com/items?itemName=geequlim.godot-tools) — scene/resource file support
- [CSharpier](https://marketplace.visualstudio.com/items?itemName=csharpier.csharpier-vscode) — formatter (see below)

**Rider**
- Godot support is built in. Install the **CSharpier** plugin from the marketplace.

## Code formatting (CSharpier)

We use [CSharpier](https://csharpier.com/) as the C# code formatter, so the code stays consistent without debates over style. It's already registered as a local .NET tool in `dotnet-tools.json`, so you just need to restore it:

```bash
dotnet tool restore
```

Format the whole project:

```bash
dotnet csharpier format .
```

Check formatting without changing files (useful before committing):

```bash
dotnet csharpier check .
```

### Format on save

It's highly recommended to format automatically on save:

- **VS Code:** install the CSharpier extension, then add to your `settings.json`:

  ```json
  "[csharp]": {
    "editor.defaultFormatter": "csharpier.csharpier-vscode",
    "editor.formatOnSave": true
  }
  ```

- **Rider:** go to **Settings → Tools → CSharpier** and enable **Run on Save**.

## Project structure

```
assets/    art, audio, themes and other imported resources
data/      game data (resources, definitions)
docs/      diagrams and UI mockups
scenes/    Godot scenes (.tscn)
scripts/   C# source code, grouped by feature
```

## About the project

### Description

ProjectREM is a single-player roguelike with a 2.5D perspective, set in the dreams of a child. The player controls a dreamer who goes through nightmares, fights enemies in real time and recovers lost memories. Each run takes place in procedurally generated levels, and dying sends the player back to the start, while some progress carries over between runs.

The focus of the project is not the game as a commercial product, but the software architecture behind it. Art, soundtrack and the full script are only developed as far as needed to validate that architecture.

### Problem

Roguelikes depend on two systems that pull in opposite directions: a procedural generator that builds a different map on every run, and a narrative that has to stay coherent across many runs. When the level generator and the story logic are tightly coupled (for example, when the same code that builds a room also decides which dialogue to show), adding a new room or story event means changing several parts of the system, which makes the game hard to maintain and extend.

Research on procedural generation and computational narrative usually studies these algorithms in isolation, and commercial games rarely document how they organize their code. This project addresses the question:

> How can the software architecture of a roguelike be structured so that procedural level generation and narrative progression stay decoupled and configurable, supporting variety between runs and ease of maintenance?

### Objectives

**General objective**

Propose, implement and evaluate a software architecture for roguelike games, applied to a prototype built in Godot with C#, in which procedural level generation and narrative progression work as decoupled, data-driven modules, and measure its effect on the variety of generated content and on how easy the project is to extend.

**Specific objectives**

- Review software engineering and procedural content generation literature for design patterns that help decouple level generation from narrative (e.g. Observer, Event Queue, Component).
- Model the proposed architecture, documenting its modules, the interfaces between them and the format of the data files that drive the level generator and narrative events.
- Implement a small-scope playable prototype in Godot with C#, with a procedural room generator and a narrative event system connected through the proposed architecture.
- Measure the variety of generated content using expressive range analysis (Smith & Whitehead, 2010), generating a sample of levels from different random seeds and computing metrics such as number of rooms, main path length and enemy distribution.
- Evaluate the architecture's extensibility through modification scenarios (e.g. adding a new room type or a new narrative event), recording how many files and classes change in each one, using the maintainability characteristic of ISO/IEC 25010 as a reference.

### Team

- João Gabriel Vireira
- Lucas Alves Souza Lima