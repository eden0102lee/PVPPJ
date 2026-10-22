# 不對稱 DEMO：啟動、驗收與協作

## 啟動

1. Unity Hub 開啟 repo 內 `GamePJ260820/`，版本 **6000.0.39f1**。
2. 開啟 `Assets/Scenes/AsymmetricDemo.unity`，或選單 **GamePJ > Asymmetric Demo > Open Demo**。
3. Play，從左側選英雄／冒險家／軍團。
4. WASD 移動，滑鼠瞄準，左鍵攻擊、右鍵範圍攻擊；金色編號據點按住 F 三秒完成任務。
5. 看右上小地圖前往右上魔王城。討伐後，最高有效傷害貢獻者選終局。右上小地圖也顯示玩家及據點位置。

本機版是 **1 人 + 3 AI**。畫面採幾何灰盒，未完成正式連線或最終美術；舊天賦編輯器保留於原場景。

## 快速檢查

F1 開測試工具，可切 20 倍速、跳到任務截止前五秒、讓 P1 完成討伐、測試死亡與終局期限。這些按鈕是測試捷徑，不是正式玩法。

- 驗證 30 分鐘討伐失敗：不擊敗魔王並讓討伐倒數結束。
- 驗證魔化：F1 完成討伐，選魔化；確認其他人城外復活、水晶／全滅／終局歸零皆可判勝。
- 驗證護送：選護送；聖器隨持有者移動，死亡掉落，AI 可接手；接近王國軍完成護送。
- 驗證暫停：停留於本機選路面板，倒數與 AI 位置均不改變。
- 驗證復活資格：完成第二項任務取得資格，在驟死死亡後仍可復活。

## 建置

**GamePJ > Asymmetric Demo > Build Windows Demo**，輸出 `GamePJ260820/Builds/AsymmetricDemo/AsymmetricDemo.exe`；只建置此場景，不更改其他場景的 Build Settings。初次輸出可能花較久，需安裝 Windows Build Support。

批次：`Unity.exe -batchmode -quit -projectPath <repo>/GamePJ260820 -executeMethod GamePJ.EditorTools.AsymmetricDemoEditor.Build -logFile <log>`。批次前關閉同一專案的 Editor，或直接使用選單建置。

## 規則測試

安裝 .NET 7 SDK 後，repo 根目錄執行：

```powershell
dotnet run --project Tools/AsymmetricRulesTests/RulesTests.csproj
```

測試不依賴 NuGet 套件、不開 Unity。覆蓋任務截止、選擇暫停、貢獻上限、選路權、獨立五分鐘、獻祭、復活、聖器接手與結算。Unity 場景／輸入／美術仍需 Play 驗收。

## GPT 與 Cursor 工作方式

規則入口：`Docs/ASYMMETRIC_DESIGN.md`；舊規則對照：`Docs/GDD_RECONCILIATION.md`；最近決策：`Docs/DESIGN_LOG.md`。

GPT 用來確認規則與小幅程式修正，每次提供修改章節與決策紀錄。Cursor 主要建置、整合、測試；開始前讀 `AGENTS.md` 和這三份文件。未定數值保持【草案】，不由 AI 自行升級確認。

請讓 Cursor 下一步處理：正式連線權威與狀態同步、天賦戰鬥接入、任務系統與 RTS 指令、既有美術資產整合。不要把 F1 測試工具變成正式遊戲規則。原型的本機驗證不代表多人同步驗證完成。
