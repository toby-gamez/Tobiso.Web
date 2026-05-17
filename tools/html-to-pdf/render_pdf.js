// Usage: node render_pdf.js input.html output.pdf
const fs = require('fs');
const puppeteer = require('puppeteer');

async function render(inputPath, outputPath){
  const browser = await puppeteer.launch({args:['--no-sandbox','--disable-setuid-sandbox']});
  const page = await browser.newPage();
  const html = fs.readFileSync(inputPath, 'utf8');

  // Ensure KaTeX resources are available and auto-render is invoked
  const wrapped = `<!doctype html><html><head><meta charset="utf-8">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css">
    <style>body{font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial;}</style>
  </head><body>${html}
  <script src="https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js"></script>
  <script src="https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/contrib/auto-render.min.js"></script>
  <script>
    (function(){
      try{ renderMathInElement(document.body, {delimiters: [{left:'$$', right:'$$', display:true},{left:'$', right:'$', display:false}]}); }catch(e){}
      window.__katexRendered = true;
    })();
  </script>
  </body></html>`;

  await page.setContent(wrapped, {waitUntil:'networkidle0'});
  // allow KaTeX to render
  await page.waitForTimeout(250);
  await page.pdf({path: outputPath, format: 'A4', printBackground: true});
  await browser.close();
}

if (process.argv.length < 4){
  console.error('Usage: node render_pdf.js input.html output.pdf');
  process.exit(2);
}

const inPath = process.argv[2];
const outPath = process.argv[3];

render(inPath, outPath).catch(e => { console.error(e); process.exit(1); });
