# GPT、Cursor 與 Git 協作

## 本次工作入口

不對稱 DEMO 規則：`Docs/ASYMMETRIC_DESIGN.md`。既有天賦／動作規格：`Docs/GDD.md`。差異對照：`Docs/GDD_RECONCILIATION.md`。AI 工作規範：根目錄 `AGENTS.md`。

## 日常流程

1. 先取得 Git 最新版本，閱讀上述文件與 `Docs/DESIGN_LOG.md`。
2. 用 GPT 確認規則；提出修改時，標出文件章節，給完整 Markdown 替換段落與一列 DESIGN_LOG。數值未定保持【草案】；只有負責人可確認。
3. GPT 可做小部分程式修改；Cursor 主要負責 Unity 建置與系統整合。程式修改需註明對應規則章節。
4. 執行 `dotnet run --project Tools/AsymmetricRulesTests/RulesTests.csproj`，再以 Unity Play 驗證場景。改變終局或復活規則時，新增相應案例。
5. 同一次提交包含規則、程式及決策紀錄，推送到 GitHub。下一輪討論以該提交為準，不用對話記憶取代 repo。

## 可直接交給 Cursor 的工作指令

```text
先讀 AGENTS.md、Docs/ASYMMETRIC_DESIGN.md、Docs/GDD_RECONCILIATION.md、Docs/DESIGN_LOG.md。
以 Assets/Scenes/AsymmetricDemo.unity 為目前不對稱 DEMO 入口。
保留既有 TalentTestArena 作動作／詞條驗證。
依指定章節實作，變更規則時同步更新文件與設計紀錄；未定數值保持草案。
執行 Tools/AsymmetricRulesTests，並檢查 Unity Console 與場景。
正式連線、完整任務／RTS、天賦戰鬥與美術接入仍是後續工作，勿將本機原型宣稱為已完成連線。
```

啟動、操作與 Windows 建置方法見 `Docs/DEMO_GUIDE.md`。
