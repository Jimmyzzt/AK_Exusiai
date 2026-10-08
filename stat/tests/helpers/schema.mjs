import {readFile,readdir} from 'node:fs/promises';
export async function applySchema(db){
 const sql=(await Promise.all((await readdir('worker/migrations')).sort().map(name=>readFile('worker/migrations/'+name,'utf8')))).join('\n').replace(/--[^\n]*/g,'');
 const statements=sql.match(/\s*CREATE TRIGGER[\s\S]*?END;|[^;]+;/g)||[];
 for(const statement of statements)await db.prepare(statement.trim()).run();
}
