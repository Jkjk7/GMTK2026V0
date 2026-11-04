const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');

test('enemy atlas covers seven distinct 48px silhouettes with crisp alpha and safe margins', async () => {
 const file=path.join(root,'Assets/Art/Pixel/enemy-atlas.png');
 const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(info.width,336); assert.equal(info.height,48);
 const hashes=new Set();
 for(let n=0;n<7;n++) {
  let occupied=0; const bytes=[];
  for(let y=0;y<48;y++) for(let x=0;x<48;x++) {
   const p=(y*336+n*48+x)*4;
   assert.ok(data[p+3]===0||data[p+3]===255);
   if(x<2||x>45||y<2||y>45) assert.equal(data[p+3],0,'sprite touches tile edge');
   if(data[p+3])occupied++;
   bytes.push(...data.subarray(p,p+4));
  }
  assert.ok(occupied>350,'empty or excessively small silhouette');
  hashes.add(require('node:crypto').createHash('sha256').update(Buffer.from(bytes)).digest('hex'));
 }
 assert.equal(hashes.size,7);
});

test('embedded runtime enemy atlas exactly matches the reviewable PNG', () => {
 const code=fs.readFileSync(path.join(root,'Assets/Scripts/Core/PixelEnemyData.cs'),'utf8');
 const embedded=code.match(/Png = "([^"]+)"/)[1];
 assert.deepEqual(Buffer.from(embedded,'base64'),fs.readFileSync(path.join(root,'Assets/Art/Pixel/enemy-atlas.png')));
});
