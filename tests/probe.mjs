// The device probe (probe/index.html) in Chromium: it must find every feature this engine has, and it must say so when one is missing.
//   node tests/probe.mjs
import http from 'node:http';
import { readFile } from 'node:fs/promises';
import { execSync } from 'node:child_process';
import { join, extname, normalize } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

// `playwright` from node_modules, else from the global install
const { chromium } = await import('playwright').catch(() =>
  import(pathToFileURL(join(execSync('npm root -g').toString().trim(), 'playwright', 'index.mjs')).href));

const root = fileURLToPath(new URL('..', import.meta.url));
const types = { '.html': 'text/html', '.js': 'text/javascript', '.wat': 'text/plain' };
const server = http.createServer(async (req, res) => {
  try {
    let path = normalize(join(root, decodeURIComponent(new URL(req.url, 'http://x').pathname)));
    if (!path.startsWith(root)) throw new Error('outside');
    if (extname(path) === '') path = join(path, 'index.html');
    res.writeHead(200, { 'content-type': types[extname(path)] ?? 'application/octet-stream' });
    res.end(await readFile(path));
  } catch { res.writeHead(404); res.end('not found'); }
}).listen(0);
await new Promise((r) => server.once('listening', r));
const url = `http://127.0.0.1:${server.address().port}/probe/`;

let failed = 0;
const check = (name, ok, extra = '') => { console.log(`${ok ? '✓' : '✗'} ${name}${ok ? '' : '  ' + extra}`); if (!ok) failed++; };

const run = async (initScript) => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  if (initScript) await page.addInitScript(initScript);
  await page.goto(url);
  await page.waitForSelector('body[data-done="1"]', { timeout: 15000 });
  const report = await page.evaluate(() => window.__report);
  const verdict = await page.textContent('#verdict');
  await browser.close();
  return { report, verdict, errors };
};

// 1. an engine that has everything the modules need
{
  const { report, verdict, errors } = await run();
  const engine = Object.entries(report.engine);
  check('the engine checks cover GC, tail calls, exceptions and js-string', engine.length === 4, JSON.stringify(Object.keys(report.engine)));
  check('every engine feature of this Chromium is found', engine.every(([, r]) => r.ok), JSON.stringify(report.engine));
  check('the verdict says the device can run the modules', report.runsFsharpWasm === true && verdict.startsWith('✓'), verdict);
  check('the report has the device and the web APIs', report.probe === 1 && report.device.userAgent.includes('Chrome') && Object.keys(report.webApis).length >= 20);
  check('module workers and canvas are there', report.webApis['Module workers'].ok && report.webApis['Canvas 2D'].ok, JSON.stringify(report.webApis['Module workers']));
  check('no page errors', errors.length === 0, errors.join('; '));
}

// 2. an engine without the string builtins: the probe must not claim it can run the modules
{
  const { report, verdict } = await run(() => {
    const compile = WebAssembly.compile.bind(WebAssembly);
    WebAssembly.compile = (bytes, options) => options?.builtins ? Promise.reject(new TypeError('builtins are not supported')) : compile(bytes, options);
  });
  check('a missing feature is reported as missing', report.engine['JS string builtins (wasm:js-string)'].ok === false && report.engine['WebAssembly GC (struct, array, ref.cast)'].ok === true, JSON.stringify(report.engine));
  check('and the verdict says so', report.runsFsharpWasm === false && verdict.startsWith('✗') && verdict.includes('1 of 4'), verdict);
}

server.close();
process.exit(failed ? 1 : 0);
