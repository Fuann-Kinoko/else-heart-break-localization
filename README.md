# Else Heartbreak Localization

[中文 README](README_CN.md)

A BepInEx plugin that provides custom language translation support for **Else Heartbreak**, including menu/UI text, tooltips, action descriptions, notifications, and adjustable speech bubble width for CJK characters.

## Features

- **Toggleable Bilingual Mode**: Toggle between showing only translation or both English and translation via hotkey (Default F11).
- **Menu and UI Translation**: Supports translation for tooltips, menu items, and notifications.
- **Custom Language Button**: Automatically adds a language selection button for your custom language in the Main Menu and Settings.
- **Speech Bubble Width**: Configurable multiplier for adjusting bubble width, useful for CJK characters.

## Installation

### Method 1: Using Release Package (Recommended)
1. Download the latest Release zip package.
2. Extract the contents **directly into the game root directory** (`ElseHeartbreak/`), overwriting any existing files.
3. Start the game.

### Method 2: Manual Installation
1. Install [BepInEx 5.x](https://github.com/BepInEx/BepInEx) (if not already included).
2. Copy the compiled `ElseHeartbreakLocalization.dll` to `ElseHeartbreak_Data/BepInEx/plugins/`.
3. Copy `assets/localization.ini` to the game root directory.
4. Copy the folders under `resources/` to `ElseHeartbreak_Data/InitData/`.

## Directory Structure

After installation, your game directory should look like this:

```
ElseHeartbreak/
├── ElseHeartbreak.exe
├── localization.ini                # Language configuration file
├── doorstop_config.ini             # Doorstop config
├── winhttp.dll                     # Doorstop loader
└── ElseHeartbreak_Data/
    ├── BepInEx/
    │   ├── core/
    │   └── plugins/
    │       └── ElseHeartbreakLocalization.dll
    └── InitData/
        ├── Translations/{Language}/         # Game dialogue translations
        │   └── ...
        └── MenuTranslations/{Language}/     # Menu/UI translations
            ├── tooltips.{idn}.mtf
            └── ...
```

> **Note**: Menu translations MUST be placed in `InitData/MenuTranslations/`, NOT `InitData/Translations/`. Otherwise, the game's native Translator will try to load them and cause errors.

## Translator Guide

Translations are divided into two parts:
1. **Dialogue Translations**: Located in `Translations/{Language}/`
2. **Menu/UI Translations**: Located in `MenuTranslations/{Language}/`

---

### 1. Dialogue Translations (Translations Folder)

This is the **main part** of the translation, containing dialogue for all characters in the game.

**Location**: `ElseHeartbreak_Data/InitData/Translations/{Language}/`

**Naming Format**: `{Character}_{Scene/Event}.{idn}.mtf`

**Format**:
```
"Original Text" => "Translated Text"
```

> **Tip**: The original game dialogue is in Swedish. You can refer to the English translations in `Translations/English/` to understand the meaning.

**Dialogue File List** (approx. 358 files):

Main Characters: `Pixie`, `Felix`, `Hank`, `Petra`, `Frank`, `Monad`, `Yulian`, `Fib`, `Ivan`, `Lars`, etc.

Scenes/Events: `Arrival`, `FirstDay`, `TryFindLodge`, `CasinoHeist`, `HackerTrial1`, `YouAreMemberNow`, etc.

---

### 2. Menu/UI Translations (MenuTranslations Folder)

This part handles game interface elements: tooltips, action buttons, notifications, etc.

**Location**: `ElseHeartbreak_Data/InitData/MenuTranslations/{Language}/`

**Menu Translation Files**:

| File | Purpose | Example |
|------|---------|---------|
| `tooltips.{idn}.mtf` | Object Nouns | door, bed, computer |
| `verbs.{idn}.mtf` | Action Verbs | open, close, pick up |
| `notifications.{idn}.mtf` | System Messages | "Door is locked", "Inventory full" |
| `menutext.{idn}.mtf` | UI Buttons/Labels | "open bag", "give" |
| `liquidtypes.{idn}.mtf` | Drink Types | beer, coffee |
| `drugtypes.{idn}.mtf` | Drug Types | Types of pills |
| `errors.{idn}.mtf` | Error Messages | Error notifications |

---

### Start Translating

1. **Create Language Folders**:
   - Dialogue: `ElseHeartbreak_Data/InitData/Translations/{Language}/`
   - Menu: `ElseHeartbreak_Data/InitData/MenuTranslations/{Language}/`

2. **Copy Example Files**:
   - Dialogue: Copy English versions from `Translations/English/` as reference.
   - Menu: Copy templates from `resources/MenuTranslations/Chinese/` in this repo.

3. **Edit `localization.ini`**: Add a new language configuration section:

```ini
[Japanese]
Code=jpn
DisplayName=日本語
TranslationFolder=Japanese
FileIdentifier=jpn
CharacterWidthMultiplier=2.0
```

### Translation File Format (.mtf)

Files use a simple key-value format separated by a Fat Arrow:

```
"Original" => "Translation"
```

Example (`tooltips.chn.mtf`):
```
"door" => "门"
"bed" => "床"
"computer" => "电脑"
```

### [N] Placeholder

For complex phrases involving prepositions, use `[N]` to mark where the object should be inserted:

```
"turn on water in" => "打开[N]的水龙头"
```

When translating `"turn on water in sink"`:
- Plugin detects preposition `" in "`
- Translates object `"sink"` → `"水槽"`
- Result: `"打开水槽的水龙头"`

### Override Files

The plugin automatically combines verb and noun translations (e.g., `open` + `door` → `Open Door`). However, sometimes automatic combination results are not ideal. In such cases, use `_override` files to provide full phrase translations.

**Use Cases**:
- Automatic combination is unnatural or inaccurate.
- Specific fixed phrases need special handling.
- Translation needs to be completely different from literal meaning.

**Naming**: `{OriginalName}_override.{idn}.mtf`

**Example**: If `verbs.chn.mtf` has `"pick up" => "拾取"` and `tooltips.chn.mtf` has `"telephone" => "电话"`, the automatic result is `"拾取电话"`.

If you want to translate it as `"拿起电话"`, add this to `menutext_override.chn.mtf`:

```
"pick up telephone" => "拿起电话"
```

**Load Priority** (Later statements overwrite earlier ones):

| Priority | Format | Example |
|----------|--------|---------|
| 1 | `{name}.{idn}.mtf` | `verbs.chn.mtf` |
| 2 | `{name}_override.{idn}.mtf` | `verbs_override.chn.mtf` |

## Configuration

Edit `localization.ini` in the game directory:

```ini
[General]
BilingualModeEnabled=true      ; Show English + Translation
BilingualToggleKey=F11         ; Toggle hotkey
FallbackToEnglish=true         ; Show English if translation missing

[Chinese]
Code=chn
DisplayName=中文
TranslationFolder=Chinese
FileIdentifier=chn
CharacterWidthMultiplier=2.5   ; Bubble width multiplier for CJK characters
```

### Configuration Parameters

| Parameter | Description |
|-----------|-------------|
| `BilingualModeEnabled` | Enable bilingual mode |
| `BilingualToggleKey` | Hotkey to toggle bilingual mode in-game |
| `FallbackToEnglish` | Fallback to English if translation is missing |
| `Code` | Internal language code |
| `DisplayName` | Language name displayed in UI |
| `TranslationFolder` | Name of the translation folder |
| `FileIdentifier` | Language identifier in filenames |
| `CharacterWidthMultiplier` | Dialogue bubble width multiplier (2.0-2.5 recommended for CJK) |

## Build from Source

Ensure file paths in `.csproj` are correct. Comment out `AfterTargets` if not needed.

```powershell
cd ElseHeartbreakLocalization
dotnet build
```

## Release Guide

If you want to release a new version:

1. Run the Release build locally:
   ```powershell
   dotnet build -c Release
   ```
2. After the build completes, a `Release.zip` will be generated in the project root.
3. Create a new Release (with a corresponding Tag) on GitHub and upload the `Release.zip`.

## License and Credits

This project contains multiple components governed by different open source licenses:

### Plugin Code
Else Heartbreak Localization code is open source.
- License: **CC0-1.0 Universal** (Public Domain)
- See `LICENSE` for details.

### BepInEx
- [BepInEx](https://github.com/BepInEx/BepInEx) framework.
- License: **LGPL-2.1**

### Chinese Translation Text
The Chinese translation text included in this plugin is sourced from the [else-heart-break-chinese](https://github.com/1PercentSync/else-heart-break-chinese) project.
- License: **CC0-1.0 Universal** (Public Domain)
- Author: 1PercentSync
- See `InitData/MenuTranslations/Chinese/LICENSE` for details.

## Contributing

### Notes for Contributing Translations

- Ensure translations are accurate and fit the game context.
- Maintain consistency in translation style.
- Test your translations to ensure they display correctly.
- Consider using `_override.mtf` files for complex phrases.
