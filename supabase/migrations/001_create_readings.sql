CREATE TABLE IF NOT EXISTS readings (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  spread_type TEXT NOT NULL CHECK (spread_type IN ('single', 'three-card', 'celtic-cross')),
  question    TEXT NOT NULL DEFAULT '',
  cards       JSONB NOT NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_readings_created_at ON readings (created_at DESC);

ALTER TABLE readings ENABLE ROW LEVEL SECURITY;

CREATE POLICY "allow_anonymous_insert" ON readings
  FOR INSERT TO anon WITH CHECK (true);

CREATE POLICY "allow_anonymous_select" ON readings
  FOR SELECT TO anon USING (true);
