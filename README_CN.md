# Else Heartbreak Localization

[English README](README.md)

一个 BepInEx 插件，为 **Else Heart.Break()** 提供自定义语言翻译支持，包括菜单/UI 文本、tooltip、MenuItem、通知消息，可以为 CJK字符 调整对话气泡宽度。

![](assets/screenshot_1.png)
![](assets/screenshot_2.png)
![](assets/screenshot_3.png)

## 功能

- **可切换双语模式**: 通过快捷键切换是否同时显示英文和翻译文本（默认F11）
- **菜单和 UI 翻译**: 对 tooltip, menuItem 以及 Notification 都可以进行翻译
- **添加自定义语言按钮**: 主菜单和设置界面自动添加对应语言选择按钮
- **对话气泡宽度**: 可配置 CJK 字符调整气泡宽度

## 安装方法

### 方式一：下载 Release（推荐）
1. 下载最新的 Release 压缩包
2. 将压缩包内容**解压到游戏根目录**（`ElseHeartbreak/`），覆盖同名文件
3. 启动游戏

### 方式二：手动安装
1. 安装 [BepInEx 5.x](https://github.com/BepInEx/BepInEx)（如果未包含）
2. （可选）将BepInEx文件夹放在 `ElseHeartbreak_Data/` 下，修改 doorstop_config.ini， 改为 `target_assembly=ElseHeartbreak_Data\BepInEx\core\BepInEx.Preloader.dll`
2. 将自行编译好的 `ElseHeartbreakLocalization.dll` 复制到 `ElseHeartbreak_Data/BepInEx/plugins/` （如果第二步没有执行，那么BepInEx文件夹在游戏根目录）
3. 将 `assets/localization.ini` 复制到游戏根目录
4. 将 `resources/` 下的文件夹复制到 `ElseHeartbreak_Data/InitData/`

### 配置

不喜欢每次打开游戏后弹出的终端界面？

查找BepInEx文件夹，在`BepInEx\config\BepInEx.cfg`中，将48行的 `Enabled = true` 改为 `Enabled = false` 即可

## 目录结构

安装完成后，你的游戏目录应该长这样：

```
ElseHeartbreak/
├── ElseHeartbreak.exe
├── localization.ini                # 语言配置文件
├── doorstop_config.ini             # Doorstop 配置
├── winhttp.dll                     # Doorstop 加载器
└── ElseHeartbreak_Data/
    ├── BepInEx/
    │   ├── core/
    │   └── plugins/
    │       └── ElseHeartbreakLocalization.dll
    └── InitData/
        ├── Translations/{Language}/         # 游戏对话翻译
        │   └── ...
        └── MenuTranslations/{Language}/     # 菜单/UI 翻译
            ├── tooltips.{idn}.mtf
            └── ...
```

>  **提醒**: 菜单翻译必须放在 `InitData/MenuTranslations/` 文件夹中，**不能**放在 `InitData/Translations/` 文件夹中。否则游戏原生的 Translator 会尝试加载它们并导致报错。

## 翻译者指南

翻译分为两部分：
1. **对话翻译**：位于 `Translations/{Language}/`
2. **菜单/UI 翻译**：位于 `MenuTranslations/{Language}/`

---

### 一、对话翻译（Translations 文件夹）

这是翻译工作的**主体部分**，包含游戏中所有角色的对话内容。

**存放位置**: `ElseHeartbreak_Data/InitData/Translations/{Language}/`

**文件命名格式**: `{角色名}_{场景/事件}.{idn}.mtf`

**文件格式**:
```
"Original Text" => "Translated Text"
```

> **提示**: 游戏原版对话为瑞典语，你可以参考 `Translations/English/` 中的英文翻译来理解原意。

**对话文件列表**（约 358 个文件）:

主要角色包括: `Pixie`, `Felix`, `Hank`, `Petra`, `Frank`, `Monad`, `Yulian`, `Fib`, `Ivan`, `Lars` 等

场景/事件包括: `Arrival`, `FirstDay`, `TryFindLodge`, `CasinoHeist`, `HackerTrial1`, `YouAreMemberNow` 等

---

### 二、菜单/UI 翻译（MenuTranslations 文件夹）

这部分负责翻译游戏界面元素：tooltip、menuItem、通知消息等。

**存放位置**: `ElseHeartbreak_Data/InitData/MenuTranslations/{Language}/`

**菜单翻译文件**:

| 文件 | 用途 | 示例 |
|------|------|------|
| `tooltips.{idn}.mtf` | 物品名词 | door（门）、bed（床）、computer（电脑） |
| `verbs.{idn}.mtf` | 动作动词 | open（打开）、close（关闭）、pick up（拾取） |
| `notifications.{idn}.mtf` | 系统消息 | "Door is locked"（门锁着）、"Inventory full"（背包已满） |
| `menutext.{idn}.mtf` | UI 按钮和标签 | "open bag"（打开背包）、"give"（给予） |
| `liquidtypes.{idn}.mtf` | 饮料类型 | beer（啤酒）、coffee（咖啡） |
| `drugtypes.{idn}.mtf` | 药物类型 | 药片种类 |
| `errors.{idn}.mtf` | 错误消息 | 错误通知 |

---

### 开始翻译

1. **创建语言文件夹**:
   - 对话翻译: `ElseHeartbreak_Data/InitData/Translations/{Language}/`
   - 菜单翻译: `ElseHeartbreak_Data/InitData/MenuTranslations/{Language}/`

2. **复制示例文件**:
   - 对话翻译: 可以从 `Translations/English/` 复制英文版作为参考
   - 菜单翻译: 可以从本仓库的 `resources/MenuTranslations/Chinese/` 复制作为模板

3. **编辑 `localization.ini`**: 添加新的语言配置部分：

```ini
[Japanese]
Code=jpn
DisplayName=日本語
TranslationFolder=Japanese
FileIdentifier=jpn
CharacterWidthMultiplier=2.0
```

### 翻译文件格式 (.mtf)

文件使用简单的键值对格式，中间用 Fat Arrow 隔开：

```
"原文" => "译文"
```

示例 (`tooltips.chn.mtf`):
```
"door" => "门"
"bed" => "床"
"computer" => "电脑"
```

### [N] 占位符

对于包含介词的复杂短语，使用 `[N]` 来标记物体应该插入的位置：

```
"turn on water in" => "打开[N]的水龙头"
```

当翻译 `"turn on water in sink"` 时：
- 插件检测到介词 `" in "`
- 翻译物体 `"sink"` → `"水槽"`
- 结果：`"打开水槽的水龙头"`

### 覆盖文件（Override）

插件会自动将动词和名词组合翻译（如 `open` + `door` → `开门`）。但有时自动组合的结果不理想，这时可以使用 `_override` 文件来提供完整短语的翻译。

**使用场景**：
- 自动组合的翻译不自然或不准确
- 需要特殊处理的固定搭配
- 需要完全不同于字面意思的翻译

**文件命名**: `{原文件名}_override.{idn}.mtf`

**示例**：如果 `verbs.chn.mtf` 中有 `"pick up" => "拾取"`，`tooltips.chn.mtf` 中有 `"telephone" => "电话"`，自动组合结果是 `"拾取电话"`。

但如果你想翻译成 `"拿起电话"`，可以在 `menutext_override.chn.mtf` 中添加：

```
"pick up telephone" => "拿起电话"
```

**加载优先级** （优先使用较晚加载的语句）：

| 优先级 | 格式 | 示例 |
|--------|------|------|
| 1（早加载） | `{name}.{idn}.mtf` | `verbs.chn.mtf` |
| 2（晚加载） | `{name}_override.{idn}.mtf` | `verbs_override.chn.mtf` |


## 配置说明

编辑游戏目录中的 `localization.ini`：

```ini
[General]
BilingualModeEnabled=true      ; 显示英文 + 翻译
BilingualToggleKey=F11         ; 切换快捷键
FallbackToEnglish=true         ; 缺少翻译时显示英文

[Chinese]
Code=chn
DisplayName=中文
TranslationFolder=Chinese
FileIdentifier=chn
CharacterWidthMultiplier=2.5   ; CJK 字符的气泡宽度倍数
```

### 配置参数说明

| 参数 | 说明 |
|------|------|
| `BilingualModeEnabled` | 是否启用双语模式 |
| `BilingualToggleKey` | 游戏内切换双语模式的快捷键 |
| `FallbackToEnglish` | 翻译缺失时是否回退到英文 |
| `Code` | 语言内部代码 |
| `DisplayName` | 在 UI 上显示的语言名称 |
| `TranslationFolder` | 翻译文件夹名称 |
| `FileIdentifier` | 翻译文件名中的语言标识 |
| `CharacterWidthMultiplier` | 对话气泡宽度倍数（CJK 字符建议 2.0-2.5） |

## 从源码构建

自行阅读 `.csproj`，确保对应文件路径正确。如果不需要，也可以注释掉 `AfterTargets`。

```powershell
cd ElseHeartbreakLocalization
dotnet build
```

## 许可协议与致谢

本项目引用了多个项目：

### BepInEx
- [BepInEx](https://github.com/BepInEx/BepInEx) 框架
- 协议: **LGPL-2.1**

### 中文翻译文本
本插件示例用的中文翻译文本来源于 [else-heart-break-chinese](https://github.com/1PercentSync/else-heart-break-chinese) 项目
- 协议: **CC0-1.0 Universal** (Public Domain)