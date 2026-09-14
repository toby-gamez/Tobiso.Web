// Pomodoro widget for the article "Nástroje k článku" panel. Fully client-side
// (no server round-trips for the per-second tick) and appended to <body> instead
// of into a Blazor-owned subtree, so - since Blazor Server does client-side
// routing for in-app navigation - the widget and its running timer survive
// navigating to a different article instead of resetting on every page.
const POMODORO_KEY = 'tobiso-pomodoro-state';
const POMODORO_DEFAULT_WORK_MIN = 25;
const POMODORO_DEFAULT_BREAK_MIN = 5;
const POMODORO_MIN_MIN = 1;
const POMODORO_MAX_MIN = 90;

let pomodoroState = null;
let pomodoroInterval = null;
let pomodoroEl = null;

function clampMinutes(value, fallback) {
    const n = parseInt(value, 10);
    if (Number.isNaN(n)) return fallback;
    return Math.min(POMODORO_MAX_MIN, Math.max(POMODORO_MIN_MIN, n));
}

function loadPomodoroState() {
    try {
        const raw = localStorage.getItem(POMODORO_KEY);
        if (raw) {
            const parsed = JSON.parse(raw);
            if (typeof parsed.remaining === 'number' && (parsed.mode === 'work' || parsed.mode === 'break')) {
                return {
                    mode: parsed.mode,
                    remaining: parsed.remaining,
                    workMin: clampMinutes(parsed.workMin, POMODORO_DEFAULT_WORK_MIN),
                    breakMin: clampMinutes(parsed.breakMin, POMODORO_DEFAULT_BREAK_MIN),
                    running: false,
                };
            }
        }
    } catch (e) { /* ignore */ }
    return { mode: 'work', remaining: POMODORO_DEFAULT_WORK_MIN * 60, workMin: POMODORO_DEFAULT_WORK_MIN, breakMin: POMODORO_DEFAULT_BREAK_MIN, running: false };
}

function savePomodoroState() {
    try { localStorage.setItem(POMODORO_KEY, JSON.stringify(pomodoroState)); } catch (e) { /* ignore */ }
}

function formatPomodoroTime(totalSeconds) {
    const m = Math.floor(totalSeconds / 60);
    const s = totalSeconds % 60;
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

function renderPomodoro() {
    if (!pomodoroEl) return;
    const timeEl = pomodoroEl.querySelector('.pomodoro-time');
    const modeEl = pomodoroEl.querySelector('.pomodoro-mode');
    const toggleBtn = pomodoroEl.querySelector('.pomodoro-toggle');
    const workInput = pomodoroEl.querySelector('.pomodoro-work-min');
    const breakInput = pomodoroEl.querySelector('.pomodoro-break-min');
    if (timeEl) timeEl.textContent = formatPomodoroTime(pomodoroState.remaining);
    if (modeEl) modeEl.textContent = pomodoroState.mode === 'work' ? 'Soustředění' : 'Přestávka';
    pomodoroEl.classList.toggle('is-break', pomodoroState.mode === 'break');
    if (toggleBtn) {
        toggleBtn.setAttribute('data-lucide', pomodoroState.running ? 'pause' : 'play');
        toggleBtn.removeAttribute('data-lucide-rendered');
    }
    if (workInput && document.activeElement !== workInput) workInput.value = pomodoroState.workMin;
    if (breakInput && document.activeElement !== breakInput) breakInput.value = pomodoroState.breakMin;
    window.initLucide?.();
}

function tickPomodoro() {
    if (!pomodoroState.running) return;
    pomodoroState.remaining--;
    if (pomodoroState.remaining <= 0) {
        pomodoroState.mode = pomodoroState.mode === 'work' ? 'break' : 'work';
        pomodoroState.remaining = (pomodoroState.mode === 'work' ? pomodoroState.workMin : pomodoroState.breakMin) * 60;
    }
    savePomodoroState();
    renderPomodoro();
}

function setDuration(kind, minutes) {
    const clamped = clampMinutes(minutes, kind === 'work' ? pomodoroState.workMin : pomodoroState.breakMin);
    pomodoroState[kind === 'work' ? 'workMin' : 'breakMin'] = clamped;

    // If the timer isn't mid-countdown for this mode, reflect the new duration right away
    // instead of only applying it the next time this mode starts.
    if (!pomodoroState.running && pomodoroState.mode === kind) {
        pomodoroState.remaining = clamped * 60;
    }

    savePomodoroState();
    renderPomodoro();
}

function buildPomodoroWidget() {
    const el = document.createElement('div');
    el.className = 'pomodoro-widget';
    el.innerHTML = `
        <div class="pomodoro-header">
            <span class="pomodoro-mode"></span>
            <button type="button" class="pomodoro-close" aria-label="Zavřít Pomodoro"><i data-lucide="x"></i></button>
        </div>
        <div class="pomodoro-time"></div>
        <div class="pomodoro-actions">
            <button type="button" class="pomodoro-toggle" aria-label="Spustit nebo pozastavit" data-lucide="play"></button>
            <button type="button" class="pomodoro-reset" aria-label="Resetovat"><i data-lucide="rotate-ccw"></i></button>
        </div>
        <div class="pomodoro-settings">
            <label>Práce
                <input type="number" class="pomodoro-work-min" min="${POMODORO_MIN_MIN}" max="${POMODORO_MAX_MIN}" step="1" />
            </label>
            <label>Přestávka
                <input type="number" class="pomodoro-break-min" min="${POMODORO_MIN_MIN}" max="${POMODORO_MAX_MIN}" step="1" />
            </label>
        </div>
    `;

    el.querySelector('.pomodoro-toggle').addEventListener('click', () => {
        pomodoroState.running = !pomodoroState.running;
        savePomodoroState();
        renderPomodoro();
    });

    el.querySelector('.pomodoro-reset').addEventListener('click', () => {
        pomodoroState.running = false;
        pomodoroState.mode = 'work';
        pomodoroState.remaining = pomodoroState.workMin * 60;
        savePomodoroState();
        renderPomodoro();
    });

    el.querySelector('.pomodoro-close').addEventListener('click', closePomodoroWidget);

    el.querySelector('.pomodoro-work-min').addEventListener('change', (e) => setDuration('work', e.target.value));
    el.querySelector('.pomodoro-break-min').addEventListener('change', (e) => setDuration('break', e.target.value));

    return el;
}

function closePomodoroWidget() {
    if (pomodoroInterval) {
        clearInterval(pomodoroInterval);
        pomodoroInterval = null;
    }
    if (pomodoroEl) {
        pomodoroEl.remove();
        pomodoroEl = null;
    }
}

window.togglePomodoroWidget = () => {
    if (pomodoroEl) {
        closePomodoroWidget();
        return;
    }

    pomodoroState = loadPomodoroState();
    pomodoroEl = buildPomodoroWidget();
    document.body.appendChild(pomodoroEl);
    renderPomodoro();
    pomodoroInterval = setInterval(tickPomodoro, 1000);
};
