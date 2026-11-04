const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
async function main() {
 const root = path.resolve(__dirname, '../..');
 const source = path.join(__dirname, 'Source/pixel-enemies.png');
 const {data,info} = await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 console.log(info);
 const boundaries=[0,232,449,686,927,1192,1437,info.width];
 const names=['Swarm','Normal','Tank','Elite','Boss','Disassembler','ShieldCaster'];
 const tiles=[];
 for(let i=0;i<7;i++) {
  let x0=info.width,y0=info.height,x1=0,y1=0;
  for(let y=0;y<info.height;y++) for(let x=boundaries[i];x<boundaries[i+1];x++) {
   if(data[(y*info.width+x)*4+3]>100){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  }
  console.log(names[i],{x0,y0,x1,y1});
  // Import processing: trim each authored sprite and resample to the game's 48px grid.
  const buf=await sharp(source).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1})
    .resize(42,42,{fit:'inside',kernel:'nearest'}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  for(let j=0;j<buf.data.length;j+=4)buf.data[j+3]=buf.data[j+3]>=128?255:0;
  const png=await sharp(buf.data,{raw:buf.info}).png().toBuffer();
  tiles.push({input:png,left:i*48+Math.floor((48-buf.info.width)/2),top:Math.floor((48-buf.info.height)/2)});
 }
 const atlas=await sharp({create:{width:336,height:48,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(tiles).png().toBuffer();
 const dir=path.join(root,'Assets/Art/Pixel');fs.mkdirSync(dir,{recursive:true});
 fs.writeFileSync(path.join(dir,'enemy-atlas.png'),atlas);
 fs.mkdirSync(path.join(root,'Tools/Art/Source'),{recursive:true});

 fs.writeFileSync(path.join(root,'Assets/Scripts/Core/PixelEnemyData.cs'),'// Generated from Assets/Art/Pixel/enemy-atlas.png; shared embedded atlas avoids runtime file I/O.\ninternal static class PixelEnemyData\n{\n    internal const string Png = "'+atlas.toString('base64')+'";\n}\n');
 if (process.argv[2]) await sharp(atlas).resize(1680,240,{kernel:'nearest'}).flatten({background:'#101725'}).png().toFile(process.argv[2]);
}
main().catch(e=>{console.error(e);process.exit(1);});
