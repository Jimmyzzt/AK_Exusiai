CREATE TABLE mod_catalog (
  id TEXT PRIMARY KEY, title TEXT NOT NULL DEFAULT '', workshop_id TEXT,
  uses INTEGER NOT NULL DEFAULT 0, official_tags TEXT NOT NULL DEFAULT '[]',
  primary_tag TEXT NOT NULL DEFAULT 'untagged', status TEXT NOT NULL DEFAULT 'pending',
  checked_at INTEGER NOT NULL DEFAULT 0, next_check INTEGER NOT NULL DEFAULT 0,
  failures INTEGER NOT NULL DEFAULT 0
);
INSERT INTO mod_catalog(id,uses)
  SELECT m.mod_id,SUM(c.runs) FROM cohort_mods m JOIN cohorts c ON c.id=m.cohort_id GROUP BY m.mod_id;
CREATE INDEX mod_catalog_due ON mod_catalog(next_check,uses DESC);
CREATE TABLE enrichment_state (key TEXT PRIMARY KEY,value INTEGER NOT NULL);
CREATE TABLE run_details (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  win3 INTEGER NOT NULL, floor3 INTEGER NOT NULL, max_act INTEGER NOT NULL
);
CREATE TABLE run_acts (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE, act INTEGER NOT NULL,
  floors INTEGER NOT NULL, completed INTEGER NOT NULL, snapshot_known INTEGER NOT NULL,
  PRIMARY KEY(run_id,act)
);
CREATE TABLE act_decks (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE, entity_id TEXT NOT NULL,
  act INTEGER NOT NULL, variant INTEGER NOT NULL,
  PRIMARY KEY(run_id,entity_id,act,variant)
);
CREATE TABLE detailed_entities (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE, entity_id TEXT NOT NULL,
  act INTEGER NOT NULL, variant INTEGER NOT NULL, offered INTEGER NOT NULL, picked INTEGER NOT NULL,
  obtained INTEGER NOT NULL, floor_sum INTEGER NOT NULL, upgraded INTEGER NOT NULL, removed INTEGER NOT NULL,
  PRIMARY KEY(run_id,entity_id,act,variant)
);
CREATE TABLE card_offers (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE, seq INTEGER NOT NULL,
  entity_id TEXT NOT NULL, variant INTEGER NOT NULL, act INTEGER NOT NULL,
  floor INTEGER NOT NULL, position INTEGER NOT NULL, picked INTEGER NOT NULL,
  expected3 REAL NOT NULL, expected_all REAL NOT NULL,
  PRIMARY KEY(run_id,seq)
);
CREATE INDEX card_offers_metrics ON card_offers(entity_id,act,variant,run_id);
CREATE TABLE fights (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE, seq INTEGER NOT NULL,
  act INTEGER NOT NULL, floor INTEGER NOT NULL, position INTEGER NOT NULL,
  encounter TEXT NOT NULL, damage INTEGER NOT NULL, turns INTEGER NOT NULL,
  expected_damage REAL, expected_turns REAL,
  PRIMARY KEY(run_id,seq)
);
CREATE TABLE skill_models (
  owner_hash TEXT NOT NULL, ascension INTEGER NOT NULL, players INTEGER NOT NULL,
  mode TEXT NOT NULL, horizon TEXT NOT NULL, mean REAL NOT NULL, variance REAL NOT NULL,
  samples INTEGER NOT NULL, PRIMARY KEY(owner_hash,ascension,players,mode,horizon)
);
CREATE TABLE survival_baselines (
  ascension INTEGER NOT NULL, players INTEGER NOT NULL, mode TEXT NOT NULL,
  horizon TEXT NOT NULL, act INTEGER NOT NULL, floor INTEGER NOT NULL,
  samples INTEGER NOT NULL, wins INTEGER NOT NULL,
  PRIMARY KEY(ascension,players,mode,horizon,act,floor)
);
CREATE TABLE fight_baselines (
  ascension INTEGER NOT NULL, players INTEGER NOT NULL, mode TEXT NOT NULL,
  act INTEGER NOT NULL, encounter TEXT NOT NULL, samples INTEGER NOT NULL,
  damage_sum INTEGER NOT NULL, turns_sum INTEGER NOT NULL,
  PRIMARY KEY(ascension,players,mode,act,encounter)
);
