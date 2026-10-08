import migration from '../migration-0003.json' with {type:'json'};
import {metered,account} from './usage.mjs';
/** Only the fixed, reviewed additive migration is exposed to the trusted publisher. */
export async function initializePublication(database){
 const {db,usage}=metered(database);
 const applied=await db.prepare("SELECT name FROM d1_migrations WHERE name='0003_incremental.sql'").first();
 if(applied){await account(database,usage);return {state:'ready',usage};}
 const seedGuard="WHERE NOT EXISTS(SELECT 1 FROM d1_migrations WHERE name='0003_incremental.sql')";
 const statements=migration.map(sql=>db.prepare(sql.startsWith('INSERT INTO stat_dirty(run_id) SELECT id FROM runs')?sql.replace('WHERE true',seedGuard):sql));
 statements.push(db.prepare("INSERT OR IGNORE INTO d1_migrations(name) VALUES('0003_incremental.sql')"));
 await db.batch(statements);await account(database,usage);return {state:'initialized',usage};
}
