// posts-graph.js - D3.js force simulation for posts network
// Expects D3.js v7 loaded from CDN

let simulation = null;
let svg = null;
let showCategoryLinks = false;
let graphData = null;

/**
 * Initialize the posts graph with D3 force simulation
 * @param {string} containerId - ID of the container div
 * @param {object} data - Graph data { nodes: [{id, title, categoryId, categoryName}], links: [{source, target, type}] }
 */
export function initGraph(containerId, data) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error(`Container ${containerId} not found`);
        return;
    }

    // Store data for toggle functionality
    graphData = data;

    // Clear any existing content
    container.innerHTML = '';

    // Get container dimensions
    const width = container.clientWidth;
    const height = container.clientHeight;

    // Create SVG
    svg = d3.select(`#${containerId}`)
        .append('svg')
        .attr('width', '100%')
        .attr('height', '100%')
        .attr('viewBox', [0, 0, width, height])
        .attr('style', 'background-color: #1a1a1a;');

    // Create group for zoom
    const g = svg.append('g');

    // Add zoom behavior
    const zoom = d3.zoom()
        .scaleExtent([0.1, 4])
        .on('zoom', (event) => {
            g.attr('transform', event.transform);
        });
    
    svg.call(zoom);

    // Filter links based on showCategoryLinks
    const visibleLinks = data.links.filter(l => 
        l.type === 'related' || (l.type === 'category' && showCategoryLinks)
    );

    // Create color scale for categories
    const categories = [...new Set(data.nodes.map(n => n.categoryName))];
    const colorScale = d3.scaleOrdinal()
        .domain(categories)
        .range(d3.schemeTableau10);

    // Calculate node degrees (number of connections)
    const nodeDegrees = new Map();
    data.nodes.forEach(n => nodeDegrees.set(n.id, 0));
    visibleLinks.forEach(l => {
        nodeDegrees.set(l.source.id || l.source, (nodeDegrees.get(l.source.id || l.source) || 0) + 1);
        nodeDegrees.set(l.target.id || l.target, (nodeDegrees.get(l.target.id || l.target) || 0) + 1);
    });

    // Create force simulation
    simulation = d3.forceSimulation(data.nodes)
        .force('link', d3.forceLink(visibleLinks)
            .id(d => d.id)
            .distance(l => l.type === 'related' ? 80 : 120))
        .force('charge', d3.forceManyBody().strength(-200))
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('collision', d3.forceCollide().radius(d => Math.sqrt(nodeDegrees.get(d.id) || 1) * 6 + 5));

    // Create links
    const link = g.append('g')
        .attr('class', 'links')
        .selectAll('line')
        .data(visibleLinks)
        .join('line')
        .attr('stroke', l => l.type === 'related' ? '#999' : '#555')
        .attr('stroke-opacity', l => l.type === 'related' ? 0.6 : 0.2)
        .attr('stroke-width', l => l.type === 'related' ? 2 : 1)
        .attr('stroke-dasharray', l => l.type === 'related' ? null : '4,4');

    // Create tooltip div
    let tooltip = d3.select('body').select('.posts-graph-tooltip');
    if (tooltip.empty()) {
        tooltip = d3.select('body').append('div')
            .attr('class', 'posts-graph-tooltip')
            .style('position', 'absolute')
            .style('visibility', 'hidden')
            .style('background-color', 'rgba(0, 0, 0, 0.9)')
            .style('color', 'white')
            .style('padding', '8px 12px')
            .style('border-radius', '4px')
            .style('font-size', '14px')
            .style('pointer-events', 'none')
            .style('z-index', 10000);
    }

    // Create popup div for click
    let popup = d3.select('body').select('.posts-graph-popup');
    if (popup.empty()) {
        popup = d3.select('body').append('div')
            .attr('class', 'posts-graph-popup')
            .style('position', 'absolute')
            .style('visibility', 'hidden')
            .style('background-color', 'rgba(0, 0, 0, 0.95)')
            .style('color', 'white')
            .style('padding', '16px')
            .style('border-radius', '8px')
            .style('font-size', '16px')
            .style('z-index', 10001)
            .style('box-shadow', '0 4px 12px rgba(0,0,0,0.5)');
    }

    // Create nodes
    const node = g.append('g')
        .attr('class', 'nodes')
        .selectAll('circle')
        .data(data.nodes)
        .join('circle')
        .attr('r', d => Math.sqrt(nodeDegrees.get(d.id) || 1) * 6 + 5)
        .attr('fill', d => colorScale(d.categoryName))
        .attr('stroke', '#fff')
        .attr('stroke-width', 1.5)
        .style('cursor', 'pointer')
        .call(d3.drag()
            .on('start', dragstarted)
            .on('drag', dragged)
            .on('end', dragended))
        .on('mouseover', function(event, d) {
            tooltip
                .style('visibility', 'visible')
                .html(`<strong>${d.title}</strong><br/><small>${d.categoryName || 'Bez kategorie'}</small>`);
        })
        .on('mousemove', function(event) {
            tooltip
                .style('top', (event.pageY - 40) + 'px')
                .style('left', (event.pageX + 10) + 'px');
        })
        .on('mouseout', function() {
            tooltip.style('visibility', 'hidden');
        })
        .on('click', function(event, d) {
            event.stopPropagation();
            
            // Hide tooltip when showing popup
            tooltip.style('visibility', 'hidden');
            
            popup
                .style('visibility', 'visible')
                .style('top', (event.pageY + 10) + 'px')
                .style('left', (event.pageX + 10) + 'px')
                .html(`
                    <strong>${d.title}</strong><br/>
                    <small>${d.categoryName || 'Bez kategorie'}</small><br/>
                    <a href="#" data-url="/post/${d.id}" class="posts-graph-link" style="color: #4da6ff; text-decoration: none; margin-top: 8px; display: inline-block;">Otevřít článek →</a>
                `);
            
            // Add click handler to the link inside popup
            popup.select('.posts-graph-link').on('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                // Get the URL from data attribute
                const url = e.target.getAttribute('data-url');
                
                // Hide both popup and tooltip
                popup.style('visibility', 'hidden');
                tooltip.style('visibility', 'hidden');
                
                // Hide modal overlay
                const modalOverlay = document.querySelector('.posts-graph-modal-overlay');
                if (modalOverlay) {
                    modalOverlay.style.display = 'none';
                }
                
                // Navigate after a short delay to ensure modal is hidden
                setTimeout(() => {
                    window.location.href = url;
                }, 50);
            });
        });

    // Close popup and tooltip on click outside
    svg.on('click', function() {
        popup.style('visibility', 'hidden');
        tooltip.style('visibility', 'hidden');
    });

    // Update positions on tick
    simulation.on('tick', () => {
        link
            .attr('x1', d => d.source.x)
            .attr('y1', d => d.source.y)
            .attr('x2', d => d.target.x)
            .attr('y2', d => d.target.y);

        node
            .attr('cx', d => d.x)
            .attr('cy', d => d.y);
    });

    function dragstarted(event, d) {
        if (!event.active) simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
    }

    function dragged(event, d) {
        d.fx = event.x;
        d.fy = event.y;
    }

    function dragended(event, d) {
        if (!event.active) simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
    }
}

/**
 * Toggle category links visibility
 * @param {boolean} show - Whether to show category links
 */
export function setShowCategoryLinks(show) {
    showCategoryLinks = show;
    
    // Re-render graph with updated link filter
    if (graphData && svg) {
        const containerId = svg.node().parentElement.id;
        destroyGraph(containerId);
        initGraph(containerId, graphData);
    }
}

/**
 * Cleanup graph resources
 * @param {string} containerId - ID of the container div
 */
export function destroyGraph(containerId) {
    if (simulation) {
        simulation.stop();
        simulation = null;
    }
    
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = '';
    }
    
    svg = null;
    
    // Remove tooltip and popup - first hide, then remove
    const tooltip = d3.select('.posts-graph-tooltip');
    if (!tooltip.empty()) {
        tooltip.style('visibility', 'hidden').remove();
    }
    
    const popup = d3.select('.posts-graph-popup');
    if (!popup.empty()) {
        popup.style('visibility', 'hidden').remove();
    }
}
