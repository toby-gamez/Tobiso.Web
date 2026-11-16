// Free-explore physics simulations for the "simulation" exercise type
// (Keplerovy zákony, hody v gravitačním poli, gravitační pole).
//
// Loaded as an ES module by SimulationExercise.razor. Each simulation builds its own
// canvas + controls inside the host element, so Blazor only has to call init() / dispose().
// Colours are read from the Organic CSS tokens, so light/dark theme follows the page.
// Nothing here is graded - the learner just explores.

const TAU = Math.PI * 2;
const DEG = Math.PI / 180;

// ── Physics helpers (pure, no DOM) ────────────────────────────────────────

/** Solves Kepler's equation E - e·sin(E) = M (Newton), returns E in [0, 2π). */
export function keplerE(M, e) {
    M = ((M % TAU) + TAU) % TAU;
    let E = e < 0.8 ? M : Math.PI;
    for (let i = 0; i < 40; i++) {
        const d = (E - e * Math.sin(E) - M) / (1 - e * Math.cos(E));
        E -= d;
        if (Math.abs(d) < 1e-12) break;
    }
    return E;
}

/**
 * Splits one orbit into n equal-TIME sectors (equal steps of the mean anomaly).
 * area = swept area from the focus (Kepler II), arc = orbit length covered in that time.
 */
export function keplerSectors(a, e, n) {
    const b = a * Math.sqrt(1 - e * e);
    const out = [];
    for (let k = 0; k < n; k++) {
        const E0 = k === 0 ? 0 : keplerE((k * TAU) / n, e);
        const E1 = k === n - 1 ? TAU : keplerE(((k + 1) * TAU) / n, e);
        const area = 0.5 * a * b * ((E1 - E0) - e * (Math.sin(E1) - Math.sin(E0)));
        let arc = 0;
        const steps = 48;
        for (let i = 0; i < steps; i++) {
            const E = E0 + ((i + 0.5) / steps) * (E1 - E0);
            arc += Math.sqrt(a * a * Math.sin(E) ** 2 + b * b * Math.cos(E) ** 2);
        }
        arc *= (E1 - E0) / steps;
        out.push({ E0, E1, area, arc });
    }
    return out;
}

/** Classical RK4 step for a state array driven by derivs(state) → state'. */
export function rk4(s, dt, derivs) {
    const add = (a, b, k) => a.map((v, i) => v + b[i] * k);
    const k1 = derivs(s);
    const k2 = derivs(add(s, k1, dt / 2));
    const k3 = derivs(add(s, k2, dt / 2));
    const k4 = derivs(add(s, k3, dt));
    return s.map((v, i) => v + (dt / 6) * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]));
}

/** Central force F = mu / r^n (per unit mass); state = [x, y, vx, vy]. */
function centralDerivs(mu, n) {
    return (s) => {
        const r = Math.hypot(s[0], s[1]);
        const f = mu / Math.pow(r, n + 1);
        return [s[2], s[3], -s[0] * f, -s[1] * f];
    };
}

/**
 * Integrates a trajectory in a central field until it crashes (r < rMin),
 * escapes (r > rMax), completes `maxTime` or runs out of steps.
 * Returns { pts: [{t,x,y}], end: "crash" | "escape" | "time" }.
 */
export function integrateCentral(start, mu, n, rMin, rMax, maxTime) {
    const f = centralDerivs(mu, n);
    let s = start.slice();
    let t = 0;
    const pts = [{ t, x: s[0], y: s[1], vx: s[2], vy: s[3] }];
    for (let i = 0; i < 6000; i++) {
        const r = Math.hypot(s[0], s[1]);
        const v = Math.hypot(s[2], s[3]);
        // step proportional to the local dynamical time keeps accuracy near periapsis
        const dt = Math.min(0.01 * r / Math.max(v, 1e-9), 0.02 * Math.sqrt(Math.pow(r, n + 1) / mu));
        s = rk4(s, dt, f);
        t += dt;
        const r2 = Math.hypot(s[0], s[1]);
        if (r2 < rMin) {
            // linear interpolation back onto the surface so the path ends exactly there
            const k = (r - rMin) / Math.max(r - r2, 1e-12);
            const p = pts[pts.length - 1];
            pts.push({ t: p.t + (t - p.t) * k, x: p.x + (s[0] - p.x) * k, y: p.y + (s[1] - p.y) * k, vx: s[2], vy: s[3] });
            return { pts, end: "crash" };
        }
        pts.push({ t, x: s[0], y: s[1], vx: s[2], vy: s[3] });
        if (r2 > rMax) return { pts, end: "escape" };
        if (t >= maxTime) return { pts, end: "time" };
    }
    return { pts, end: "time" };
}

/** Orbit classification from position + velocity in a 1/r² field. */
export function classifyOrbit(r, v, angleFromHorizontal, mu, rBody) {
    const eps = (v * v) / 2 - mu / r;
    const h = r * v * Math.cos(angleFromHorizontal);
    const e = Math.sqrt(Math.max(0, 1 + (2 * eps * h * h) / (mu * mu)));
    if (Math.abs(eps) < 1e-4 * mu / r) return { kind: "parabola", e: 1, eps };
    if (eps > 0) return { kind: "hyperbola", e, eps };
    const a = -mu / (2 * eps);
    const rp = a * (1 - e);
    if (e < 0.005 && rp > rBody) return { kind: "circle", e, eps, a, rp, T: TAU * Math.sqrt((a * a * a) / mu) };
    return { kind: rp > rBody ? "orbit" : "suborbital", e, eps, a, rp, T: TAU * Math.sqrt((a * a * a) / mu) };
}

/** Flat-field (uniform g) throw: closed-form quantities. */
export function flatThrow(v0, angleDeg, h, g) {
    const vx = v0 * Math.cos(angleDeg * DEG);
    const vy = v0 * Math.sin(angleDeg * DEG);
    const tFlight = (vy + Math.sqrt(vy * vy + 2 * g * h)) / g;
    const range = vx * tFlight;
    const hMax = vy > 0 ? h + (vy * vy) / (2 * g) : h;
    const vImpact = Math.hypot(vx, vy - g * tFlight);
    return { vx, vy, tFlight, range, hMax, vImpact };
}

function niceStep(span, target = 6) {
    const raw = span / target;
    const pow = Math.pow(10, Math.floor(Math.log10(raw)));
    const m = raw / pow;
    return (m < 1.5 ? 1 : m < 3.5 ? 2 : m < 7.5 ? 5 : 10) * pow;
}

const fmt = (v, d = 2) => v.toFixed(d).replace(".", ",");

// ── DOM / drawing helpers ──────────────────────────────────────────────────

function el(tag, cls, text) {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
}

const reducedMotion = () => window.matchMedia?.("(prefers-reduced-motion: reduce)").matches === true;

function addSlider(parent, { label, min, max, step, value, unit = "", digits = 2, onInput }) {
    const wrap = el("label", "sim-field");
    const head = el("span", "sim-field-head");
    const name = el("span", "sim-field-name", label);
    const out = el("output", "sim-field-value");
    head.append(name, out);
    const input = el("input");
    input.type = "range";
    input.min = min; input.max = max; input.step = step; input.value = value;
    const show = () => {
        const v = parseFloat(input.value);
        out.textContent = `${fmt(v, digits)}${unit ? " " + unit : ""}`;
        // filled part of the track (see .sim-field input[type="range"] in simulation.css)
        input.style.setProperty("--fill", `${((v - min) / (max - min)) * 100}%`);
    };
    input.addEventListener("input", () => { show(); onInput(parseFloat(input.value)); });
    show();
    wrap.append(head, input);
    parent.append(wrap);
    return {
        set(v) { input.value = v; show(); },
        get: () => parseFloat(input.value),
    };
}

function addToggle(parent, label, checked, onChange) {
    const wrap = el("label", "sim-toggle");
    const input = el("input");
    input.type = "checkbox";
    input.checked = checked;
    input.addEventListener("change", () => onChange(input.checked));
    wrap.append(input, el("span", null, label));
    parent.append(wrap);
    return input;
}

function addButton(parent, label, onClick, on = false) {
    const b = el("button", on ? "pill pill-on" : "pill", label);
    b.type = "button";
    b.addEventListener("click", onClick);
    parent.append(b);
    return b;
}

function addReadouts(parent, labels) {
    const grid = el("div", "sim-readouts");
    const cells = {};
    for (const [key, label] of Object.entries(labels)) {
        const c = el("div", "sim-readout");
        c.append(el("span", "sim-readout-label", label));
        cells[key] = el("span", "sim-readout-value", "–");
        c.append(cells[key]);
        grid.append(c);
    }
    parent.append(grid);
    return (values) => { for (const k in values) cells[k].textContent = values[k]; };
}

/** Canvas that tracks its CSS size (HiDPI aware) and re-reads theme colours periodically. */
function createStage(host, ariaLabel) {
    const wrap = el("div", "sim-canvas-wrap");
    const canvas = el("canvas", "sim-canvas");
    canvas.setAttribute("role", "img");
    canvas.setAttribute("aria-label", ariaLabel);
    wrap.append(canvas);
    host.append(wrap);

    const ctx = canvas.getContext("2d");
    const stage = { canvas, ctx, w: 0, h: 0, c: {}, visible: true };

    const resize = () => {
        const r = wrap.getBoundingClientRect();
        const dpr = window.devicePixelRatio || 1;
        stage.w = r.width;
        stage.h = r.height;
        canvas.width = Math.max(1, Math.round(r.width * dpr));
        canvas.height = Math.max(1, Math.round(r.height * dpr));
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        stage.dirty = true;
    };
    const ro = new ResizeObserver(resize);
    ro.observe(wrap);
    const io = new IntersectionObserver((e) => { stage.visible = e[0].isIntersecting; });
    io.observe(wrap);

    stage.readColors = () => {
        const cs = getComputedStyle(canvas);
        const g = (n, fb) => cs.getPropertyValue(n).trim() || fb;
        stage.c = {
            bg: g("--color-bg", "#f5ead8"),
            surface: g("--color-surface", "#ebddc5"),
            text: g("--color-text", "#201e1d"),
            grid: g("--color-divider", "#00000020"),
            accent: g("--color-accent-600", "#b2622d"),
            accentSoft: g("--color-accent-400", "#e8935c"),
            accent2: g("--color-accent-2-600", "#607c43"),
            accent2Soft: g("--color-accent-2-400", "#9fbf74"),
            muted: g("--color-neutral-600", "#907860"),
            faint: g("--color-neutral-400", "#bcaa8e"),
        };
    };
    stage.readColors();
    resize();
    stage.dispose = () => { ro.disconnect(); io.disconnect(); };
    return stage;
}

/** requestAnimationFrame loop; step(dt seconds) is only called while the canvas is on screen. */
function startLoop(stage, step) {
    let last = performance.now();
    let id = 0;
    let frame = 0;
    let alive = true;
    const tick = (now) => {
        if (!alive) return;
        // the first frame timestamp can precede performance.now() taken at start-up
        const dt = Math.min(0.05, Math.max(0, (now - last) / 1000));
        last = now;
        if (stage.visible) {
            if (frame++ % 30 === 0) stage.readColors();
            step(dt);
        }
        id = requestAnimationFrame(tick);
    };
    id = requestAnimationFrame(tick);
    return () => { alive = false; cancelAnimationFrame(id); };
}

function arrow(ctx, x1, y1, x2, y2, color, width = 2.5) {
    const len = Math.hypot(x2 - x1, y2 - y1);
    if (len < 2) return;
    const ang = Math.atan2(y2 - y1, x2 - x1);
    const head = Math.min(9, len * 0.45);
    ctx.strokeStyle = color; ctx.fillStyle = color; ctx.lineWidth = width; ctx.lineCap = "round";
    ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2 - Math.cos(ang) * head * 0.6, y2 - Math.sin(ang) * head * 0.6); ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(x2, y2);
    ctx.lineTo(x2 - Math.cos(ang - 0.45) * head, y2 - Math.sin(ang - 0.45) * head);
    ctx.lineTo(x2 - Math.cos(ang + 0.45) * head, y2 - Math.sin(ang + 0.45) * head);
    ctx.closePath(); ctx.fill();
}

function label(ctx, text, x, y, color, align = "center", size = 13, weight = 600) {
    ctx.font = `${weight} ${size}px Figtree, system-ui, sans-serif`;
    ctx.fillStyle = color; ctx.textAlign = align; ctx.textBaseline = "middle";
    ctx.fillText(text, x, y);
}

function dot(ctx, x, y, r, fill, stroke) {
    ctx.beginPath(); ctx.arc(x, y, r, 0, TAU);
    if (fill) { ctx.fillStyle = fill; ctx.fill(); }
    if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = 2; ctx.stroke(); }
}

function note(parent, text) {
    parent.append(el("p", "sim-note", text));
}

function segmented(parent, items, current, onPick) {
    const row = el("div", "sim-segmented");
    const btns = items.map(([id, text]) => {
        const b = el("button", id === current ? "pill pill-on" : "pill", text);
        b.type = "button";
        b.addEventListener("click", () => { btns.forEach((x, i) => x.className = items[i][0] === id ? "pill pill-on" : "pill"); onPick(id); });
        row.append(b);
        return b;
    });
    parent.append(row);
}

// ── Kepler ────────────────────────────────────────────────────────────────

const PLANETS = [
    ["Merkur", 0.387], ["Venuše", 0.723], ["Země", 1], ["Mars", 1.524], ["Jupiter", 5.203],
];
const MU_SUN = 4 * Math.PI * Math.PI;   // AU³ / rok²
const KMS_PER_AU_YR = 4.74047;          // 1 AU/rok v km/s
const YEAR_SECONDS = 10;                // 1 rok simulace = 10 s při rychlosti ×1

function initKepler(host, cfg) {
    const st = {
        e: cfg.eccentricity ?? 0.6,
        a: cfg.semiMajorAxis ?? 1,
        n: cfg.sectors ?? 8,
        speed: 1,
        M: 0,
        playing: !reducedMotion(),
        axes: cfg.showAxes ?? true,
        foci: cfg.showFoci ?? false,
        sectors: cfg.showSectors ?? true,
        velocity: cfg.showVelocity ?? true,
    };
    let stats = keplerSectors(st.a, st.e, st.n);
    const refreshStats = () => { stats = keplerSectors(st.a, st.e, st.n); };

    const root = el("div", "sim-root");
    host.append(root);
    const stage = createStage(root, "Eliptická dráha planety kolem Slunce s vyznačenými poloosami a výsečemi stejné plochy");
    const controls = el("div", "sim-controls");
    root.append(controls);

    addSlider(controls, { label: "Excentricita e", min: 0, max: 0.9, step: 0.01, value: st.e, digits: 2, onInput: (v) => { st.e = v; refreshStats(); } });
    const aSlider = addSlider(controls, { label: "Hlavní poloosa a", min: 0.3, max: 6, step: 0.01, value: st.a, unit: "AU", onInput: (v) => { st.a = v; refreshStats(); } });
    addSlider(controls, { label: "Počet stejných časových dílů", min: 3, max: 16, step: 1, value: st.n, digits: 0, onInput: (v) => { st.n = v; refreshStats(); } });
    addSlider(controls, { label: "Rychlost animace", min: 0, max: 4, step: 0.05, value: st.speed, unit: "×", digits: 2, onInput: (v) => { st.speed = v; } });

    const toggles = el("div", "sim-toggles");
    root.append(toggles);
    addToggle(toggles, "Poloosy", st.axes, (v) => { st.axes = v; });
    addToggle(toggles, "Ohniska (r₁ + r₂)", st.foci, (v) => { st.foci = v; });
    addToggle(toggles, "Výseče za stejný čas", st.sectors, (v) => { st.sectors = v; });
    addToggle(toggles, "Vektor rychlosti", st.velocity, (v) => { st.velocity = v; });

    const actions = el("div", "sim-actions");
    root.append(actions);
    const playBtn = addButton(actions, st.playing ? "Pozastavit" : "Spustit", () => {
        st.playing = !st.playing;
        playBtn.textContent = st.playing ? "Pozastavit" : "Spustit";
    }, true);
    addButton(actions, "Od začátku", () => { st.M = 0; });
    for (const [name, a] of PLANETS) {
        addButton(actions, name, () => { st.a = a; aSlider.set(a); refreshStats(); });
    }

    const update = addReadouts(root, {
        r: "Vzdálenost od Slunce r",
        v: "Rychlost v",
        T: "Oběžná doba T = a³ᐟ²",
        k3: "T² / a³",
        area: "Plocha jedné výseče",
        arc: "Dráha v aktuální výseči",
        dt: "Doba jedné výseče",
        sum: "r₁ + r₂",
    });
    note(root, "Excentricita je záměrně přehnaná – skutečná dráha Země má e ≈ 0,017 a je téměř kružnice. Všechny výseče mají stejnou plochu, protože je planeta oběhne za stejný čas.");

    const stop = startLoop(stage, (dt) => {
        const T = Math.pow(st.a, 1.5);
        if (st.playing) st.M = (st.M + (TAU * dt * st.speed) / (YEAR_SECONDS * T)) % TAU;
        draw(T);
    });

    function draw(T) {
        const { ctx, w, h, c } = stage;
        ctx.clearRect(0, 0, w, h);
        const { a, e, n } = st;
        const b = a * Math.sqrt(1 - e * e);
        const cc = a * e;
        const pad = 40;
        const s = Math.min((w - 2 * pad) / (2 * a), (h - 2 * pad) / (2 * b));
        const C = { x: w / 2, y: h / 2 };
        const F = { x: C.x + cc * s, y: C.y };      // Slunce (ohnisko)
        const F2 = { x: C.x - cc * s, y: C.y };
        const pos = (E) => ({ x: F.x + a * (Math.cos(E) - e) * s, y: F.y - b * Math.sin(E) * s });

        const E = keplerE(st.M, e);
        const P = pos(E);
        const sector = Math.min(n - 1, Math.floor(st.M / (TAU / n)));

        // sektory
        if (st.sectors) {
            for (let k = 0; k < n; k++) {
                const sc = stats[k];
                ctx.beginPath(); ctx.moveTo(F.x, F.y);
                const steps = 28;
                for (let i = 0; i <= steps; i++) {
                    const p = pos(sc.E0 + ((sc.E1 - sc.E0) * i) / steps);
                    ctx.lineTo(p.x, p.y);
                }
                ctx.closePath();
                ctx.globalAlpha = k === sector ? 0.6 : 0.2;
                ctx.fillStyle = k % 2 === 0 ? c.accentSoft : c.accent2Soft;
                ctx.fill();
                ctx.globalAlpha = 0.5; ctx.strokeStyle = c.faint; ctx.lineWidth = 1; ctx.stroke();
                ctx.globalAlpha = 1;
            }
        }

        // dráha
        ctx.beginPath();
        for (let i = 0; i <= 200; i++) {
            const p = pos((i / 200) * TAU);
            if (i === 0) ctx.moveTo(p.x, p.y); else ctx.lineTo(p.x, p.y);
        }
        ctx.strokeStyle = c.muted; ctx.lineWidth = 2; ctx.stroke();

        // značky po stejných časových dílech (u afelu řidší, u perihelu hustší)
        if (st.sectors) for (const sc of stats) { const p = pos(sc.E0); dot(ctx, p.x, p.y, 3.5, c.muted); }

        // poloosy
        if (st.axes) {
            ctx.setLineDash([6, 5]); ctx.lineWidth = 1.5;
            ctx.strokeStyle = c.accent;
            ctx.beginPath(); ctx.moveTo(C.x - a * s, C.y); ctx.lineTo(C.x + a * s, C.y); ctx.stroke();
            ctx.strokeStyle = c.accent2;
            ctx.beginPath(); ctx.moveTo(C.x, C.y + b * s); ctx.lineTo(C.x, C.y - b * s); ctx.stroke();
            ctx.setLineDash([]);
            dot(ctx, C.x, C.y, 3, c.muted);
            label(ctx, `a = ${fmt(a)} AU`, C.x - (a * s) / 2, C.y + 16, c.accent);
            label(ctx, `b = ${fmt(b)} AU`, C.x + 6, C.y - (b * s) / 2, c.accent2, "left");
            label(ctx, "perihel", C.x + a * s - 4, C.y - 16, c.muted, "right", 12, 500);
            label(ctx, "afel", C.x - a * s + 4, C.y - 16, c.muted, "left", 12, 500);
        }

        // ohniska a součet vzdáleností
        let r1 = Math.hypot(P.x - F.x, P.y - F.y) / s;
        const r2 = Math.hypot(P.x - F2.x, P.y - F2.y) / s;
        if (st.foci) {
            dot(ctx, F2.x, F2.y, 5, null, c.muted);
            ctx.setLineDash([3, 4]); ctx.lineWidth = 1.5; ctx.strokeStyle = c.muted;
            ctx.beginPath(); ctx.moveTo(P.x, P.y); ctx.lineTo(F2.x, F2.y); ctx.stroke();
            ctx.setLineDash([]);
        }
        ctx.lineWidth = 1.8; ctx.strokeStyle = c.accent;
        ctx.beginPath(); ctx.moveTo(F.x, F.y); ctx.lineTo(P.x, P.y); ctx.stroke();

        // měřítko 1 AU
        ctx.strokeStyle = c.muted; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(16, h - 18); ctx.lineTo(16 + s, h - 18); ctx.stroke();
        label(ctx, "1 AU", 16 + s / 2, h - 30, c.muted, "center", 12, 500);

        // Slunce
        dot(ctx, F.x, F.y, 15, c.accentSoft, c.accent);

        // rychlost
        const v = Math.sqrt(MU_SUN * (2 / r1 - 1 / a));
        if (st.velocity) {
            const tx = -a * Math.sin(E), ty = -b * Math.cos(E);   // canvas směr
            const tl = Math.hypot(tx, ty);
            const len = Math.min(90, 22 + 14 * v);
            arrow(ctx, P.x, P.y, P.x + (tx / tl) * len, P.y + (ty / tl) * len, c.accent2);
        }

        // planeta
        dot(ctx, P.x, P.y, 8, c.accent2Soft, c.accent2);

        const sc = stats[sector];
        update({
            r: `${fmt(r1, 3)} AU`,
            v: `${fmt(v * KMS_PER_AU_YR, 1)} km/s`,
            T: `${fmt(T, 2)} roku`,
            k3: fmt((T * T) / (a * a * a), 3),
            area: `${fmt(sc.area, 3)} AU²`,
            arc: `${fmt(sc.arc, 3)} AU`,
            dt: `${fmt((T * 365.25) / n, 0)} dní`,
            sum: st.foci ? `${fmt(r1 + r2, 3)} AU (= 2a)` : "zapni ohniska",
        });
    }

    return { dispose: () => { stop(); stage.dispose(); } };
}

// ── Hod v gravitačním poli ──────────────────────────────────────────────────

const BODIES = [
    ["Země", 9.81], ["Měsíc", 1.62], ["Mars", 3.71], ["Jupiter", 24.79],
];
const R_EARTH = 6371;       // km
const MU_EARTH = 398600;    // km³/s²

function initProjectile(host, cfg) {
    const st = {
        mode: cfg.mode === "central" ? "central" : "flat",
        // plochý hod
        g: cfg.gravity ?? 9.81, v0: cfg.speed ?? 20, angle: cfg.angle ?? 45, h: cfg.height ?? 0,
        vectors: true, strobe: true,
        // centrální pole
        cv: cfg.centralSpeed ?? 7.0, cangle: cfg.centralAngle ?? 0, calt: cfg.altitude ?? 300, compare: true,
        t: 0, playing: !reducedMotion(),
    };

    const root = el("div", "sim-root");
    host.append(root);
    segmented(root, [["flat", "Homogenní pole (g = konst.)"], ["central", "Centrální pole (Země)"]], st.mode, (m) => { st.mode = m; rebuildPanel(); restart(); });
    const stage = createStage(root, "Trajektorie vrhu v gravitačním poli");
    const panel = el("div", "sim-panel");
    root.append(panel);
    // px na 1 m/s: délka šipky rychlosti i tažného úchytu (větší plátno = delší šipka)
    const vecPx = () => Math.min(3.4, Math.max(1.8, Math.min(stage.w, stage.h) * 0.0065));

    let flat = null;          // analytické veličiny
    let lastView = null;      // měřítko plochého hodu z posledního snímku (pro tažení myší)
    const flatSliders = {};   // posuvníky plochého hodu, aby je tažení v plátně mohlo přepsat
    let central = null;       // { sim, info, D }
    let readout = () => {};

    function restart() { st.t = 0; compute(); st.playing = true; }

    function compute() {
        if (st.mode === "flat") {
            flat = flatThrow(st.v0, st.angle, st.h, st.g);
        } else {
            const r0 = R_EARTH + st.calt;
            const a = st.cangle * DEG;
            computeCentral([0, r0, st.cv * Math.cos(a), st.cv * Math.sin(a)], []);
        }
    }

    // Nová dráha z libovolného stavu [x, y, vx, vy]; past = už uražená část (kreslí se plně).
    function computeCentral(start, past) {
        const r = Math.hypot(start[0], start[1]);
        const v = Math.hypot(start[2], start[3]);
        const radial = (start[0] * start[2] + start[1] * start[3]) / (r * Math.max(v, 1e-9));
        const info = classifyOrbit(r, v, Math.asin(Math.max(-1, Math.min(1, radial))), MU_EARTH, R_EARTH);
        const sim = integrateCentral(start, MU_EARTH, 2, R_EARTH, 14 * R_EARTH, Math.max(info.T ?? 20000, 600));
        central = { sim, info, D: sim.pts[sim.pts.length - 1].t, r0: r, start, past };
    }

    const centralEnded = () => central.sim.end !== "time" && st.t >= central.D - 1e-9;
    const centralFlying = () => central && st.t > 1e-6 && !centralEnded();

    function centralNow() {
        const pts = central.sim.pts;
        let i = 0;
        while (i < pts.length - 1 && pts[i + 1].t <= st.t) i++;
        const a = pts[i], b = pts[Math.min(i + 1, pts.length - 1)];
        const k = b.t > a.t ? Math.min(1, Math.max(0, (st.t - a.t) / (b.t - a.t))) : 0;
        const l = (u, w) => u + (w - u) * k;
        return { x: l(a.x, b.x), y: l(a.y, b.y), vx: l(a.vx, b.vx), vy: l(a.vy, b.vy) };
    }

    // Změna za letu: dráha se přepočítá z aktuálního bodu (jako zážeh motoru), uražená část zůstane.
    // Před startem nebo po dopadu/úniku jde o nový výstřel z nastavených hodnot.
    function relaunchCentral(adjust) {
        if (!centralFlying()) { restart(); return; }
        const n = centralNow();
        const traveled = central.past
            .concat(central.sim.pts.filter((p) => p.t <= st.t).map((p) => ({ x: p.x, y: p.y })), [{ x: n.x, y: n.y }])
            .slice(-4000);
        const [vx, vy] = adjust(n);
        computeCentral([n.x, n.y, vx, vy], traveled);
        st.t = 0;
        st.playing = true;
    }

    // směr = aktuální směr letu (nebo vodorovně, stojí-li těleso); velikost = v
    const withSpeed = (v) => (n) => {
        const sp = Math.hypot(n.vx, n.vy), r = Math.hypot(n.x, n.y);
        return sp > 1e-9 ? [(n.vx / sp) * v, (n.vy / sp) * v] : [(n.y / r) * v, (-n.x / r) * v];
    };
    // úhel nad místní vodorovnou rovinou (tečna po směru výstřelu), rychlost zůstává
    const withAngle = (A) => (n) => {
        const sp = Math.hypot(n.vx, n.vy), r = Math.hypot(n.x, n.y);
        const ca = Math.cos(A), sa = Math.sin(A);
        return [sp * (ca * (n.y / r) + sa * (n.x / r)), sp * (ca * (-n.x / r) + sa * (n.y / r))];
    };

    function rebuildPanel() {
        st.drag = null;
        panel.replaceChildren();
        const controls = el("div", "sim-controls");
        panel.append(controls);
        const redo = () => { restart(); };

        if (st.mode === "flat") {
            flatSliders.v0 = addSlider(controls, { label: "Počáteční rychlost v₀", min: 1, max: 80, step: 0.5, value: st.v0, unit: "m/s", digits: 1, onInput: (v) => { st.v0 = v; redo(); } });
            flatSliders.angle = addSlider(controls, { label: "Úhel výstřelu α", min: -45, max: 90, step: 1, value: st.angle, unit: "°", digits: 0, onInput: (v) => { st.angle = v; redo(); } });
            flatSliders.h = addSlider(controls, { label: "Výška výstřelu h", min: 0, max: 60, step: 1, value: st.h, unit: "m", digits: 0, onInput: (v) => { st.h = v; redo(); } });
            const gs = addSlider(controls, { label: "Gravitační zrychlení g", min: 1, max: 26, step: 0.01, value: st.g, unit: "m/s²", onInput: (v) => { st.g = v; redo(); } });
            const toggles = el("div", "sim-toggles");
            panel.append(toggles);
            addToggle(toggles, "Rozklad rychlosti", st.vectors, (v) => { st.vectors = v; });
            addToggle(toggles, "Poloha každou 0,5 s", st.strobe, (v) => { st.strobe = v; });
            const actions = el("div", "sim-actions");
            panel.append(actions);
            addButton(actions, "Vystřelit znovu", redo, true);
            for (const [name, g] of BODIES) addButton(actions, name, () => { st.g = g; gs.set(g); redo(); });
            readout = addReadouts(panel, { range: "Dolet", hmax: "Maximální výška", t: "Doba letu", vi: "Rychlost dopadu" });
            note(panel, "Vodorovná složka rychlosti se nemění, svislá se mění rovnoměrně – proto jsou značky po 0,5 s vodorovně stejně daleko od sebe. Vodorovný hod zkus pomocí α = 0° a nenulové výšky. Trajektorii můžeš měnit i přímo v obrázku: tažením za kroužek na konci šipky nastavíš rychlost a úhel, tažením za bod výstřelu výšku.");
        } else {
            const altSlider = addSlider(controls, { label: "Výška výstřelu nad povrchem", min: 0, max: 1000, step: 10, value: st.calt, unit: "km", digits: 0, onInput: (v) => { st.calt = v; redo(); } });
            const vSlider = addSlider(controls, { label: "Rychlost v (jde měnit i za letu)", min: 0, max: 14, step: 0.05, value: st.cv, unit: "km/s", onInput: (v) => { st.cv = v; relaunchCentral(withSpeed(v)); } });
            const angSlider = addSlider(controls, { label: "Úhel nad vodorovnou rovinou", min: -30, max: 90, step: 1, value: st.cangle, unit: "°", digits: 0, onInput: (v) => { st.cangle = v; relaunchCentral(withAngle(v * DEG)); } });
            const toggles = el("div", "sim-toggles");
            panel.append(toggles);
            addToggle(toggles, "Porovnat s homogenním polem", st.compare, (v) => { st.compare = v; });
            const actions = el("div", "sim-actions");
            panel.append(actions);
            addButton(actions, "Vystřelit znovu", redo, true);
            // kosmická rychlost vodorovně vzhledem k místu, kde těleso právě je
            const cosmic = (mult) => {
                const n = centralFlying() ? centralNow() : null;
                const r = n ? Math.hypot(n.x, n.y) : R_EARTH + st.calt;
                const v = Math.sqrt((mult * MU_EARTH) / r);
                st.cv = v; st.cangle = 0; vSlider.set(v); angSlider.set(0);
                relaunchCentral((m) => { const rr = Math.hypot(m.x, m.y); return [(m.y / rr) * v, (-m.x / rr) * v]; });
            };
            addButton(actions, "1. kosmická", () => cosmic(1));
            addButton(actions, "2. kosmická", () => cosmic(2));
            void altSlider;
            readout = addReadouts(panel, { type: "Typ dráhy", ecc: "Excentricita e", v1: "1. kosmická v₁", v2: "2. kosmická v₂", T: "Oběžná doba", status: "Stav" });
            note(panel, "Při malých rychlostech je dráha téměř parabola (homogenní pole), jak ale rychlost roste, je vidět, že skutečná dráha je část elipsy se Zemí v ohnisku. Nad v₁ těleso Zemi neprobíjí, nad v₂ unikne. Rychlost a úhel můžeš měnit i za letu – těleso dostane impuls v místě, kde zrovna je, a dráha se přepočítá.");
        }
    }

    rebuildPanel();
    compute();

    const stop = startLoop(stage, (dt) => {
        if (st.mode === "flat") stepFlat(dt); else stepCentral(dt);
    });

    // ── plochý hod ──
    function stepFlat(dt) {
        const { ctx, w, h, c } = stage;
        ctx.clearRect(0, 0, w, h);
        const f = flat;
        const rate = f.tFlight > 4 ? f.tFlight / 4 : 1;
        if (st.playing) { st.t += dt * rate; if (st.t >= f.tFlight) { st.t = f.tFlight; st.playing = false; } }

        const padL = 44, padR = 24, padT = 30, padB = 44;
        const xSpan = Math.max(f.range, 10) * 1.08, ySpan = Math.max(f.hMax, 5) * 1.2;
        // při tažení se měřítko zmrazí, jinak by se úchyt pod prstem posouval
        const s = st.drag ? st.drag.s : Math.min((w - padL - padR) / xSpan, (h - padT - padB) / ySpan);
        lastView = { s, padL, padB, h };
        const X = (x) => padL + x * s, Y = (y) => h - padB - y * s;
        const pos = (t) => ({ x: f.vx * t, y: st.h + f.vy * t - 0.5 * st.g * t * t });

        // země + osa
        ctx.fillStyle = c.accent2Soft; ctx.globalAlpha = 0.35;
        ctx.fillRect(0, Y(0), w, h - Y(0)); ctx.globalAlpha = 1;
        ctx.strokeStyle = c.accent2; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(0, Y(0)); ctx.lineTo(w, Y(0)); ctx.stroke();
        const step = niceStep(xSpan);
        for (let x = 0; x <= xSpan; x += step) {
            ctx.strokeStyle = c.muted; ctx.lineWidth = 1.5;
            ctx.beginPath(); ctx.moveTo(X(x), Y(0)); ctx.lineTo(X(x), Y(0) + 6); ctx.stroke();
            label(ctx, `${fmt(x, x < 10 && step < 1 ? 1 : 0)} m`, X(x), Y(0) + 18, c.muted, "center", 12, 500);
        }

        // věž
        if (st.h > 0) {
            ctx.strokeStyle = c.muted; ctx.lineWidth = 4; ctx.lineCap = "round";
            ctx.beginPath(); ctx.moveTo(X(0) - 2, Y(0)); ctx.lineTo(X(0) - 2, Y(st.h)); ctx.stroke();
        }

        // předpověď (čárkovaně)
        ctx.setLineDash([6, 6]); ctx.strokeStyle = c.faint; ctx.lineWidth = 2;
        ctx.beginPath();
        for (let i = 0; i <= 80; i++) { const p = pos((i / 80) * f.tFlight); i ? ctx.lineTo(X(p.x), Y(p.y)) : ctx.moveTo(X(p.x), Y(p.y)); }
        ctx.stroke(); ctx.setLineDash([]);

        // uražená dráha
        ctx.strokeStyle = c.accent; ctx.lineWidth = 3;
        ctx.beginPath();
        const nSeg = 80;
        for (let i = 0; i <= nSeg; i++) { const p = pos((i / nSeg) * st.t); i ? ctx.lineTo(X(p.x), Y(p.y)) : ctx.moveTo(X(p.x), Y(p.y)); }
        ctx.stroke();

        // stroboskop
        if (st.strobe) {
            for (let t = 0; t <= st.t + 1e-9; t += 0.5) { const p = pos(t); dot(ctx, X(p.x), Y(p.y), 3.5, c.muted); }
        }

        // míč + vektory
        const P = pos(st.t);
        const vx = f.vx, vy = f.vy - st.g * st.t;
        if (st.vectors) {
            const k = vecPx();
            arrow(ctx, X(P.x), Y(P.y), X(P.x) + vx * k, Y(P.y), c.accent2, 2.5);
            arrow(ctx, X(P.x), Y(P.y), X(P.x), Y(P.y) - vy * k, c.accent, 2.5);
            arrow(ctx, X(P.x), Y(P.y), X(P.x) + vx * k, Y(P.y) - vy * k, c.text, 2);
        }
        dot(ctx, X(P.x), Y(P.y), 8, c.accentSoft, c.accent);

        // úchyty: konec počáteční rychlosti (v₀, α) a bod výstřelu (výška h)
        const L = { x: X(0), y: Y(st.h) };
        const tip = { x: L.x + f.vx * vecPx(), y: L.y - f.vy * vecPx() };
        ctx.setLineDash([4, 4]); ctx.strokeStyle = c.accent; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(L.x, L.y); ctx.lineTo(tip.x, tip.y); ctx.stroke(); ctx.setLineDash([]);
        ctx.beginPath(); ctx.arc(tip.x, tip.y, 10, 0, TAU);
        ctx.fillStyle = c.bg; ctx.globalAlpha = 0.85; ctx.fill(); ctx.globalAlpha = 1;
        ctx.strokeStyle = c.accent; ctx.lineWidth = 2.5; ctx.stroke();
        ctx.beginPath(); ctx.arc(L.x, L.y, 7, 0, TAU); ctx.strokeStyle = c.muted; ctx.lineWidth = 2; ctx.stroke();
        if (!st.touched) label(ctx, "táhni za kroužek", tip.x + 16, tip.y - 14, c.muted, "left", 12, 500);

        readout({
            range: `${fmt(f.range, 1)} m`, hmax: `${fmt(f.hMax, 1)} m`,
            t: `${fmt(f.tFlight, 2)} s`, vi: `${fmt(f.vImpact, 1)} m/s`,
        });
    }

    // ── tažení trajektorie přímo v obrázku (plochý hod) ──
    const canvas = stage.canvas;
    canvas.style.touchAction = "none";
    const local = (ev) => { const r = canvas.getBoundingClientRect(); return { x: ev.clientX - r.left, y: ev.clientY - r.top }; };
    const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
    const grabbed = (p) => {
        if (st.mode !== "flat" || !flat || !lastView) return null;
        const L = { x: lastView.padL, y: lastView.h - lastView.padB - st.h * lastView.s };
        const tip = { x: L.x + flat.vx * vecPx(), y: L.y - flat.vy * vecPx() };
        if (Math.hypot(p.x - tip.x, p.y - tip.y) < 26) return "v";
        if (Math.hypot(p.x - L.x, p.y - L.y) < 22) return "h";
        return null;
    };
    const onDown = (ev) => {
        const kind = grabbed(local(ev));
        if (!kind) return;
        st.drag = { kind, s: lastView.s };
        st.touched = true;
        st.playing = false;
        st.t = 0;
        canvas.setPointerCapture(ev.pointerId);
        canvas.style.cursor = "grabbing";
    };
    const onMove = (ev) => {
        const p = local(ev);
        if (!st.drag) { canvas.style.cursor = grabbed(p) ? "grab" : ""; return; }
        const { padL, padB, h } = lastView, s = st.drag.s;
        if (st.drag.kind === "v") {
            const dx = p.x - padL, dy = h - padB - st.h * s - p.y;
            st.v0 = clamp(Math.round((Math.hypot(dx, dy) / vecPx()) * 2) / 2, 1, 80);
            st.angle = clamp(Math.round(Math.atan2(dy, dx) / DEG), -45, 90);
            flatSliders.v0.set(st.v0);
            flatSliders.angle.set(st.angle);
        } else {
            st.h = clamp(Math.round((h - padB - p.y) / s), 0, 60);
            flatSliders.h.set(st.h);
        }
        flat = flatThrow(st.v0, st.angle, st.h, st.g);
    };
    const onUp = () => {
        if (!st.drag) return;
        st.drag = null;
        canvas.style.cursor = "";
        restart();      // po puštění se míč vydá po nové trajektorii
    };
    canvas.addEventListener("pointerdown", onDown);
    canvas.addEventListener("pointermove", onMove);
    canvas.addEventListener("pointerup", onUp);
    canvas.addEventListener("pointercancel", onUp);

    // ── centrální pole ──
    function stepCentral(dt) {
        const { ctx, w, h, c } = stage;
        ctx.clearRect(0, 0, w, h);
        const { sim, info, D, r0 } = central;
        const closed = sim.end === "time" && info.T != null;
        const DURATION = 9; // s reálného času na celou dráhu
        if (st.playing) {
            st.t += (dt * D) / DURATION;
            if (st.t >= D) { if (closed) st.t %= D; else { st.t = D; st.playing = false; } }
        }

        // měřítko podle trajektorie
        let minX = central.start[0], maxX = minX, minY = central.start[1], maxY = minY;
        for (const p of central.past) { minX = Math.min(minX, p.x); maxX = Math.max(maxX, p.x); minY = Math.min(minY, p.y); maxY = Math.max(maxY, p.y); }
        minX = Math.min(minX, 0); maxX = Math.max(maxX, 0);
        for (const p of sim.pts) { minX = Math.min(minX, p.x); maxX = Math.max(maxX, p.x); minY = Math.min(minY, p.y); maxY = Math.max(maxY, p.y); }
        if (sim.end === "escape") { const k = 0.35; minX *= k; maxX *= k; minY = minY > 0 ? minY * (1 - k) : minY * k; maxY *= k; }
        const minSpan = 0.5 * R_EARTH;
        const spanX = Math.max(maxX - minX, minSpan), spanY = Math.max(maxY - minY, minSpan);
        const pad = 46;
        const s = Math.min((w - 2 * pad) / spanX, (h - 2 * pad) / spanY);
        const cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
        const X = (x) => w / 2 + (x - cx) * s, Y = (y) => h / 2 - (y - cy) * s;

        // planeta
        ctx.beginPath(); ctx.arc(X(0), Y(0), R_EARTH * s, 0, TAU);
        ctx.fillStyle = c.accent2Soft; ctx.globalAlpha = 0.35; ctx.fill(); ctx.globalAlpha = 1;
        ctx.strokeStyle = c.accent2; ctx.lineWidth = 2.5; ctx.stroke();
        dot(ctx, X(0), Y(0), 3, c.accent2);

        // homogenní pole (srovnání)
        if (st.compare && central.past.length === 0) {
            const g0 = MU_EARTH / (r0 * r0);
            const a = st.cangle * DEG, vx = st.cv * Math.cos(a), vy = st.cv * Math.sin(a);
            ctx.setLineDash([6, 6]); ctx.strokeStyle = c.faint; ctx.lineWidth = 2; ctx.beginPath();
            let started = false;
            for (let t = 0; t <= D; t += D / 200) {
                const x = vx * t, y = r0 + vy * t - 0.5 * g0 * t * t;
                if (Math.hypot(x, y) < R_EARTH) break;
                started ? ctx.lineTo(X(x), Y(y)) : ctx.moveTo(X(x), Y(y)); started = true;
            }
            ctx.stroke(); ctx.setLineDash([]);
        }

        // dráha uražená před změnami za letu
        if (central.past.length > 1) {
            ctx.strokeStyle = c.accent; ctx.lineWidth = 3; ctx.beginPath();
            central.past.forEach((p, i) => i ? ctx.lineTo(X(p.x), Y(p.y)) : ctx.moveTo(X(p.x), Y(p.y)));
            ctx.stroke();
        }

        // skutečná trajektorie – celá slabě, uražená část silně
        ctx.strokeStyle = c.faint; ctx.lineWidth = 2; ctx.beginPath();
        sim.pts.forEach((p, i) => i ? ctx.lineTo(X(p.x), Y(p.y)) : ctx.moveTo(X(p.x), Y(p.y)));
        ctx.stroke();
        ctx.strokeStyle = c.accent; ctx.lineWidth = 3; ctx.beginPath();
        let cur = sim.pts[0];
        ctx.moveTo(X(cur.x), Y(cur.y));
        for (let i = 1; i < sim.pts.length; i++) {
            const p = sim.pts[i];
            if (p.t <= st.t) { ctx.lineTo(X(p.x), Y(p.y)); cur = p; continue; }
            const k = (st.t - cur.t) / Math.max(p.t - cur.t, 1e-9);
            cur = { x: cur.x + (p.x - cur.x) * k, y: cur.y + (p.y - cur.y) * k };
            ctx.lineTo(X(cur.x), Y(cur.y));
            break;
        }
        ctx.stroke();
        dot(ctx, X(cur.x), Y(cur.y), 8, c.accentSoft, c.accent);
        dot(ctx, X(central.start[0]), Y(central.start[1]), 3.5, c.muted);

        // měřítko
        const bar = niceStep(w / s / 5, 1);
        ctx.strokeStyle = c.muted; ctx.lineWidth = 2;
        ctx.beginPath(); ctx.moveTo(16, h - 18); ctx.lineTo(16 + bar * s, h - 18); ctx.stroke();
        label(ctx, `${bar.toLocaleString("cs-CZ")} km`, 16 + (bar * s) / 2, h - 30, c.muted, "center", 12, 500);

        const names = {
            circle: "Kružnice", orbit: "Elipsa – oběžná dráha", suborbital: "Elipsa – těleso dopadne",
            parabola: "Parabola – těleso unikne", hyperbola: "Hyperbola – těleso unikne",
        };
        const r = Math.hypot(cur.x, cur.y);
        readout({
            type: names[info.kind],
            ecc: fmt(info.e, 3),
            v1: `${fmt(Math.sqrt(MU_EARTH / r), 2)} km/s`,
            v2: `${fmt(Math.sqrt((2 * MU_EARTH) / r), 2)} km/s`,
            T: info.T && info.kind !== "suborbital" ? `${fmt(info.T / 60, 0)} min` : "–",
            status: sim.end === "crash" ? (st.t >= D ? "Dopadlo" : "Letí") : sim.end === "escape" ? (st.t >= D ? "Uniklo" : "Letí") : "Obíhá",
        });
    }

    return {
        dispose: () => {
            stop(); stage.dispose();
            canvas.removeEventListener("pointerdown", onDown);
            canvas.removeEventListener("pointermove", onMove);
            canvas.removeEventListener("pointerup", onUp);
            canvas.removeEventListener("pointercancel", onUp);
        },
    };
}

// ── Gravitační pole ───────────────────────────────────────────────────────

function initGravityField(host, cfg) {
    const R_BODY = 0.18;
    const VIEW = 3.2;           // polovina zobrazeného rozsahu (jednotky)
    const st = {
        n: cfg.exponent ?? 2,
        mu: cfg.mass ?? 1,
        m: 1,
        vFactor: cfg.speedFactor ?? 1,
        pos: { x: 1.5, y: 0 },
        vel: null,              // null = těleso se táhne, jinak běží
        trail: [],
        status: "",
        arrows: true,
        rings: true,
    };

    const root = el("div", "sim-root");
    host.append(root);
    const stage = createStage(root, "Gravitační pole centrálního tělesa se zkušebním tělesem, které lze přetahovat");
    stage.canvas.style.touchAction = "none";
    const controls = el("div", "sim-controls");
    root.append(controls);

    const field = (r) => st.mu / Math.pow(r, st.n);
    const vCirc = (r) => Math.sqrt(st.mu * Math.pow(r, 1 - st.n));
    const vEsc = (r) => st.n > 1 ? Math.sqrt((2 * st.mu) / ((st.n - 1) * Math.pow(r, st.n - 1))) : Infinity;
    // Poloměr centrálního tělesa (světové jednotky) roste s jeho hmotností; kreslení i kolize používají tutéž hodnotu.
    const bodyRadius = () => Math.max(R_BODY * (0.6 + 0.4 * st.mu), 10 / ((Math.min(stage.w, stage.h) / (2 * VIEW)) || 1));
    const clampPos = () => {
        const r = Math.hypot(st.pos.x, st.pos.y);
        const k = Math.min(VIEW * 0.95, Math.max(bodyRadius() + 0.1, r)) / Math.max(r, 1e-9);
        st.pos.x *= k; st.pos.y *= k;
    };

    const halt = () => { st.vel = null; st.trail = []; st.status = ""; };
    // Za letu se parametry mění plynule (dráha se jen zakřiví jinak); zastavit je potřeba jen v klidu / po dopadu.
    const running = () => st.vel && !st.status;
    // v / v_kruhová platí pro aktuální vzdálenost; směr letu zůstává
    const applySpeed = () => {
        if (!running()) return;
        const r = Math.hypot(st.pos.x, st.pos.y);
        const v = Math.hypot(st.vel[0], st.vel[1]);
        const target = st.vFactor * vCirc(r);
        st.vel = v > 1e-9
            ? [(st.vel[0] / v) * target, (st.vel[1] / v) * target]
            : [(-st.pos.y / r) * target, (st.pos.x / r) * target];
    };
    let rSlider;
    addSlider(controls, { label: "Exponent síly n (F ∝ 1/rⁿ)", min: 1.5, max: 3, step: 0.05, value: st.n, digits: 2, onInput: (v) => { st.n = v; if (!running()) halt(); } });
    addSlider(controls, { label: "Hmotnost centrálního tělesa M", min: 0.3, max: 3, step: 0.1, value: st.mu, digits: 1, onInput: (v) => { st.mu = v; if (!running()) halt(); clampPos(); rSlider.set(Math.hypot(st.pos.x, st.pos.y)); } });
    addSlider(controls, { label: "Hmotnost zkušebního tělesa m", min: 1, max: 10, step: 0.5, value: st.m, digits: 1, onInput: (v) => { st.m = v; } });
    rSlider = addSlider(controls, { label: "Vzdálenost r", min: 0.45, max: 3, step: 0.01, value: Math.hypot(st.pos.x, st.pos.y), digits: 2, onInput: (v) => {
        const r = Math.hypot(st.pos.x, st.pos.y) || 1;
        st.pos.x *= v / r; st.pos.y *= v / r; if (!running()) halt(); clampPos();
    } });
    const vSlider = addSlider(controls, { label: "Rychlost v / v_kruhová", min: 0, max: 1.6, step: 0.01, value: st.vFactor, digits: 2, onInput: (v) => { st.vFactor = v; applySpeed(); } });

    const toggles = el("div", "sim-toggles");
    root.append(toggles);
    addToggle(toggles, "Šipky pole", st.arrows, (v) => { st.arrows = v; });
    addToggle(toggles, "Kružnice stejné velikosti pole", st.rings, (v) => { st.rings = v; });

    const actions = el("div", "sim-actions");
    root.append(actions);
    addButton(actions, "Pustit", () => {
        const r = Math.hypot(st.pos.x, st.pos.y);
        const v = st.vFactor * vCirc(r);
        // rychlost kolmo na spojnici, proti směru hodinových ručiček
        st.home = { x: st.pos.x, y: st.pos.y };
        st.vel = [(-st.pos.y / r) * v, (st.pos.x / r) * v];
        st.trail = [{ x: st.pos.x, y: st.pos.y }];
        st.status = "";
    }, true);
    addButton(actions, "Zastavit a vrátit", () => {
        halt();
        if (st.home) { st.pos = { ...st.home }; rSlider.set(Math.hypot(st.pos.x, st.pos.y)); }
    });
    addButton(actions, "v = v_kruhová", () => { st.vFactor = 1; vSlider.set(1); applySpeed(); });
    addButton(actions, "v = v_úniková", () => {
        const r = Math.hypot(st.pos.x, st.pos.y);
        const k = vEsc(r) / vCirc(r);
        if (Number.isFinite(k)) { st.vFactor = Math.min(1.6, k); vSlider.set(st.vFactor); applySpeed(); }
    });

    const update = addReadouts(root, {
        r: "Vzdálenost r",
        g: "Intenzita pole g = F/m",
        f: "Síla F = m·g",
        vc: "Kruhová rychlost",
        ve: "Úniková rychlost",
        state: "Stav",
    });
    note(root, "Táhni zkušebním tělesem myší nebo prstem. Pro n = 2 (Newton) je dráha uzavřená elipsa; při jiném exponentu se dráha stáčí do růžice a nikdy se neuzavře. Hmotnost m mění sílu, ale ne dráhu.");

    // tažení tělesem
    const toWorld = (ev) => {
        const r = stage.canvas.getBoundingClientRect();
        const s = Math.min(stage.w, stage.h) / (2 * VIEW);
        return { x: (ev.clientX - r.left - stage.w / 2) / s, y: -(ev.clientY - r.top - stage.h / 2) / s };
    };
    let dragging = false;
    const onDown = (ev) => {
        const p = toWorld(ev);
        const s = Math.min(stage.w, stage.h) / (2 * VIEW);
        if (Math.hypot(p.x - st.pos.x, p.y - st.pos.y) * s < 26) {
            dragging = true; halt();
            stage.canvas.setPointerCapture(ev.pointerId);
        }
    };
    const onMove = (ev) => {
        if (!dragging) return;
        const p = toWorld(ev);
        st.pos.x = p.x; st.pos.y = p.y; clampPos();
        rSlider.set(Math.hypot(st.pos.x, st.pos.y));
    };
    const onUp = () => { dragging = false; };
    stage.canvas.addEventListener("pointerdown", onDown);
    stage.canvas.addEventListener("pointermove", onMove);
    stage.canvas.addEventListener("pointerup", onUp);
    stage.canvas.addEventListener("pointercancel", onUp);

    const stop = startLoop(stage, (dt) => {
        if (st.vel && !st.status) advance(dt);
        draw();
    });

    function advance(dt) {
        // 1 jednotka času = oběh po kružnici r=1 trvá 2π/v_c(1); zrychlíme 2× pro živější animaci
        let rem = dt * 2;
        const f = centralDerivs(st.mu, st.n);
        let s = [st.pos.x, st.pos.y, st.vel[0], st.vel[1]];
        while (rem > 1e-9) {
            const r = Math.hypot(s[0], s[1]);
            const v = Math.hypot(s[2], s[3]);
            const h = Math.min(rem, 0.004 * r / Math.max(v, 1e-6), 0.02);
            s = rk4(s, h, f);
            rem -= h;
            const r2 = Math.hypot(s[0], s[1]);
            if (r2 < bodyRadius()) { st.status = "Dopad na těleso"; break; }
            if (r2 > VIEW * 1.6) { st.status = "Uniklo z dosahu"; break; }
        }
        st.pos.x = s[0]; st.pos.y = s[1]; st.vel = [s[2], s[3]];
        const last = st.trail[st.trail.length - 1];
        if (!last || Math.hypot(last.x - s[0], last.y - s[1]) > 0.02) st.trail.push({ x: s[0], y: s[1] });
        if (st.trail.length > 1600) st.trail.shift();
    }

    function draw() {
        const { ctx, w, h, c } = stage;
        ctx.clearRect(0, 0, w, h);
        const s = Math.min(w, h) / (2 * VIEW);
        const X = (x) => w / 2 + x * s, Y = (y) => h / 2 - y * s;

        // kružnice stejné velikosti pole
        if (st.rings) {
            ctx.setLineDash([4, 6]); ctx.lineWidth = 1.2; ctx.strokeStyle = c.faint;
            for (const r of [0.5, 1, 1.5, 2, 2.5, 3]) {
                ctx.beginPath(); ctx.arc(X(0), Y(0), r * s, 0, TAU); ctx.stroke();
                label(ctx, `${fmt(r, 1)}`, X(r * Math.SQRT1_2) + 4, Y(r * Math.SQRT1_2) - 4, c.muted, "left", 11, 500);
            }
            ctx.setLineDash([]);
        }

        // šipky pole (délka roste s intenzitou, ale komprimovaně)
        if (st.arrows) {
            const f1 = field(1);
            for (let gx = -3; gx <= 3; gx += 0.5) for (let gy = -3; gy <= 3; gy += 0.5) {
                const r = Math.hypot(gx, gy);
                if (r < 0.45 || r > VIEW * 0.98) continue;
                const mag = field(r) / f1;
                const len = Math.min(0.42 * s, 0.17 * s * Math.pow(mag, 0.5) + 4);
                const ux = -gx / r, uy = -gy / r;
                ctx.globalAlpha = Math.min(1, 0.25 + 0.4 * Math.pow(mag, 0.5));
                arrow(ctx, X(gx) - ux * len * 0.5, Y(gy) + uy * len * 0.5, X(gx) + ux * len * 0.5, Y(gy) - uy * len * 0.5, c.accent, 1.8);
                ctx.globalAlpha = 1;
            }
        }

        // trajektorie
        if (st.trail.length > 1) {
            ctx.strokeStyle = c.accent2; ctx.lineWidth = 2.5; ctx.lineJoin = "round"; ctx.beginPath();
            st.trail.forEach((p, i) => i ? ctx.lineTo(X(p.x), Y(p.y)) : ctx.moveTo(X(p.x), Y(p.y)));
            ctx.stroke();
        }

        // centrální těleso
        dot(ctx, X(0), Y(0), bodyRadius() * s, c.accentSoft, c.accent);

        // zkušební těleso + síla a rychlost
        const r = Math.hypot(st.pos.x, st.pos.y);
        const px = X(st.pos.x), py = Y(st.pos.y);
        const fMag = field(r) / field(1);
        const fl = Math.min(70, 16 + 26 * Math.sqrt(fMag * st.m));
        arrow(ctx, px, py, px - (st.pos.x / r) * fl, py + (st.pos.y / r) * fl, c.accent, 3);
        if (st.vel) {
            const v = Math.hypot(st.vel[0], st.vel[1]);
            const vl = Math.min(70, 14 + 34 * v);
            arrow(ctx, px, py, px + (st.vel[0] / Math.max(v, 1e-9)) * vl, py - (st.vel[1] / Math.max(v, 1e-9)) * vl, c.accent2, 3);
        }
        dot(ctx, px, py, 9, c.accent2Soft, c.accent2);
        if (!st.vel) label(ctx, "táhni mě", px, py - 20, c.muted, "center", 12, 500);

        const ve = vEsc(r);
        update({
            r: fmt(r, 2),
            g: fmt(field(r), 3),
            f: fmt(field(r) * st.m, 3),
            vc: fmt(vCirc(r), 3),
            ve: Number.isFinite(ve) ? fmt(ve, 3) : "neexistuje",
            state: st.status || (st.vel ? "Letí" : "Čeká na puštění"),
        });
    }

    return {
        dispose: () => {
            stop(); stage.dispose();
            stage.canvas.removeEventListener("pointerdown", onDown);
            stage.canvas.removeEventListener("pointermove", onMove);
            stage.canvas.removeEventListener("pointerup", onUp);
            stage.canvas.removeEventListener("pointercancel", onUp);
        },
    };
}

// ── Entry point used by SimulationExercise.razor ───────────────────────────

const KINDS = { kepler: initKepler, projectile: initProjectile, "gravity-field": initGravityField };

export function init(host, kind, config) {
    const make = KINDS[kind];
    host.replaceChildren();
    if (!make) {
        host.append(el("p", "sim-note", `Neznámý typ simulace: ${kind}`));
        return { dispose() {} };
    }
    let cfg = {};
    try { cfg = typeof config === "string" ? JSON.parse(config) : (config ?? {}); } catch { /* neplatný JSON → výchozí hodnoty */ }
    return make(host, cfg);
}
