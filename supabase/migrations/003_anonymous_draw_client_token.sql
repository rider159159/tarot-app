-- Phase: Anonymous draw support
-- Adds client_token to track readings imported from anonymous (logged-out) draws.
-- NULL for readings created directly while logged in. Filled when an anonymous
-- draw is later imported after login. Partial unique index prevents the same
-- anonymous draw being saved twice if the user accidentally triggers import twice.

ALTER TABLE readings
  ADD COLUMN IF NOT EXISTS client_token UUID NULL;

CREATE UNIQUE INDEX IF NOT EXISTS readings_client_token_unique
  ON readings (client_token)
  WHERE client_token IS NOT NULL;
