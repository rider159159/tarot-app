-- Phase 1: Auth-aware schema
-- Drop old anonymous readings table and policies from 001 (if exists)
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'readings') THEN
    DROP POLICY IF EXISTS "allow_anonymous_insert" ON readings;
    DROP POLICY IF EXISTS "allow_anonymous_select" ON readings;
    DROP TABLE readings;
  END IF;
END
$$;

-- ============================================================
-- Profiles table
-- ============================================================
CREATE TABLE profiles (
  id           UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  display_name TEXT NOT NULL,
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "profiles_select_own" ON profiles
  FOR SELECT USING (auth.uid() = id);

CREATE POLICY "profiles_update_own" ON profiles
  FOR UPDATE USING (auth.uid() = id)
  WITH CHECK (auth.uid() = id);

-- Auto-create profile on signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, display_name)
  VALUES (NEW.id, split_part(NEW.email, '@', 1));
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- ============================================================
-- Readings table (with user ownership)
-- ============================================================
CREATE TABLE readings (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id        UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  spread_type    TEXT NOT NULL CHECK (spread_type IN ('single', 'three-card', 'celtic-cross')),
  question       TEXT,
  cards          JSONB NOT NULL,
  interpretation TEXT,
  notes          TEXT,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_readings_user_created ON readings (user_id, created_at DESC);

ALTER TABLE readings ENABLE ROW LEVEL SECURITY;

CREATE POLICY "readings_select_own" ON readings
  FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "readings_insert_own" ON readings
  FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "readings_update_own" ON readings
  FOR UPDATE USING (auth.uid() = user_id)
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "readings_delete_own" ON readings
  FOR DELETE USING (auth.uid() = user_id);
