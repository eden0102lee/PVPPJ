# PVPPJ

3D 俯視 MOBA 競技原型（類 DOTA）：摧毀敵方核心獲勝，並以模組化天賦樹與動作向戰鬥為差異化賣點。

| 項目 | 內容 |
|------|------|
| 引擎 | Unity 6（**6000.0.39f1**）+ URP 17.0.3 |
| Unity 專案目錄 | [`GamePJ260820/`](GamePJ260820/) |
| 企劃 | [`Docs/GDD.md`](Docs/GDD.md) |
| 動作／詞條表 | [`Docs/ACTION_MODIFIER_REGISTRY.md`](Docs/ACTION_MODIFIER_REGISTRY.md) |
| 外掛與 Asset Store | [`GamePJ260820/Assets/Docs/PLUGIN_SETUP.md`](GamePJ260820/Assets/Docs/PLUGIN_SETUP.md) |

## 開啟專案

1. 安裝 Unity Hub 與 **Unity 6000.0.39f1**。
2. 以 Hub 開啟 `GamePJ260820/`（不要開外層資料夾）。
3. 首次開啟會重建 `Library/`（本機產生，不進 Git）。
4. 開場場景：`Assets/Scenes/TalentTestArena.unity`。
5. 外掛異動後，在 Editor 執行 **GamePJ > Plugin Setup > Run Conflict Audit**。

## 目錄

```
PVPPJ/
  Docs/                 企劃與登錄表
  GamePJ260820/         Unity 專案
    Assets/Scripts/     原型邏輯（天賦、戰鬥、Arena、操作）
    Assets/Scenes/      TalentTestArena、TestArena
    Packages/           lilToon、Cozy Builder（embedded）
    ProjectSettings/
```

`Library/`、`Temp/`、`Logs/`、`UserSettings/` 與產生的 `.csproj` / `.sln` 已列入 `.gitignore`。

## Git LFS

GitHub 單檔上限 100 MB。下列 ToonScapes skybox 貼圖以 Git LFS 追蹤：

- `TSI_Skybox_Background_01A.tga`
- `TSI_Skybox_Midground_01A.tga`

Clone 後請確認已安裝 [Git LFS](https://git-lfs.com/)。
