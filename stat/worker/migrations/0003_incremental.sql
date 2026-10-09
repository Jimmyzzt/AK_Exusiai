-- Additive migration. Original history and snapshots are retained.
CREATE TABLE IF NOT EXISTS stat_state (
 id INTEGER PRIMARY KEY CHECK(id=1),revision INTEGER NOT NULL DEFAULT 0,
 catalog_revision INTEGER NOT NULL DEFAULT 0,policy TEXT NOT NULL DEFAULT 'null',
 lease TEXT,lease_until INTEGER NOT NULL DEFAULT 0,
 window INTEGER NOT NULL DEFAULT 0,reads INTEGER NOT NULL DEFAULT 0,writes INTEGER NOT NULL DEFAULT 0,
 day INTEGER NOT NULL DEFAULT 0,day_reads INTEGER NOT NULL DEFAULT 0,day_writes INTEGER NOT NULL DEFAULT 0,cooldown INTEGER NOT NULL DEFAULT 0
);
INSERT OR IGNORE INTO stat_state(id) VALUES(1);
CREATE TABLE IF NOT EXISTS stat_dirty (
 seq INTEGER PRIMARY KEY AUTOINCREMENT,run_id TEXT NOT NULL UNIQUE,generation INTEGER NOT NULL DEFAULT 1
);
CREATE TABLE IF NOT EXISTS stat_facts (
 run_id TEXT PRIMARY KEY,contribution TEXT NOT NULL,max_act INTEGER NOT NULL,
 extended INTEGER NOT NULL,win3 INTEGER,algorithm INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS stat_cells (
 key TEXT PRIMARY KEY,dims TEXT NOT NULL,projection TEXT NOT NULL,
 payload TEXT NOT NULL,revision INTEGER NOT NULL,active INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS stat_cells_revision ON stat_cells(revision,key);
CREATE INDEX IF NOT EXISTS runs_cohort ON runs(cohort_id);
CREATE TRIGGER IF NOT EXISTS stat_new_run AFTER INSERT ON runs BEGIN
 INSERT INTO stat_dirty(run_id) VALUES(NEW.id) ON CONFLICT(run_id) DO UPDATE SET generation=generation+1;
END;
CREATE TRIGGER IF NOT EXISTS stat_changed_run AFTER UPDATE OF victory,abandoned,floor,duration,day,version,revision,ascension,players,mode,cohort_id ON runs BEGIN
 INSERT INTO stat_dirty(run_id) VALUES(NEW.id) ON CONFLICT(run_id) DO UPDATE SET generation=generation+1;
END;
CREATE TRIGGER IF NOT EXISTS stat_removed_run AFTER DELETE ON runs BEGIN
 INSERT INTO stat_dirty(run_id) VALUES(OLD.id) ON CONFLICT(run_id) DO UPDATE SET generation=generation+1;
 UPDATE mod_catalog SET uses=MAX(0,uses-1) WHERE id IN(SELECT mod_id FROM cohort_mods WHERE cohort_id=OLD.cohort_id);
END;
CREATE TRIGGER IF NOT EXISTS stat_mod_metadata AFTER UPDATE OF title,uses,primary_tag,official_tags,workshop_id,status ON mod_catalog
 WHEN OLD.title!=NEW.title OR OLD.uses!=NEW.uses OR OLD.primary_tag!=NEW.primary_tag OR OLD.official_tags!=NEW.official_tags OR OLD.workshop_id IS NOT NEW.workshop_id OR OLD.status!=NEW.status BEGIN
 UPDATE stat_state SET catalog_revision=catalog_revision+1 WHERE id=1;
END;
CREATE TRIGGER IF NOT EXISTS stat_mod_added AFTER INSERT ON mod_catalog BEGIN
 UPDATE stat_state SET catalog_revision=catalog_revision+1 WHERE id=1;
END;
CREATE TRIGGER IF NOT EXISTS stat_mod_reclassified AFTER UPDATE OF primary_tag ON mod_catalog WHEN OLD.primary_tag!=NEW.primary_tag BEGIN
 INSERT INTO stat_dirty(run_id) SELECT r.id FROM cohort_mods m JOIN runs r ON r.cohort_id=m.cohort_id WHERE m.mod_id=NEW.id
 ON CONFLICT(run_id) DO UPDATE SET generation=generation+1;
END;
INSERT INTO stat_dirty(run_id) SELECT id FROM runs WHERE true ON CONFLICT(run_id) DO UPDATE SET generation=generation+1;
CREATE TABLE IF NOT EXISTS stat_work (
 nonce TEXT PRIMARY KEY,run_id TEXT NOT NULL,generation INTEGER NOT NULL,
 revision INTEGER NOT NULL,policy TEXT NOT NULL,next TEXT,keys TEXT NOT NULL,expires INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS stat_work_expiry ON stat_work(expires);
CREATE TABLE IF NOT EXISTS stat_work_chunks (
 nonce TEXT NOT NULL REFERENCES stat_work(nonce) ON DELETE CASCADE,
 ordinal INTEGER NOT NULL,hash TEXT NOT NULL,payload TEXT NOT NULL,PRIMARY KEY(nonce,ordinal)
);
