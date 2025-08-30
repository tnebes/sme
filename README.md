# Stronghold Map Editor (SME)

A modern, cross-platform map editor tool for the game Stronghold Crusader Definitive Edition (2025). This tool allows
you to modify `.map` files to change their type or unlock them for editing.

This project is a rewrite of an original 2001 tool called `Gunnie`, reverse-engineered and implemented in modern C# with
a cross-platform UI.

## Purpose

The purpose of this tool is to allow users to 'unlock' Stronghold Crusader maps so they can be opened and edited in the Map editor of the game.

## Features

- **Unlock Maps:** Modify a map file to allow it to be opened and edited in the official Stronghold map editor.
- **Change Map Type:** Convert maps to be either "Invasion" or "Siege" type maps.
- **Cross-Platform:** Built with Eto.Forms to run on Windows, macOS, and Linux.
- **Logging:** Creates a detailed `sme.log` file for troubleshooting.

## How to Run (Easy Method)

The easiest way to use the application is to download a pre-built version from the releases page.

1. Go to the [Releases](https://github.com/tnebes/sme/releases) page of this repository.
2. Download the `.zip` file for your operating system (e.g., `sme-win-x64.zip`).
3. Extract the contents of the zip file to a new folder on your computer.
4. Double-click `StrongholdMapEditor.exe` (on Windows) or run the corresponding executable to start the application.

## How to Build from Source (for Developers)

If you want to build the project from the source code, follow these steps:

#### 1. Prerequisites

- Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or a newer version.
- Install [Git](https://git-scm.com/downloads/).

#### 2. Clone the Repository

```sh
git clone https://github.com/tnebes/sme.git
cd sme
```

#### 3. Build and Run

You can build and run the project from the command line:

```sh
# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

Alternatively, you can open the `SME.csproj` file in an IDE like Visual Studio, Rider, or VS Code and run it from there.

#### 4. Create a Standalone Package

To create a self-contained package that can be run on other computers without requiring the .NET SDK, use the `publish`
command. This is how the official releases are made.

```sh
# For Windows x64
dotnet publish -c Release -r win-x64 --self-contained true
```

The ready-to-use files will be in the `bin/Release/net9.0-windows/win-x64/publish/` directory.
