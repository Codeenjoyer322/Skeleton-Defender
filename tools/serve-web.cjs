const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../Builds/Web');
const port = Number(process.env.SKELETON_PORT || 8765);
const types = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.json':'application/json','.png':'image/png','.ico':'image/x-icon'};
if (!fs.existsSync(path.join(root, 'index.html'))) { console.error('Builds/Web is missing. Build Browser in Unity first.'); process.exit(1); }
http.createServer((req,res)=>{
  let decoded;
  try { decoded=decodeURIComponent(new URL(req.url, 'http://localhost').pathname); } catch { res.writeHead(400);res.end();return; }
  const file = path.resolve(root, '.' + (decoded === '/' ? '/index.html' : decoded));
  if (!file.startsWith(root + path.sep)) {res.writeHead(403);res.end();return;}
  fs.stat(file,(err,stat)=>{
    if(err || !stat.isFile()){res.writeHead(404);res.end('Not found');return;}
    res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':'no-cache','X-Content-Type-Options':'nosniff'});
    if(req.method==='HEAD'){res.end();return;}
    const stream=fs.createReadStream(file);stream.on('error',()=>res.destroy());stream.pipe(res);
  });
}).listen(port,'127.0.0.1',()=>console.log('Skeleton Defender: http://127.0.0.1:'+port+'\nKeep this window open while playing. Ctrl+C stops the server.'));
