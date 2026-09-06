const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {spawn} = require('node:child_process');
const command = process.argv[2] || 'tests';
const allowed = ['tests', 'build-all', 'build-web', 'build-windows', 'export-balance'];
if (!allowed.includes(command)) {
  console.error('Command must be one of: ' + allowed.join(', '));
  process.exit(1);
}
const root = path.resolve(__dirname, '..');
if (command === 'build-all') {
  const script = path.join(root, 'tools', 'build-all.ps1');
  const child = spawn('powershell.exe', ['-NoProfile', '-File', script], {
    cwd: root,
    stdio: 'inherit',
    shell: false,
    windowsHide: true
  });
  child.once('error', error => {
    console.error('Could not start the sequential Unity build: ' + error.message);
    process.exitCode = 1;
  });
  child.once('close', (code, signal) => {
    if (signal) console.error('Sequential Unity build stopped: ' + signal);
    process.exitCode = Number.isInteger(code) && code >= 0 ? code : 1;
  });
  return;
}
const work = path.join(root, 'work');
fs.mkdirSync(work, {recursive: true});
const id = crypto.randomUUID();
const temporary = path.join(work, 'unity-request.' + id + '.tmp');
fs.writeFileSync(temporary, JSON.stringify({id, command}));
fs.renameSync(temporary, path.join(work, 'unity-request.json'));
console.log('Unity: ' + command + '. Keep the project open in the Unity Editor.');
const started = Date.now();
const poll = setInterval(() => {
  let result;
  try { result = JSON.parse(fs.readFileSync(path.join(work, 'unity-result.json'), 'utf8')); }
  catch { /* Editor has not produced the result yet. */ }
  if (result && result.id === id && result.status !== 'running') {
    clearInterval(poll);
    console.log(JSON.stringify(result, null, 2));
    process.exitCode = result.status === 'succeeded' ? 0 : 1;
  } else if (Date.now() - started > 20 * 60 * 1000) {
    clearInterval(poll);
    console.error('Unity has not finished in 20 minutes. Check its Console and work/unity-result.json.');
    process.exitCode = 1;
  }
}, 1000);
