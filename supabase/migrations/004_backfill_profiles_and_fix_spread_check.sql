-- Phase: data repair
--
-- 1. Backfill profiles for users created before migration 002.
--    The handle_new_user() trigger only fires on future auth.users INSERTs,
--    so any account that existed before 002 was applied has no profiles row.
--    Such accounts hit a 404 on GET /api/profile (surfaced as a 500 on the
--    profile page because the server load fails the whole Promise.all).
--    display_name mirrors handle_new_user(): the local-part of the email.
--
-- 2. Add 'weekly-fortune' to the readings.spread_type CHECK constraint.
--    The app (SpreadTypeToString / weekly-fortune flow) writes this value,
--    but the constraint never listed it, so weekly-fortune inserts would fail.

-- 1. Backfill missing profiles ------------------------------------------------
INSERT INTO public.profiles (id, display_name, created_at, updated_at)
SELECT u.id, split_part(u.email, '@', 1), u.created_at, now()
FROM auth.users u
WHERE NOT EXISTS (SELECT 1 FROM public.profiles p WHERE p.id = u.id);

-- 2. Fix the spread_type CHECK constraint -------------------------------------
ALTER TABLE readings DROP CONSTRAINT IF EXISTS readings_spread_type_check;

ALTER TABLE readings ADD CONSTRAINT readings_spread_type_check
  CHECK (spread_type IN (
    'single',
    'three-card',
    'three-card-time',
    'three-card-problem',
    'three-card-linear',
    'celtic-cross',
    'weekly-fortune'
  ));
