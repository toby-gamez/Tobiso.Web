// Mind map renderer for the "Myšlenková mapa" AI tool - an OrgPad-style pan/zoom canvas:
// nodes start from a recursive local-orbit auto-layout (each node's children fan out in a
// ring around IT, not around one distant global center), but the whole thing lives in a
// fixed-size viewport the user can pan/zoom, drag individual nodes around freely, and click
// a node to zoom in on just that node's branch. None of this is persisted - every open
// starts from the same auto-layout again, so there is no server round-trip here.
//
// Colors are set via CSS custom properties (organic.css/variables.css tokens) rather than
// hardcoded hex, so the map follows the site's light/dark theme automatically, including a
// live theme toggle, without any prefers-color-scheme detection here.

const SVG_NS = 'http://www.w3.org/2000/svg';
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));

export function renderMindMap(containerId, mapData) {
  const container = document.getElementById(containerId);
  if (!container || !mapData) return;

  container.innerHTML = '';

  const nodes = (mapData.nodes || []).map(n => ({ id: n.id, label: n.label || '' }));
  const edges = (mapData.edges || []).map(e => ({ source: e.source, target: e.target, label: e.label || '' }))
    .filter(e => nodes.some(n => n.id === e.source) && nodes.some(n => n.id === e.target));
  if (nodes.length === 0) return;

  // --- OrgPad-style layout: each node's children orbit in a ring around THAT node, not
  // around one distant global center. Radius grows with each generation, but unevenly -
  // a branch with few, narrow children gets a tight local orbit while a branch with many
  // stays roomy - which is what gives this look its organic (rather than perfectly
  // concentric) feel. This is only the STARTING layout; the user can drag any node
  // anywhere afterward.
  //
  // Roots are nodes with no incoming edge (typically the central/general concept). A BFS
  // from all roots at once also records each node's first-discovery parent, giving a single
  // spanning tree to recurse over even when the AI's edges form a DAG (a node reachable from
  // two parents still gets exactly one "home" orbit) - every edge is still drawn later, this
  // spanning tree only decides positions.
  const outgoing = new Map(nodes.map(n => [n.id, []]));
  const incomingCount = new Map(nodes.map(n => [n.id, 0]));
  edges.forEach(e => {
    outgoing.get(e.source).push(e.target);
    incomingCount.set(e.target, incomingCount.get(e.target) + 1);
  });

  const parentOf = new Map();
  const queue = [];
  nodes.forEach(n => {
    if (incomingCount.get(n.id) === 0) queue.push(n.id);
  });
  if (queue.length === 0) queue.push(nodes[0].id);
  const visited = new Set(queue);
  for (let qi = 0; qi < queue.length; qi++) {
    const id = queue[qi];
    outgoing.get(id).forEach(childId => {
      if (!visited.has(childId)) {
        visited.add(childId);
        parentOf.set(childId, id);
        queue.push(childId);
      }
    });
  }
  // Disconnected islands unreached from any root become extra roots of their own.
  const roots = nodes.filter(n => !parentOf.has(n.id));

  const treeChildren = new Map(nodes.map(n => [n.id, []]));
  nodes.forEach(n => { if (parentOf.has(n.id)) treeChildren.get(parentOf.get(n.id)).push(n); });

  nodes.forEach(n => {
    const clean = n.label.length > 24 ? n.label.slice(0, 23) + '…' : n.label;
    n.display = clean;
    n.w = Math.max(64, Math.min(180, clean.length * 7.2 + 34));
    n.h = 34;
  });

  const MIN_RADIUS = 100, EDGE_GAP = 26;

  // Places `items` on a ring around (cx,cy), spanning `items.length` slots evenly spaced
  // around a full circle, centered on `aroundAngle` so a branch fans outward continuing the
  // direction it arrived from rather than every ring restarting at a fixed compass heading.
  function placeRing(cx, cy, items, aroundAngle, ownerNode) {
    const k = items.length;
    if (k === 0) return;
    let radius = MIN_RADIUS;
    if (ownerNode) radius = Math.max(radius, ownerNode.w / 2 + Math.max(...items.map(c => c.w)) / 2 + 40);
    const angleStep = (2 * Math.PI) / k;
    if (k > 1) {
      for (let i = 0; i < k; i++) {
        const a = items[i], b = items[(i + 1) % k];
        radius = Math.max(radius, ((a.w + b.w) / 2 + EDGE_GAP) / angleStep);
      }
    }
    const startAngle = aroundAngle - (angleStep * (k - 1)) / 2;
    items.forEach((child, i) => {
      const angle = startAngle + i * angleStep;
      child.x = cx + radius * Math.cos(angle);
      child.y = cy + radius * Math.sin(angle);
      placeRing(child.x, child.y, treeChildren.get(child.id), angle, child);
    });
  }

  if (roots.length === 1) {
    const root = roots[0];
    root.x = 0; root.y = 0;
    placeRing(0, 0, treeChildren.get(root.id), -Math.PI / 2, root);
  } else {
    placeRing(0, 0, roots, -Math.PI / 2, null);
  }

  // The recursive orbit above can still let two unrelated branches' descendants drift into
  // each other (it only keeps a node clear of its own immediate siblings), so finish with a
  // deterministic pairwise separation pass - guarantees zero overlap regardless of shape.
  for (let iter = 0; iter < 300; iter++) {
    let any = false;
    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) {
        const a = nodes[i], b = nodes[j];
        const dx = b.x - a.x, dy = b.y - a.y;
        const overlapX = (a.w + b.w) / 2 + 14 - Math.abs(dx);
        const overlapY = (a.h + b.h) / 2 + 14 - Math.abs(dy);
        if (overlapX > 0 && overlapY > 0) {
          any = true;
          if (overlapX < overlapY) {
            const push = (overlapX / 2) * (dx === 0 ? (i < j ? 1 : -1) : Math.sign(dx));
            a.x -= push; b.x += push;
          } else {
            const push = (overlapY / 2) * (dy === 0 ? 1 : Math.sign(dy));
            a.y -= push; b.y += push;
          }
        }
      }
    }
    if (!any) break;
  }

  const padX = 30, padTop = 30, padBottom = 30;
  const minX = Math.min(...nodes.map(n => n.x - n.w / 2));
  const maxX = Math.max(...nodes.map(n => n.x + n.w / 2));
  const minY = Math.min(...nodes.map(n => n.y - n.h / 2));
  const maxY = Math.max(...nodes.map(n => n.y + n.h / 2));
  const shiftX = padX - minX, shiftY = padTop - minY;
  nodes.forEach(n => { n.x += shiftX; n.y += shiftY; });
  const contentW = (maxX - minX) + padX * 2;
  const contentH = (maxY - minY) + padTop + padBottom;

  // --- fixed-size viewport the user pans/zooms within (CSS controls its actual px size) ---
  const viewportW = Math.max(280, container.clientWidth || 320);
  const viewportH = Math.max(220, container.clientHeight || 440);
  const kMin = 0.2, kMax = 3;

  const fitPad = 30;
  const k0 = clamp(Math.min((viewportW - fitPad * 2) / contentW, (viewportH - fitPad * 2) / contentH), kMin, 1.3);
  const tx0 = viewportW / 2 - (contentW / 2) * k0;
  const ty0 = viewportH / 2 - (contentH / 2) * k0;
  let view = { k: k0, tx: tx0, ty: ty0 };

  const svg = document.createElementNS(SVG_NS, 'svg');
  svg.setAttribute('width', viewportW);
  svg.setAttribute('height', viewportH);
  svg.setAttribute('viewBox', `0 0 ${viewportW} ${viewportH}`);
  svg.style.display = 'block';
  svg.style.touchAction = 'none';
  svg.style.cursor = 'grab';
  container.appendChild(svg);

  // Transparent (not "none") fill so this rect participates in hit-testing across the whole
  // viewport, giving the canvas-pan gesture something to grab even over empty space.
  const bg = document.createElementNS(SVG_NS, 'rect');
  bg.setAttribute('x', '0'); bg.setAttribute('y', '0');
  bg.setAttribute('width', viewportW); bg.setAttribute('height', viewportH);
  bg.setAttribute('fill', 'transparent');
  svg.appendChild(bg);

  // --- defs: node gradient, soft shadow, arrow marker ---
  const defs = document.createElementNS(SVG_NS, 'defs');

  const grad = document.createElementNS(SVG_NS, 'linearGradient');
  grad.setAttribute('id', `nodegrad-${containerId}`);
  grad.setAttribute('x1', '0'); grad.setAttribute('y1', '0');
  grad.setAttribute('x2', '0'); grad.setAttribute('y2', '1');
  [[0, '--color-accent-200'], [1, '--color-accent-300']].forEach(([o, token]) => {
    const s = document.createElementNS(SVG_NS, 'stop');
    s.setAttribute('offset', o);
    s.style.stopColor = `var(${token})`;
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
  ds.setAttribute('flood-color', '#201e1d');
  ds.setAttribute('flood-opacity', '0.22');
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
  poly.style.fill = 'var(--color-accent-500)';
  marker.appendChild(poly);
  defs.appendChild(marker);

  svg.appendChild(defs);

  // Everything pannable/zoomable lives in this group. transform-origin is pinned to 0 0 so
  // the CSS `transform` behaves exactly like the classic SVG translate/scale attribute math
  // this module's view-fitting arithmetic assumes.
  const viewG = document.createElementNS(SVG_NS, 'g');
  viewG.style.transformOrigin = '0 0';
  svg.appendChild(viewG);

  function applyTransform() {
    viewG.style.transform = `translate(${view.tx}px, ${view.ty}px) scale(${view.k})`;
  }
  function animateTo(target) {
    viewG.style.transition = 'transform 380ms ease';
    view = target;
    applyTransform();
    setTimeout(() => { viewG.style.transition = ''; }, 400);
  }
  applyTransform();

  function subtreeIds(rootId) {
    const seen = new Set([rootId]);
    const stack = [rootId];
    while (stack.length) {
      const id = stack.pop();
      (outgoing.get(id) || []).forEach(childId => {
        if (!seen.has(childId)) { seen.add(childId); stack.push(childId); }
      });
    }
    return seen;
  }

  function focusOnNode(node) {
    const ids = subtreeIds(node.id);
    const sub = nodes.filter(n => ids.has(n.id));
    const minX = Math.min(...sub.map(n => n.x - n.w / 2));
    const maxX = Math.max(...sub.map(n => n.x + n.w / 2));
    const minY = Math.min(...sub.map(n => n.y - n.h / 2));
    const maxY = Math.max(...sub.map(n => n.y + n.h / 2));
    const bw = Math.max(60, maxX - minX), bh = Math.max(40, maxY - minY);
    const pad = 50;
    const k = clamp(Math.min((viewportW - pad * 2) / bw, (viewportH - pad * 2) / bh), kMin, kMax);
    const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
    animateTo({ k, tx: viewportW / 2 - cx * k, ty: viewportH / 2 - cy * k });
  }

  function resetView() { animateTo({ k: k0, tx: tx0, ty: ty0 }); }

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
  viewG.appendChild(edgeLayer);
  viewG.appendChild(nodeLayer);

  // --- draw edges as gentle curves with arrowheads + haloed labels ---
  const edgeRenders = [];
  edges.forEach(e => {
    const a = nodes.find(n => n.id === e.source);
    const b = nodes.find(n => n.id === e.target);
    if (!a || !b) return;

    const path = document.createElementNS(SVG_NS, 'path');
    path.setAttribute('fill', 'none');
    path.style.stroke = 'var(--color-accent-2-500)';
    path.setAttribute('stroke-width', '1.8');
    path.setAttribute('stroke-opacity', '0.8');
    path.setAttribute('marker-end', `url(#arrow-${containerId})`);
    edgeLayer.appendChild(path);

    let label = null;
    if (e.label) {
      label = document.createElementNS(SVG_NS, 'text');
      label.setAttribute('text-anchor', 'middle');
      label.setAttribute('dominant-baseline', 'middle');
      label.setAttribute('font-size', '9.5');
      label.style.fontFamily = 'var(--font-body)';
      label.style.fill = 'var(--color-neutral-700)';
      label.style.stroke = 'var(--color-surface)';
      label.setAttribute('stroke-width', '3');
      label.setAttribute('paint-order', 'stroke');
      label.setAttribute('stroke-linejoin', 'round');
      label.textContent = e.label;
      edgeLayer.appendChild(label);
    }

    edgeRenders.push({ a, b, path, label });
  });

  function updateEdge(er) {
    const p1 = boxExit(er.a, er.b.x, er.b.y);
    const p2 = boxExit(er.b, er.a.x, er.a.y);
    const mx = (p1.x + p2.x) / 2, my = (p1.y + p2.y) / 2;
    const dx = p2.x - p1.x, dy = p2.y - p1.y;
    const len = Math.hypot(dx, dy) || 0.01;
    const off = Math.min(26, len * 0.14);
    const ctrlX = mx + (-dy / len) * off;
    const ctrlY = my + (dx / len) * off;
    er.path.setAttribute('d', `M ${p1.x.toFixed(1)} ${p1.y.toFixed(1)} Q ${ctrlX.toFixed(1)} ${ctrlY.toFixed(1)} ${p2.x.toFixed(1)} ${p2.y.toFixed(1)}`);
    if (er.label) {
      er.label.setAttribute('x', (0.25 * p1.x + 0.5 * ctrlX + 0.25 * p2.x).toFixed(1));
      er.label.setAttribute('y', (0.25 * p1.y + 0.5 * ctrlY + 0.25 * p2.y).toFixed(1));
    }
  }
  edgeRenders.forEach(updateEdge);

  // --- draw nodes as rounded rects with gradient + shadow; each is draggable and click-to-focus ---
  const CLICK_MAX_MOVE = 6, CLICK_MAX_MS = 500;

  nodes.forEach(n => {
    const g = document.createElementNS(SVG_NS, 'g');
    g.setAttribute('filter', `url(#nodeshadow-${containerId})`);
    g.style.cursor = 'grab';

    const rect = document.createElementNS(SVG_NS, 'rect');
    rect.setAttribute('width', n.w.toFixed(1));
    rect.setAttribute('height', n.h);
    rect.setAttribute('rx', '14');
    rect.setAttribute('ry', '14');
    rect.setAttribute('fill', `url(#nodegrad-${containerId})`);
    rect.style.stroke = 'var(--color-accent-500)';
    rect.setAttribute('stroke-width', '1.5');
    g.appendChild(rect);

    const txt = document.createElementNS(SVG_NS, 'text');
    txt.setAttribute('text-anchor', 'middle');
    txt.setAttribute('dominant-baseline', 'middle');
    txt.setAttribute('font-size', '11.5');
    txt.style.fontFamily = 'var(--font-heading)';
    txt.setAttribute('font-weight', '600');
    txt.style.fill = 'var(--color-accent-900)';
    txt.textContent = n.display;
    g.appendChild(txt);

    function updateNodeVisual() {
      rect.setAttribute('x', (n.x - n.w / 2).toFixed(1));
      rect.setAttribute('y', (n.y - n.h / 2).toFixed(1));
      txt.setAttribute('x', n.x.toFixed(1));
      txt.setAttribute('y', (n.y + 0.5).toFixed(1));
    }
    updateNodeVisual();

    const myEdges = edgeRenders.filter(er => er.a === n || er.b === n);

    let drag = null;
    g.addEventListener('pointerdown', (e) => {
      e.stopPropagation();
      g.setPointerCapture(e.pointerId);
      drag = { startX: e.clientX, startY: e.clientY, startNodeX: n.x, startNodeY: n.y, startT: Date.now(), moved: false };
      g.style.cursor = 'grabbing';
    });
    g.addEventListener('pointermove', (e) => {
      if (!drag) return;
      const dx = (e.clientX - drag.startX) / view.k;
      const dy = (e.clientY - drag.startY) / view.k;
      if (Math.abs(e.clientX - drag.startX) > CLICK_MAX_MOVE || Math.abs(e.clientY - drag.startY) > CLICK_MAX_MOVE) drag.moved = true;
      n.x = drag.startNodeX + dx;
      n.y = drag.startNodeY + dy;
      updateNodeVisual();
      myEdges.forEach(updateEdge);
    });
    function endDrag(e) {
      if (!drag) return;
      const wasClick = !drag.moved && (Date.now() - drag.startT) < CLICK_MAX_MS;
      drag = null;
      g.style.cursor = 'grab';
      if (wasClick) focusOnNode(n);
    }
    g.addEventListener('pointerup', endDrag);
    g.addEventListener('pointercancel', endDrag);

    nodeLayer.appendChild(g);
  });

  // --- canvas-level pan (background drag), wheel zoom, and pinch-to-zoom ---
  let pan = null;
  const activePointers = new Map();
  let pinch = null;

  svg.addEventListener('pointerdown', (e) => {
    activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });
    svg.setPointerCapture(e.pointerId);

    if (activePointers.size === 2) {
      pan = null;
      const pts = [...activePointers.values()];
      pinch = { lastDist: Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y) };
      return;
    }
    if (activePointers.size === 1) {
      pan = { startX: e.clientX, startY: e.clientY, startTx: view.tx, startTy: view.ty, moved: false };
      svg.style.cursor = 'grabbing';
    }
  });

  svg.addEventListener('pointermove', (e) => {
    if (!activePointers.has(e.pointerId)) return;
    activePointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

    if (pinch && activePointers.size === 2) {
      const pts = [...activePointers.values()];
      const rect = container.getBoundingClientRect();
      const midX = (pts[0].x + pts[1].x) / 2 - rect.left;
      const midY = (pts[0].y + pts[1].y) / 2 - rect.top;
      const dist = Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y) || 0.01;
      const ratio = dist / pinch.lastDist;
      pinch.lastDist = dist;
      zoomAt(midX, midY, ratio);
      return;
    }
    if (pan) {
      const dx = e.clientX - pan.startX, dy = e.clientY - pan.startY;
      if (Math.abs(dx) > CLICK_MAX_MOVE || Math.abs(dy) > CLICK_MAX_MOVE) pan.moved = true;
      view.tx = pan.startTx + dx;
      view.ty = pan.startTy + dy;
      applyTransform();
    }
  });

  function endPointer(e) {
    activePointers.delete(e.pointerId);
    if (activePointers.size < 2) pinch = null;
    if (activePointers.size === 0) {
      const wasBackgroundClick = pan && !pan.moved;
      pan = null;
      svg.style.cursor = 'grab';
      if (wasBackgroundClick) resetView();
    }
  }
  svg.addEventListener('pointerup', endPointer);
  svg.addEventListener('pointercancel', endPointer);

  function zoomAt(px, py, factor) {
    const newK = clamp(view.k * factor, kMin, kMax);
    const actualFactor = newK / view.k;
    view.tx = px - (px - view.tx) * actualFactor;
    view.ty = py - (py - view.ty) * actualFactor;
    view.k = newK;
    applyTransform();
  }

  svg.addEventListener('wheel', (e) => {
    e.preventDefault();
    const rect = container.getBoundingClientRect();
    zoomAt(e.clientX - rect.left, e.clientY - rect.top, Math.exp(-e.deltaY * 0.0015));
  }, { passive: false });

  // --- zoom in/out/fit overlay controls ---
  const controls = document.createElement('div');
  controls.className = 'mindmap-controls';
  const zoomInBtn = document.createElement('button');
  zoomInBtn.type = 'button'; zoomInBtn.textContent = '+'; zoomInBtn.setAttribute('aria-label', 'Přiblížit');
  zoomInBtn.addEventListener('click', () => animateTo({ ...view, k: clamp(view.k * 1.3, kMin, kMax) }));
  const zoomOutBtn = document.createElement('button');
  zoomOutBtn.type = 'button'; zoomOutBtn.textContent = '–'; zoomOutBtn.setAttribute('aria-label', 'Oddálit');
  zoomOutBtn.addEventListener('click', () => animateTo({ ...view, k: clamp(view.k / 1.3, kMin, kMax) }));
  const fitBtn = document.createElement('button');
  fitBtn.type = 'button'; fitBtn.className = 'mindmap-fit'; fitBtn.textContent = '⬚'; fitBtn.setAttribute('aria-label', 'Celá mapa');
  fitBtn.addEventListener('click', resetView);
  controls.appendChild(zoomInBtn);
  controls.appendChild(zoomOutBtn);
  controls.appendChild(fitBtn);
  container.appendChild(controls);
}
