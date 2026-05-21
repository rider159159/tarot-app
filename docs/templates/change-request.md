<!--
Track B 修改需求 brief 範本

用法：
1. 複製本檔到 docs/changes/<kebab-case-slug>.md
2. 填入下列各欄內容
3. 刪除本 HTML 註解區塊
4. 完成後用 /build 開始實作（會啟動 regression 紀律）

對應流程：/feature 判定為 Track B → 讀既有實作 → 套本範本產出 → /build 接力
-->

## 修改項目：<簡稱>

### 現況
<目前的行為，附檔案路徑與行號>

### 目標
<改完後應有的行為>

### 後向相容評估
- 既有資料：<既有 readings / profiles 會不會壞>
- 破壞性變更：<逐項列出，或「無」>
- 需前後端同時部署？<是 / 否，附理由>

### 技術影響
- 前端：<改哪個 route / component / 型別>
- 後端：<改哪個 Service / Controller / DTO>
- DB：<需要的 migration，或「無」>

### 待釐清
- <仍不確定的點>
