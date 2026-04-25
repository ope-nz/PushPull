# PushPull for GitHub

A simple Windows desktop app for syncing a local folder with a GitHub repository, without needing Git installed.

Aimed at solo developers and non-technical users who want to use GitHub for file backup and sharing but find Git, SSH keys, and the command line intimidating. If you just want to keep a folder in sync with a repo and don't need branching, merging, or a full Git workflow, PushPull gets out of the way and lets you do that with a few clicks.

Good for: scripts and config files you want backed up offsite, sharing assets between machines, lightweight version history without learning Git, or anywhere a full Git client feels like overkill.

![PushPull Screenshot](screenshot.png)

## Features

- Connect to GitHub using a personal access token (no Git required)
- Register multiple projects, each linking a local folder to a GitHub repo
- Side-by-side file browser showing local and remote files (inspired by WinSCP)
- Files grouped by folder, with full relative paths shown for easy navigation
- Color-coded status: see at a glance which files are newer locally, newer on GitHub, or only exist on one side
- Push or pull all changed files with one click, or right-click to push/pull selected files only
- Push with a custom commit message using the **Push+** button
- Auto-selects a newly created project immediately after saving it
- Local menu: delete selected or all local files (sent to Recycle Bin), open project folder in Explorer
- Remote menu: delete selected or all remote files, open the repo on GitHub
- Click column headers to sort by name or date
- Flexible ignore patterns: wildcards, file extensions, and folder names
- Restores your last project, window position, and size on startup
- Single instance: launching a second copy raises the existing window
- Default repo owner and default ignore patterns configurable in Settings, applied automatically to new projects
- Command line support for opening a specific project or pushing without a UI

## Requirements

- Windows
- .NET Framework 4.0 or later (included with Windows 7 and above)
- A GitHub personal access token with `repo` scope

## Installation

No installer. Just download `PushPull.exe` and run it.

## Getting Started

### 1. Create a GitHub token

1. Go to GitHub > Settings > Developer settings > Personal access tokens > Tokens (classic)
2. Click **Generate new token**
3. Give it a name (e.g. `PushPull`) and select the `repo` scope
4. Copy the token

### 2. Enter your token and defaults

Open PushPull, go to **File > Settings** and configure:

| Setting | Description |
|---|---|
| GitHub Auth Token | Paste your personal access token; click **Test Connection** to verify |
| Default Repo Owner | Pre-filled into every new project (your GitHub username or organization) |
| Default Ignore Patterns | Applied automatically to every new project |

### 3. Add a project

Go to **File > New Project** and fill in:

| Field | Description |
|---|---|
| Name | A friendly label for this project |
| Local Folder | The folder on your PC to sync |
| Owner | Your GitHub username or organization |
| Repo | The repository name |
| Branch | The branch to sync against (e.g. `main`) |
| Ignore | Files/folders to skip (one per line) |

Click **Load Branches** to populate the branch list from GitHub, or just type the branch name manually.

### 4. Refresh and sync

Click **Refresh** to compare your local folder with the remote repo. Files are color-coded:

| Color | Meaning |
|---|---|
| Green | File exists locally but not on GitHub, or local copy has changed |
| Blue | File exists on GitHub but not locally, or remote copy has changed |
| White | Files are identical |

Use the toolbar buttons to sync:

| Button | Action |
|---|---|
| **Refresh** | Re-compare local and remote |
| **Push Selected** | Upload files selected in the left pane |
| **Push All** | Upload all local-only and locally-changed files |
| **Pull Selected** | Download files selected in the right pane |
| **Pull All** | Download all remote-only and remotely-changed files |

Right-click any file to get a **Push Folder** or **Pull Folder** option, which syncs all changed files in that folder only.

## Local Menu

| Option | Action |
|---|---|
| **Delete Selected Local Files...** | Sends selected files in the left pane to the Recycle Bin |
| **Delete All Local Files...** | Sends every local file in the project folder to the Recycle Bin |
| **Open in Explorer** | Opens the project folder in Windows Explorer |

Delete options prompt for confirmation before making any changes. Files are recoverable from the Recycle Bin.

## Remote Menu

| Option | Action |
|---|---|
| **Delete Selected Remote Files...** | Permanently deletes the files selected in the remote pane from GitHub |
| **Delete All Remote Files...** | Permanently deletes every file in the repository for the current project |
| **Open on GitHub** | Opens the repository in your default browser |

Delete options prompt for confirmation before making any changes.

## Ignore Patterns

In the project settings, list patterns to exclude from the comparison, one per line. Ignored files and folders are hidden from both panes.

| Pattern | Matches |
|---|---|
| `*.exe` | Any file with that extension |
| `bin` | Any folder named `bin`, at any depth |
| `bin/` | Same, explicit folder syntax |
| `.vs/` | The `.vs` folder |

Examples:

```
*.exe
*.dll
*.pdb
bin
obj
.vs
node_modules
```

## Command Line

Two modes are available from the command line:

**Open the app with a project pre-selected:**
```
PushPull.exe "MyProject"
```

**Push all changed files without opening the UI:**
```
PushPull.exe "MyProject" push
```

Replace `MyProject` with the name you gave the project in the app. In push mode, output goes to the calling terminal and the exit code is `0` on success or `1` if the project was not found or all uploads failed. Multiple push instances can run concurrently without interfering with the single-instance GUI check.

## Portable Mode

If a `config.json` file exists in the same folder as `PushPull.exe`, it is used instead of `%APPDATA%\PushPull\config.json`. This lets you carry the app and its settings together on a USB drive or in a synced folder.

To switch an existing install to portable mode, copy `%APPDATA%\PushPull\config.json` next to the exe.

## Config File

By default, settings and project definitions are stored at:

```
%APPDATA%\PushPull\config.json
```

## Building from Source

Requires the .NET Framework 4 C# compiler (`csc.exe`), which ships with Windows.

Run `_compile.bat` to build `PushPull.exe`.

## License

MIT
