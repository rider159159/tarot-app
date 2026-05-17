-- Phase: schema change
--
-- Add 'custom' to the readings.spread_type CHECK constraint.
-- The custom-spread feature lets users draw a self-chosen number of cards
-- (1–10); those readings are stored with spread_type = 'custom'. The card
-- count is not a column — it is derived from the length of the cards JSONB
-- array.

ALTER TABLE readings DROP CONSTRAINT IF EXISTS readings_spread_type_check;

ALTER TABLE readings ADD CONSTRAINT readings_spread_type_check
  CHECK (spread_type IN (
    'single',
    'three-card',
    'three-card-time',
    'three-card-problem',
    'three-card-linear',
    'celtic-cross',
    'weekly-fortune',
    'custom'
  ));
