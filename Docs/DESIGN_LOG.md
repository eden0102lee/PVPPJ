# 設計決策紀錄（Design Log）

> 供 Cursor、ChatGPT、Codex 與人類快速查看 **最近企劃變更**。新條目加在 **表上方**（最新在前）。

| 日期 | 來源 | GDD／文件章節 | 決策摘要 |
|------|------|---------------|----------|
| 2026-10-23 | 負責人要求 / Codex | ASYMMETRIC_DESIGN §§1–8；GDD_RECONCILIATION R1–R7 | 依目前規則新增獨立不對稱 DEMO：30 分鐘討伐、選路暫停、獨立五分鐘終局、保留任務復活；60×60 峽谷地圖。數值為草案，舊天賦測試場保留；GPT 確認規則、Cursor 主要建置。 |
| 2026-10-23 | Cursor | 協作流程 | 建立 `AGENTS.md`、`ASYMMETRIC_DESIGN.md`、`GDD_RECONCILIATION.md`、`CHATGPT_SETUP.md`；GDD 新增 §1.2 指向不對稱匯入流程；對稱 MOBA【已確認】在 R1–R7 定案前維持不變。 |

## 條目格式（複製使用）

```markdown
| YYYY-MM-DD | ChatGPT／Cursor／Codex／人類 | GDD §x.x 或 Docs/… | 一句話摘要 |
```

## 與 AI 協作

- **ChatGPT** 提出設計變更時，應同時給出 **一列** 上表內容（日期可留空由人類補）。
- **Cursor** 因實作被迫調整設計時，在同一 PR／commit 更新 GDD【草案】段落 **並** 新增本表一列。
- 僅 **人類** 可將 GDD 段落標為【已確認】。
