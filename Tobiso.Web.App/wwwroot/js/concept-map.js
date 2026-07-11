// Concept Map renderer using D3.js (already loaded globally via posts-graph.js context)

export function renderConceptMap(containerId, mapData) {
  const container = document.getElementById(containerId);
  if (!container || !mapData) return;

  container.innerHTML = '';

  const nodes = mapData.nodes || [];
  const edges = mapData.edges || [];
  if (nodes.length === 0) return;

  const W = container.clientWidth || 300;
  const H = Math.max(220, Math.min(320, nodes.length * 35));

  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('width', '100%');
  svg.setAttribute('height', H);
  svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
  container.appendChild(svg);

  // Arrow marker
  const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
  const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
  marker.setAttribute('id', `arrow-${containerId}`);
  marker.setAttribute('markerWidth', '8');
  marker.setAttribute('markerHeight', '8');
  marker.setAttribute('refX', '20');
  marker.setAttribute('refY', '3');
  marker.setAttribute('orient', 'auto');
  const poly = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
  poly.setAttribute('points', '0 0, 6 3, 0 6');
  poly.setAttribute('fill', '#4fc3f7');
  marker.appendChild(poly);
  defs.appendChild(marker);
  svg.appendChild(defs);

  const isDark = document.documentElement.dataset.theme === 'dark'
    || (!document.documentElement.dataset.theme && window.matchMedia('(prefers-color-scheme: dark)').matches);
  const nodeColor = isDark ? '#1e3a5f' : '#e3f2fd';
  const textColor = isDark ? '#e0f7fa' : '#0d1b2a';
  const edgeColor = '#4fc3f7';

  // Simple force-directed layout (manual, no D3 dependency)
  const nodeMap = {};
  nodes.forEach((n, i) => {
    const angle = (2 * Math.PI * i) / nodes.length;
    const r = Math.min(W, H) * 0.35;
    nodeMap[n.id] = {
      id: n.id,
      label: n.label,
      x: W / 2 + r * Math.cos(angle - Math.PI / 2),
      y: H / 2 + r * Math.sin(angle - Math.PI / 2)
    };
  });

  // Draw edges
  edges.forEach(e => {
    const src = nodeMap[e.source];
    const tgt = nodeMap[e.target];
    if (!src || !tgt) return;

    const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    line.setAttribute('x1', src.x);
    line.setAttribute('y1', src.y);
    line.setAttribute('x2', tgt.x);
    line.setAttribute('y2', tgt.y);
    line.setAttribute('stroke', edgeColor);
    line.setAttribute('stroke-width', '1.5');
    line.setAttribute('stroke-opacity', '0.6');
    line.setAttribute('marker-end', `url(#arrow-${containerId})`);
    svg.appendChild(line);

    if (e.label) {
      const mx = (src.x + tgt.x) / 2;
      const my = (src.y + tgt.y) / 2;
      const lt = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      lt.setAttribute('x', mx);
      lt.setAttribute('y', my - 3);
      lt.setAttribute('text-anchor', 'middle');
      lt.setAttribute('font-size', '9');
      lt.setAttribute('fill', edgeColor);
      lt.setAttribute('opacity', '0.8');
      lt.textContent = e.label;
      svg.appendChild(lt);
    }
  });

  // Draw nodes
  Object.values(nodeMap).forEach(n => {
    const labelLen = n.label.length;
    const rx = Math.max(30, Math.min(55, labelLen * 4.5));
    const ry = 14;

    const ellipse = document.createElementNS('http://www.w3.org/2000/svg', 'ellipse');
    ellipse.setAttribute('cx', n.x);
    ellipse.setAttribute('cy', n.y);
    ellipse.setAttribute('rx', rx);
    ellipse.setAttribute('ry', ry);
    ellipse.setAttribute('fill', nodeColor);
    ellipse.setAttribute('stroke', edgeColor);
    ellipse.setAttribute('stroke-width', '1.5');
    svg.appendChild(ellipse);

    const txt = document.createElementNS('http://www.w3.org/2000/svg', 'text');
    txt.setAttribute('x', n.x);
    txt.setAttribute('y', n.y + 4);
    txt.setAttribute('text-anchor', 'middle');
    txt.setAttribute('font-size', '11');
    txt.setAttribute('fill', textColor);
    txt.setAttribute('font-weight', '500');
    const label = n.label.length > 14 ? n.label.slice(0, 13) + '…' : n.label;
    txt.textContent = label;
    svg.appendChild(txt);
  });
}
