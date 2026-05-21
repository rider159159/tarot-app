<!--
Track C 除錯 brief 範本

用法：
1. 複製本檔到 docs/bugs/<kebab-case-slug>.md
2. 填入下列各欄內容
3. 刪除本 HTML 註解區塊
4. 完成後用 /build 開始修復（會啟動 root cause + regression 紀律）

對應流程：/feature 判定為 Track C → 確認重現 → 定位 root cause → 套本範本產出 → /build 接力
-->

## Bug：<簡稱>

### 重現步驟
1. ...
2. ...

### 預期 vs 實際
- 預期：...
- 實際：...

### Root cause
<根本原因，附檔案路徑與行號>

### 影響範圍
- <同類 pattern 會受影響的位置；標 ⚠ 表示要一併修>

### 修復方向
<建議怎麼修；但不要動 code>
