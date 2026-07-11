// Formula Playground — injects interactive sliders below KaTeX formulas

export function initFormulaPlayground(formulas) {
  if (!formulas || formulas.length === 0) return;

  const content = document.getElementById('content');
  if (!content) return;

  formulas.forEach(entry => {
    if (!entry.formula || !entry.expression || entry.variables.length === 0) return;
    injectPlayground(content, entry);
  });
}

function injectPlayground(content, entry) {
  // Find KaTeX spans that contain the formula pattern (match by resultVar or first variable)
  const katexEls = content.querySelectorAll('.katex-display, .katex');
  let target = null;
  for (const el of katexEls) {
    const text = el.textContent || '';
    if (entry.variables.some(v => text.includes(v.name)) && text.includes(entry.resultVar)) {
      target = el.closest('.katex-display') || el;
      break;
    }
  }

  // If no KaTeX match, try to find the formula as plain text in a paragraph
  if (!target) {
    const paras = content.querySelectorAll('p, li');
    for (const p of paras) {
      if (p.textContent.includes(entry.resultVar) && entry.variables.some(v => p.textContent.includes(v.name))) {
        target = p;
        break;
      }
    }
  }

  if (!target) return;

  // Don't inject twice
  if (target.nextElementSibling?.classList?.contains('formula-playground')) return;

  const playground = document.createElement('div');
  playground.className = 'formula-playground';

  // Build state
  const state = {};
  entry.variables.forEach(v => { state[v.name] = v.defaultVal; });

  // Result display
  const resultRow = document.createElement('div');
  resultRow.className = 'fp-result';

  function updateResult() {
    try {
      const fn = new Function(...Object.keys(state), `return ${entry.expression};`);
      const val = fn(...Object.values(state));
      const formatted = Number.isFinite(val) ? (Number.isInteger(val) ? val : val.toFixed(3)) : '?';
      resultRow.innerHTML = `<span class="fp-result-var">${entry.resultVar}</span> = <strong>${formatted}</strong> <span class="fp-result-unit">${entry.resultUnit || ''}</span>`;
    } catch {
      resultRow.textContent = 'Chyba ve výpočtu.';
    }
  }

  // Build sliders
  const slidersWrap = document.createElement('div');
  slidersWrap.className = 'fp-sliders';

  entry.variables.forEach(v => {
    const row = document.createElement('div');
    row.className = 'fp-slider-row';

    const label = document.createElement('label');
    label.className = 'fp-label';
    label.textContent = `${v.label} (${v.unit})`;

    const sliderWrap = document.createElement('div');
    sliderWrap.className = 'fp-slider-wrap';

    const slider = document.createElement('input');
    slider.type = 'range';
    slider.className = 'fp-slider';
    slider.min = v.min;
    slider.max = v.max;
    slider.step = v.step || 1;
    slider.value = v.defaultVal;

    const valDisplay = document.createElement('span');
    valDisplay.className = 'fp-val';
    valDisplay.textContent = v.defaultVal;

    slider.addEventListener('input', () => {
      state[v.name] = parseFloat(slider.value);
      valDisplay.textContent = slider.value;
      updateResult();
    });

    sliderWrap.appendChild(slider);
    sliderWrap.appendChild(valDisplay);
    row.appendChild(label);
    row.appendChild(sliderWrap);
    slidersWrap.appendChild(row);
  });

  playground.appendChild(slidersWrap);
  playground.appendChild(resultRow);
  updateResult();

  target.after(playground);
}
