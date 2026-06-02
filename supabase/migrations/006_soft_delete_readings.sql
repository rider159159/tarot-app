-- Phase: schema change — soft delete for readings
--
-- 個人歷史頁面的「刪除單一紀錄」改為軟刪除：不真的從資料庫移除，
-- 而是寫入刪除時間戳，後端所有查詢都排除 deleted_at IS NOT NULL 的紀錄。
-- 這樣保留資料以便日後復原／稽核，使用者端看到的行為與硬刪除相同。
--
-- NULL = 仍存在；有值 = 已刪除（值為刪除當下的 UTC 時間）。

ALTER TABLE readings
  ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL;

-- 列表／統計幾乎都只看「未刪除」的紀錄，用 partial index 讓常見查詢更快。
CREATE INDEX IF NOT EXISTS idx_readings_user_active
  ON readings (user_id, created_at DESC)
  WHERE deleted_at IS NULL;
