# Agent 協作規範（Cursor / ChatGPT / Codex）

本 repo：[eden0102lee/PVPPJ](https://github.com/eden0102lee/PVPPJ) — Unity 6 MOBA 原型（GamePJ260820）。

## 目前不對稱 DEMO 的適用規格

負責人 2026-10-23 指示依目前討論規則製作 DEMO。AsymmetricDemo 對局流程以 Docs/ASYMMETRIC_DESIGN.md 為依據；原 GDD 的動作／天賦規格與 TalentTestArena 保留。差異見 Docs/GDD_RECONCILIATION.md，不要把原對稱核心勝負套回新場景。GPT 確認規則與少量程式修改，Cursor 主要建置整合；驗收見 Docs/DEMO_GUIDE.md。
## 文件優先順序

1. [Docs/GDD.md](Docs/GDD.md) — 主企劃（【已確認】優先於【草案】）
2. [Docs/ACTION_MODIFIER_REGISTRY.md](Docs/ACTION_MODIFIER_REGISTRY.md) — 動作／詞條登錄
3. [Docs/ASYMMETRIC_DESIGN.md](Docs/ASYMMETRIC_DESIGN.md) — 不對稱模式匯入稿（在 [GDD_RECONCILIATION.md](Docs/GDD_RECONCILIATION.md) 定案前為補充，不可覆寫 GDD【已確認】）
4. [Docs/DESIGN_LOG.md](Docs/DESIGN_LOG.md) — 決策紀錄
5. `GamePJ260820/Assets/Scripts/` — 實作以 GDD 章節為準

## 狀態標記

- **【已確認】**：僅 **專案負責人（人類）** 可新增或修改。
- **【草案】**：AI 可撰寫、修訂；實作時應標註對應 GDD 章節，若與【已確認】衝突則停止並改走 reconciliation。

## 實作規則（Cursor / Codex 寫程式碼時）

- 新系統或行為變更前，註明對應 **GDD 章節編號**（例如「依 GDD §3.2」）。
- 若程式與 GDD 不一致：**同一變更** 內更新 GDD【草案】或 [DESIGN_LOG.md](Docs/DESIGN_LOG.md)，勿 silent drift。
- Unity 專案根目錄為 `GamePJ260820/`（非 repo 根目錄）。
- 勿提交 `Library/`、`Temp/`、本機 `UserSettings/`。

## 企劃規則（ChatGPT 討論設計時）

- 開始前先讀本檔與 `Docs/GDD.md`（GitHub 連線時自 repo 讀取）。
- 輸出變更時提供：**完整 Markdown 章節** + 【草案】標記 + [DESIGN_LOG.md](Docs/DESIGN_LOG.md) 一列條目。
- 不對稱相關內容寫入或更新 `Docs/ASYMMETRIC_DESIGN.md` 結構；與 GDD 衝突處更新 `Docs/GDD_RECONCILIATION.md` 的 R-ID 列。
- 不要假設 ChatGPT 對話紀錄為真相；**以 Git 內 Docs 為準**。

## Git 與分支建議

- 日常實作：`main` 或現有功能分支。
- 純企劃匯入（可選）：`design/描述性名稱`，PR 審閱後合併。
- 合併前：`git pull`；Docs 與程式分 commit 可選，但 DESIGN_LOG 應與 GDD 變更同 PR。

## 快速連結

- 企劃：[Docs/GDD.md](Docs/GDD.md)
- ChatGPT 設定說明：[Docs/CHATGPT_SETUP.md](Docs/CHATGPT_SETUP.md)
- README 開專案方式：[README.md](README.md)

## Unity MCP

即時場景、物件元件與 Console 請使用 unityMCP，詳見 Docs/UNITY_MCP.md。先列出 instances、選擇 GamePJ260820 對應實例並確認 project/info 的路徑。只讀檢查不可清除 Console、存檔、切換場景或進入 Play Mode；修改仍依本檔既有規範。MCP 不可用時，明確區分原始檔內容與未取得的 Editor 即時狀態。
