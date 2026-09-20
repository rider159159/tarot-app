# M7｜抽牌儀式感升級（純前端，可隨時插隊）

> 目標：補齊「醞釀 → 靜心 → 揭示節奏」三個投報率最高的儀式感細節。零後端改動、零新套件、零素材。
> 對象檔案：`CardPicker.svelte`、`FlippableCard.svelte`、`routes/+page.svelte`。動之前先讀完這三個檔案的現有結構。

## 1. 範圍（只做這三件）

### 1.1 洗牌前置動畫（醞釀感）
- 進入互動選牌（CardPicker 出現）前，播 1.5~2 秒洗牌動畫再顯示牌桌。
- 做法：CardPicker 前面加一個 overlay 狀態（`$state`），內含 5~7 張卡背元素，用 CSS keyframes 做交錯位移＋輕微旋轉（`transform: translate/rotate`），結尾收攏到中央後淡出。
- 純 CSS keyframes 即可，不要引入動畫套件。

### 1.2 靜心呼吸引導（抽牌前專注）
- 按下抽牌（或洗牌動畫結束）後、翻牌前，插入 3 秒引導畫面：一個緩慢縮放的圓（`scale(1)→scale(1.4)→scale(1)`，`ease-in-out`）＋文案「深呼吸，心裡想著你的問題」。
- 提供「跳過」按鈕（右下角小字），跳過偏好存 `localStorage`（`skipBreathing=1`），存過就不再出現。
- 若走過 M4 intake，文案改成帶入精煉問題：「深呼吸，想著：{refinedQuestion}」。

### 1.3 揭示節奏（懸念）
- 多張牌的結果揭示改成**逐張翻**：第 n 張延遲 `n * 250ms` 翻面（FlippableCard 已有 3D 翻牌，只是把「同時翻」改成 stagger）。
- 每張翻開瞬間加一個短暫光暈（`box-shadow` transition 0.6s 後退場）。
- 逆位牌：翻面動畫最後帶 `rotate(180deg)` 的過渡（0.4s），而不是直接以倒置狀態出現；牌面角落維持現有的正逆位標示。

## 2. 無障礙（必做，不是選配）
所有新動畫包在：

```css
@media (prefers-reduced-motion: reduce) {
	/* 動畫全部關閉：洗牌 overlay 直接不顯示、呼吸畫面自動 300ms 帶過、翻牌無 stagger */
}
```

JS 側用 `window.matchMedia('(prefers-reduced-motion: reduce)').matches` 判斷，直接走無動畫分支。

## 3. 驗證
- `pnpm check` 0 error。
- 手動：互動抽牌全流程順（洗牌→靜心→選牌→逐張揭示）；快速模式（非互動）不受影響。
- 系統開啟「減少動態效果」後：三個動畫全部不播、流程仍完整可用。
- 手機寬度（375px）檢查 overlay 與呼吸畫面不破版。

## 4. 禁區
- 不加音效、不加震動、不做卡背 skins（之後另開）。
- 不引入任何動畫/音訊套件。
- 不動抽牌資料流（draw action、API、狀態機都不碰，只包 UI 層）。

## 5. Definition of Done
- [ ] 三個細節都上、reduced-motion 降級可用、手機不破版
- [ ] 快速抽牌模式行為不變
- [ ] 跑過 `/qa`（重點看清單 #3 UI 狀態）
