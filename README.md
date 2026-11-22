# BISBuddy（台服 / Dalamud API 13）

台灣版《最終幻想XIV》用的 [RajahOmen/BisBuddy](https://github.com/RajahOmen/BisBuddy)（作者 Lily）。這個分支給 [cycleapple/XIVTCLauncher](https://github.com/cycleapple/XIVTCLauncher) 搭配 [yanmucorp/Dalamud](https://github.com/yanmucorp/Dalamud) **API 13 / .NET 9**。

它從配裝網站匯入裝備，在視窗裡標示還缺的物品，並追蹤物品欄狀態。

**不要裝到官方 Dalamud API 14／15**（國際服 XIVLauncher）。這個組建只給台服。這不是官方 RajahOmen 插件。授權沿用上游 [AGPL-3.0-or-later](LICENSE.md)。

視窗標示使用 [MidoriKami/KamiToolKit](https://github.com/MidoriKami/KamiToolKit)（MIT，Copyright (c) 2024 MidoriKami）。這次帶入的是 `fdf0b553853ea81ce0dae5fecbe248c9ff641c9f`（最後一版 `Dalamud.NET.Sdk` 13.1.0）。

## 這個版本

版本標示是 **0.2.9-tw**。組件版本是 `0.2.9.0`。組建自 yanmucorp Dalamud `13.0.0.16`（`Dalamud.NET.Sdk` 13.1.0，`FFXIVClientStructs` 0.0.6966.0）。

- 介面文字是繁體中文。Dalamud 介面語言代碼 `tw`（以及 `zh`、`zh-TW`、`zh-Hant`）會載入 `Resource.zh-Hant`。請在 `/xlsettings` 把介面語言設成 **繁體中文**。
- 物品、職業、部位與魔晶石名稱在遊戲內依列 ID 讀取用戶端繁中資料（先試 `ChineseTraditional`，再試 yanmucorp 的 `TraditionalChinese`）。沒有寫死名稱表。
- 貼上英文或繁中物品名稱時，會同時查英文與繁中的 Item 表，再用物品 ID 當主鍵。
- 從 xivgear.app 匯入仍走 `api.xivgear.app` 的物品 ID。複製到 xivgear 會產生該站 Import Sheet 接受的 JSON（物品 ID、英文職業縮寫），請貼到 https://xivgear.app/?page=importsheet 。不會自動上傳。
- 既有的複製 JSON、複製來源網址、複製原始文字仍保留。Etro 網址與這個插件自己的 JSON 匯入／匯出也保留。

## 安裝

1. 在 XIVTCLauncher 啟用 Dalamud，進過一次遊戲讓資料夾出現，然後**關閉遊戲**。
2. 下載 `BisBuddy-0.2.9-tw.zip`。解開後是一個 `BisBuddy` 資料夾，含 `BisBuddy.dll`、`BisBuddy.json`，以及一起載入的組件（含 `KamiToolKit.dll` 與 `Assets/`）。
3. 把這個資料夾放到下面的路徑。`dll` 和 `json` 要在同一層，不要再多包一層：

```text
%AppData%\FFXIVSimpleLauncher\Dalamud\Config\devPlugins\BisBuddy\
```

帳號如果用自己的插件設定檔，改放：

```text
%AppData%\FFXIVSimpleLauncher\Profiles\<設定檔 ID>\Dalamud\devPlugins\BisBuddy\
```

4. 進遊戲後用 `/xldev` 啟用 BISBuddy。`/bisbuddy` 與 `/bis` 開啟主視窗。

## 遊戲內請先試這些

這個組建沒有在台服客戶端裡跑過。載入後請依序確認：

1. `/xldev` 能載入，主視窗按鈕是繁體中文，物品名稱是官方繁中。
2. 貼上一條 xivgear.app 連結，配裝能進來。
3. 在配裝上按 **Xivgear**（或右鍵「複製到 Xivgear」），把剪貼簿貼到 https://xivgear.app/?page=importsheet 。物品應以 ID 匯入，職業縮寫是英文。
4. 若有開啟標示，看需求／貪婪、商店、鑲嵌、市場、物品欄與物品提示。附加元件節點配置若與這份 `FFXIVClientStructs` 不一致，標示可能對不準或沒有出現；插件日誌會留下錯誤，不應因此寫入未確認的位址。

## 自行編譯

`Dalamud.NET.Sdk` 13.1.0 對本機的 `Dalamud.dll` 編譯。把 `DALAMUD_HOME` 指到 yanmucorp Dalamud `13.0.0.16` 裡含有 `Dalamud.dll` 的目錄（[發行檔](https://github.com/yanmucorp/Dalamud/releases/download/26-10-03-01/latest.7z) 的 `latest.7z`）。沒有設定時，SDK 會去官方啟動器的 `%AppData%\XIVLauncher\addon\Hooks\dev\`。

1. 安裝 .NET 9 SDK。
2. 在這個目錄執行 `dotnet build -c Release -p:EnableWindowsTargeting=true`。

專案目標是 `net9.0-windows7.0`。

## English

Unofficial Traditional Chinese adaptation of [RajahOmen/BisBuddy](https://github.com/RajahOmen/BisBuddy) by Lily, for yanmucorp Dalamud API 13 / .NET 9 and XIVTCLauncher. Do not install this on official Dalamud API 14 or 15. Upstream remains AGPL-3.0-or-later.

Window highlighting uses [MidoriKami/KamiToolKit](https://github.com/MidoriKami/KamiToolKit) (MIT, Copyright (c) 2024 MidoriKami), vendored at `fdf0b55`, the last `Dalamud.NET.Sdk` 13.1.0 revision.

Version label **0.2.9-tw** (assembly `0.2.9.0`). UI strings load `Resource.zh-Hant` when Dalamud’s UI language is `tw`. Item, job, slot, and materia names are read from the Traditional Chinese Lumina sheets by row id. xivgear import stays on numeric ids from `api.xivgear.app`. The Xivgear copy action writes the Import Sheet JSON (item ids and an English job abbreviation) for https://xivgear.app/?page=importsheet .

Extract `BisBuddy\` from `BisBuddy-0.2.9-tw.zip` into `%AppData%\FFXIVSimpleLauncher\Dalamud\Config\devPlugins\` and enable it in `/xldev`.

Highlight node layouts were not verified in the Taiwan client. If a window’s nodes do not match `FFXIVClientStructs` 0.0.6966.0, highlighting for that window can miss; the plugin logs the failure instead of using an unconfirmed offset.
