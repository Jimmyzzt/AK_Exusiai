CREATE TABLE cohorts (
  id TEXT PRIMARY KEY, day TEXT NOT NULL, version TEXT NOT NULL, revision TEXT NOT NULL,
  ascension INTEGER NOT NULL, players INTEGER NOT NULL, mode TEXT NOT NULL, abandoned INTEGER NOT NULL,
  runs INTEGER NOT NULL DEFAULT 0, wins INTEGER NOT NULL DEFAULT 0,
  floor_sum INTEGER NOT NULL DEFAULT 0, duration_sum INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX cohorts_filter ON cohorts(mode, players, day, version, ascension);
CREATE TABLE cohort_mods (
  cohort_id TEXT NOT NULL REFERENCES cohorts(id) ON DELETE CASCADE, mod_id TEXT NOT NULL,
  PRIMARY KEY(cohort_id,mod_id)
);
CREATE INDEX cohort_mod_filter ON cohort_mods(mod_id,cohort_id);
CREATE TABLE cohort_entities (
  cohort_id TEXT NOT NULL REFERENCES cohorts(id) ON DELETE CASCADE, entity_id TEXT NOT NULL, act INTEGER NOT NULL,
  owned INTEGER NOT NULL, owned_wins INTEGER NOT NULL, offered INTEGER NOT NULL, picked INTEGER NOT NULL,
  obtained INTEGER NOT NULL, floor_sum INTEGER NOT NULL, upgraded INTEGER NOT NULL, removed INTEGER NOT NULL,
  picked_runs INTEGER NOT NULL, picked_wins INTEGER NOT NULL,
  PRIMARY KEY(cohort_id,entity_id,act)
);
CREATE TABLE runs (
  id TEXT PRIMARY KEY, owner_hash TEXT NOT NULL, day TEXT NOT NULL,
  version TEXT NOT NULL, revision TEXT NOT NULL, game_version TEXT NOT NULL,
  ascension INTEGER NOT NULL, players INTEGER NOT NULL, mode TEXT NOT NULL,
  victory INTEGER NOT NULL, abandoned INTEGER NOT NULL, floor INTEGER NOT NULL,
  duration INTEGER NOT NULL, received_at INTEGER NOT NULL, payload TEXT NOT NULL,
  cohort_id TEXT NOT NULL REFERENCES cohorts(id), write_nonce TEXT NOT NULL
);
CREATE INDEX runs_owner ON runs(owner_hash,received_at);
CREATE UNIQUE INDEX runs_nonce ON runs(write_nonce);
CREATE INDEX runs_filter ON runs(mode, players, day, version, ascension);
CREATE TABLE entities (
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  entity_id TEXT NOT NULL, act INTEGER NOT NULL,
  owned INTEGER NOT NULL, offered INTEGER NOT NULL, picked INTEGER NOT NULL,
  obtained INTEGER NOT NULL, floor_sum INTEGER NOT NULL,
  upgraded INTEGER NOT NULL, removed INTEGER NOT NULL,
  PRIMARY KEY(run_id, entity_id, act)
);
CREATE TABLE snapshots (
  key TEXT PRIMARY KEY, updated_at INTEGER NOT NULL, payload TEXT NOT NULL
);
-- A revoked installation cannot be replayed into the database after deletion.
CREATE TABLE revoked_owners (owner_hash TEXT PRIMARY KEY, revoked_at INTEGER NOT NULL);
