# GFD - GitHub for Dummies: Implementation Plan

## Project Setup

- [x] Create `GFD` directory structure with `MainForm.cs`, `MainForm.Designer.cs`, `Program.cs`, source files, and `_compile.bat`
- [x] Set up `_compile.bat` using `csc.exe /target:winexe` referencing `System.Windows.Forms`, `System.Drawing`, `System.Net`, etc.
- [x] Port `github.cs` from `b4x_custom_actions` as the starting point for GitHub API logic (kept `JavaScriptSerializer` from `System.Web.Extensions`)
- [ ] Add `icon.ico` and wire it up in the compile script

## Data / Config Layer

- [x] Define a `GfdProject` model: local folder path, GitHub owner, repo name, branch, ignore patterns
- [x] Store projects in a JSON config file (`%APPDATA%\GFD\config.json`)
- [x] Store the GitHub auth token separately in config (not in project entries)
- [x] `ConfigManager.cs` with load/save methods

## GitHub API Layer (`GitHub.cs`)

- [x] Namespace `GFD`
- [x] `GetRepoTree`: uses `/git/trees/{sha}?recursive=1` for a flat file list with SHA and size in one request
- [x] `DownloadFile`: fetches raw content via `application/vnd.github.v3.raw`
- [x] `UploadFile`, `DeleteFile`, `CalcLocalSha` (Git blob SHA1 matching GitHub's format)
- [x] `GetBranches`
- [ ] Surface rate-limit response headers in the status bar

## Core Sync Logic (`SyncEngine.cs`)

- [x] `FileEntry` model: relative path, local modified time, local SHA1, remote SHA1, `SyncStatus` enum (`Same`, `LocalNewer`, `RemoteNewer`, `LocalOnly`, `RemoteOnly`)
- [x] `Compare`: builds local index (SHA1 per file), merges with remote tree, assigns status
- [x] Ignore patterns: wildcard (`*.exe`), directory prefix (`bin/`), and suffix matching
- [ ] `DeleteRemoteFile` exposed as a UI action (logic exists in `GitHub.cs`, not yet wired to a button)

## Main UI (`MainForm.cs`)

### Layout (WinSCP-style dual-pane)

- [x] `SplitContainer` as the main layout
- [x] Left pane: local `ListView` (Name, Size, Modified, Status)
- [x] Right pane: remote `ListView` (Name, Size, SHA, Status)
- [x] Status bar showing current operation
- [x] Toolbar: Refresh, Push Selected, Pull Selected, Push All, Pull All
- [x] Project `ComboBox` at the top

### Status colour coding

- [x] Same: default colour
- [x] Local newer / local only: green highlight
- [x] Remote newer / remote only: blue highlight

### Column sorting

- [x] Click column header to sort (toggle asc/desc)

### Project management

- [x] New Project dialog: local folder (Browse button), owner, repo, branch (with Load Branches), ignore patterns
- [x] Edit Project dialog
- [x] Remove Project menu item

### Settings dialog

- [x] GitHub auth token field (masked, show/hide toggle)
- [x] Test Connection button

## Menus

- [x] File: New Project, Edit Project, Remove Project, Exit
- [x] Tools: Settings
- [ ] Help: About

## Sync Workflow (UI-driven)

- [x] Refresh: fetches remote tree in background thread, populates both list views
- [x] Push Selected: pushes files selected in the local pane
- [x] Pull Selected: pulls files selected in the remote pane
- [x] Push All: pushes all local-newer and local-only entries
- [x] Pull All: pulls all remote-newer and remote-only entries
- [ ] Confirm before deleting remote files (delete not yet exposed in UI)
- [ ] Progress dialog for large batches (currently uses status bar text only)

## Still To Do

- [ ] Add `icon.ico`
- [ ] Delete remote file action (right-click context menu or toolbar button)
- [ ] Conflict status color (orange) for files that differ but direction is ambiguous
- [ ] Rate-limit feedback in the status bar
- [ ] Help > About dialog
- [ ] Progress dialog for batch operations

## Stretch Goals (post-MVP)

- [ ] Drag-and-drop files between panes
- [ ] Per-file diff viewer (text files)
- [ ] System tray icon with background sync polling
- [ ] Auto-ignore rules read from `.gitignore` in the local folder
