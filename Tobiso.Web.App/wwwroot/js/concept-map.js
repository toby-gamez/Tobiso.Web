// Concept Map renderer — custom lightweight force-directed layout, no external deps.
// Uses the Tobiso pink/mauve palette to match the rest of the site.

const SVG_NS = 'http://www.w3.org/2000/svg';

export function renderConceptMap(containerId, mapData) {
  const container = document.getElementById(containerId);
  if (!container || !mapData) return;

  container.innerHTML = '';

  const nodes = (mapData.nodes || []).map(n => ({ id: n.id, label: n.label || '' }));
  const edges = (mapData.edges || []).map(e => ({ source: e.source, target: e.target, label: e.label || '' }));
  if (nodes.length === 0) return;

  // Match the site's own theming (it toggles `body.dark-mode`), not prefers-color-scheme.
  const isDark = document.body.classList.contains('dark-mode')
    || document.documentElement.dataset.theme === 'dark';

  // --- Tobiso palette ---
  const palette = isDark
    ? {
        nodeTop: '#8a4066', nodeBottom: '#642a49',
        nodeStroke: '#d99cbe', text: '#f9ecf3',
        edge: '#e9dbe3', edgeLabel: '#f0d9e6',
        halo: '#241019', arrow: '#f2e4ec'
      }
    : {
        nodeTop: '#fdeef5', nodeBottom: '#f9dcea',
        nodeStroke: '#e0a8c6', text: '#5a2d44',
        edge: '#1a1a1a', edgeLabel: '#2a2a2a',
        halo: '#ffffff', arrow: '#111111'
      };

  const W = Math.max(280, container.clientWidth || 320);
  // Give the graph generous vertical room so nodes can spread out.
  const H = Math.round(Math.max(340, Math.min(560, 150 + nodes.length * 42)));

  const svg = document.createElementNS(SVG_NS, 'svg');
  svg.setAttribute('width', '100%');
  svg.setAttribute('height', H);
  svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
  svg.style.display = 'block';
  svg.style.maxWidth = '100%';
  container.appendChild(svg);

  // --- defs: node gradient, soft shadow, arrow marker ---
  const defs = document.createElementNS(SVG_NS, 'defs');

  const grad = document.createElementNS(SVG_NS, 'linearGradient');
  grad.setAttribute('id', `nodegrad-${containerId}`);
  grad.setAttribute('x1', '0'); grad.setAttribute('y1', '0');
  grad.setAttribute('x2', '0'); grad.setAttribute('y2', '1');
  [[0, palette.nodeTop], [1, palette.nodeBottom]].forEach(([o, c]) => {
    const s = document.createElementNS(SVG_NS, 'stop');
    s.setAttribute('offset', o); s.setAttribute('stop-color', c);
    grad.appendChild(s);
  });
  defs.appendChild(grad);

  const filter = document.createElementNS(SVG_NS, 'filter');
  filter.setAttribute('id', `nodeshadow-${containerId}`);
  filter.setAttribute('x', '-30%'); filter.setAttribute('y', '-30%');
  filter.setAttribute('width', '160%'); filter.setAttribute('height', '160%');
  const ds = document.createElementNS(SVG_NS, 'feDropShadow');
  ds.setAttribute('dx', '0'); ds.setAttribute('dy', '1.5');
  ds.setAttribute('stdDeviation', '2.5');
  ds.setAttribute('flood-color', isDark ? '#000000' : '#c36f9a');
  ds.setAttribute('flood-opacity', isDark ? '0.5' : '0.28');
  filter.appendChild(ds);
  defs.appendChild(filter);

  const marker = document.createElementNS(SVG_NS, 'marker');
  marker.setAttribute('id', `arrow-${containerId}`);
  marker.setAttribute('markerWidth', '14');
  marker.setAttribute('markerHeight', '14');
  marker.setAttribute('refX', '11');
  marker.setAttribute('refY', '5');
  marker.setAttribute('orient', 'auto');
  marker.setAttribute('markerUnits', 'userSpaceOnUse');
  const poly = document.createElementNS(SVG_NS, 'polygon');
  poly.setAttribute('points', '0 0, 11 5, 0 10');
  poly.setAttribute('fill', palette.arrow);
  marker.appendChild(poly);
  defs.appendChild(marker);

  svg.appendChild(defs);

  // --- node sizing ---
  nodes.forEach(n => {
    const clean = n.label.length > 20 ? n.label.slice(0, 19) + '…' : n.label;
    n.display = clean;
    n.w = Math.max(58, Math.min(150, clean.length * 7.1 + 26));
    n.h = 30;
  });

  const padX = 12, padTop = 14, padBottom = 14;
  const cx = W / 2, cy = H / 2;

  // --- initial placement on a circle ---
  const R0 = Math.min(W, H) * 0.36;
  nodes.forEach((n, i) => {
    const a = (2 * Math.PI * i) / nodes.length - Math.PI / 2;
    n.x = cx + R0 * Math.cos(a);
    n.y = cy + R0 * Math.sin(a);
  });

  // --- force-directed relaxation for pleasant, non-overlapping spacing ---
  const idealEdge = Math.min(W, H) * 0.32;
  const iterations = 320;
  for (let it = 0; it < iterations; it++) {
    const cool = 1 - it / iterations;

    // repulsion between every pair
    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) {
        const a = nodes[i], b = nodes[j];
        let dx = a.x - b.x, dy = a.y - b.y;
        let dist = Math.hypot(dx, dy) || 0.01;
        // stronger push when boxes are near-overlapping horizontally/vertically
        const minGap = (a.w + b.w) / 2 + 18;
        const force = (minGap * minGap) / dist * 0.06;
        dx /= dist; dy /= dist;
        a.x += dx * force * cool; a.y += dy * force * cool;
        b.x -= dx * force * cool; b.y -= dy * force * cool;
      }
    }

    // spring attraction along edges
    edges.forEach(e => {
      const a = nodes.find(n => n.id === e.source);
      const b = nodes.find(n => n.id === e.target);
      if (!a || !b) return;
      let dx = b.x - a.x, dy = b.y - a.y;
      let dist = Math.hypot(dx, dy) || 0.01;
      const force = (dist - idealEdge) * 0.05;
      dx /= dist; dy /= dist;
      a.x += dx * force * cool; a.y += dy * force * cool;
      b.x -= dx * force * cool; b.y -= dy * force * cool;
    });

    // gentle pull to center
    nodes.forEach(n => {
      n.x += (cx - n.x) * 0.012 * cool;
      n.y += (cy - n.y) * 0.012 * cool;
    });
  }

  // clamp inside the viewport
  nodes.forEach(n => {
    const hw = n.w / 2, hh = n.h / 2;
    n.x = Math.max(padX + hw, Math.min(W - padX - hw, n.x));
    n.y = Math.max(padTop + hh, Math.min(H - padBottom - hh, n.y));
  });

  // helper: point where the segment center->outside exits a node's box
  function boxExit(node, towardX, towardY) {
    const dx = towardX - node.x, dy = towardY - node.y;
    const dist = Math.hypot(dx, dy) || 0.01;
    const ux = dx / dist, uy = dy / dist;
    const hw = node.w / 2 + 2, hh = node.h / 2 + 2;
    const tx = Math.abs(ux) < 1e-6 ? Infinity : hw / Math.abs(ux);
    const ty = Math.abs(uy) < 1e-6 ? Infinity : hh / Math.abs(uy);
    const t = Math.min(tx, ty);
    return { x: node.x + ux * t, y: node.y + uy * t };
  }

  const edgeLayer = document.createElementNS(SVG_NS, 'g');
  const nodeLayer = document.createElementNS(SVG_NS, 'g');
  svg.appendChild(edgeLayer);
  svg.appendChild(nodeLayer);

  // --- draw edges as gentle curves with arrowheads + haloed labels ---
  edges.forEach(e => {
    const a = nodes.find(n => n.id === e.source);
    const b = nodes.find(n => n.id === e.target);
    if (!a || !b) return;

    const p1 = boxExit(a, b.x, b.y);
    const p2 = boxExit(b, a.x, a.y);

    const mx = (p1.x + p2.x) / 2, my = (p1.y + p2.y) / 2;
    // perpendicular offset gives a soft curve, reducing visual clutter
    const dx = p2.x - p1.x, dy = p2.y - p1.y;
    const len = Math.hypot(dx, dy) || 0.01;
    const off = Math.min(26, len * 0.14);
    const ctrlX = mx + (-dy / len) * off;
    const ctrlY = my + (dx / len) * off;

    const path = document.createElementNS(SVG_NS, 'path');
    path.setAttribute('d', `M ${p1.x.toFixed(1)} ${p1.y.toFixed(1)} Q ${ctrlX.toFixed(1)} ${ctrlY.toFixed(1)} ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`);
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke', palette.edge);
    path.setAttribute('stroke-width', '1.8');
    path.setAttribute('stroke-opacity', isDark ? '0.75' : '0.85');
    path.setAttribute('marker-end', `url(#arrow-${containerId})`);
    edgeLayer.appendChild(path);

    if (e.label) {
      // point on the quadratic curve at t=0.5
      const lx = 0.25 * p1.x + 0.5 * ctrlX + 0.25 * p2.x;
      const ly = 0.25 * p1.y + 0.5 * ctrlY + 0.25 * p2.y;
      const lt = document.createElementNS(SVG_NS, 'text');
      lt.setAttribute('x', lx.toFixed(1));
      lt.setAttribute('y', ly.toFixed(1));
      lt.setAttribute('text-anchor', 'middle');
      lt.setAttribute('dominant-baseline', 'middle');
      lt.setAttribute('font-size', '9.5');
      lt.setAttribute('font-family', 'system-ui, sans-serif');
      lt.setAttribute('fill', palette.edgeLabel);
      // white/dark halo so the label stays readable over edges
      lt.setAttribute('stroke', palette.halo);
      lt.setAttribute('stroke-width', '3');
      lt.setAttribute('paint-order', 'stroke');
      lt.setAttribute('stroke-linejoin', 'round');
      lt.textContent = e.label;
      edgeLayer.appendChild(lt);
    }
  });

  // --- draw nodes as rounded rects with gradient + shadow ---
  nodes.forEach(n => {
    const g = document.createElementNS(SVG_NS, 'g');
    g.setAttribute('filter', `url(#nodeshadow-${containerId})`);

    const rect = document.createElementNS(SVG_NS, 'rect');
    rect.setAttribute('x', (n.x - n.w / 2).toFixed(1));
    rect.setAttribute('y', (n.y - n.h / 2).toFixed(1));
    rect.setAttribute('width', n.w.toFixed(1));
    rect.setAttribute('height', n.h);
    rect.setAttribute('rx', '15');
    rect.setAttribute('ry', '15');
    rect.setAttribute('fill', `url(#nodegrad-${containerId})`);
    rect.setAttribute('stroke', palette.nodeStroke);
    rect.setAttribute('stroke-width', '1.5');
    g.appendChild(rect);

    const txt = document.createElementNS(SVG_NS, 'text');
    txt.setAttribute('x', n.x.toFixed(1));
    txt.setAttribute('y', (n.y + 0.5).toFixed(1));
    txt.setAttribute('text-anchor', 'middle');
    txt.setAttribute('dominant-baseline', 'middle');
    txt.setAttribute('font-size', '11.5');
    txt.setAttribute('font-family', 'system-ui, sans-serif');
    txt.setAttribute('font-weight', '600');
    txt.setAttribute('fill', palette.text);
    txt.textContent = n.display;
    g.appendChild(txt);

    nodeLayer.appendChild(g);
  });
}
