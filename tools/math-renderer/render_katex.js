// Minimal renderer: read LaTeX from stdin and output PNG bytes to stdout
// Usage: node render_katex.js "\\frac{1}{2}" > out.png

const fs = require('fs');
const path = require('path');
const puppeteer = require('puppeteer');

async function render(latex) {
  const browser = await puppeteer.launch({args: ['--no-sandbox','--disable-setuid-sandbox']});
  const page = await browser.newPage();
  const html = `<!doctype html><html><head><meta charset="utf-8"><link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css"></head><body style="margin:0; padding:10px; background:transparent;">
  <div id="math">${latex}</div>
  <script src="https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js"></script>
  <script>try{katex.render(String.raw`$${'${LATEX}'}$`, document.getElementById('math'), {throwOnError:false, displayMode:false});}catch(e){console.error(e)}</script>
  </body></html>`;

  // replace placeholder
  const final = html.replace('${LATEX}', latex.replace(/`/g, '\\`'));
  await page.setContent(final, {waitUntil: 'networkidle0'});
  const el = await page.$('#math');
  const buffer = await el.screenshot({omitBackground:true});
  await browser.close();
  return buffer;
}

async function main(){
  const latex = process.argv.slice(2).join(' ');
  if(!latex){
    console.error('Usage: node render_katex.js "\\frac{1}{2}"');
    process.exit(2);
  }
  try{
    const buf = await render(latex);
    process.stdout.write(buf);
  }catch(e){
    console.error(e);
    process.exit(1);
  }
}

main();
