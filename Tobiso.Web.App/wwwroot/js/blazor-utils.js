// blazor-utils.js - JavaScript module pro Blazor
let dotNetHelper;
let index, pages, categories;

// Pomocná funkce pro bezpečné volání .NET metod
async function safeInvokeDotNet(methodName, ...args) {
  if (!dotNetHelper) return;
  
  try {
    return await dotNetHelper.invokeMethodAsync(methodName, ...args);
  } catch (error) {
    console.log(`[blazor-utils] Blazor circuit disconnected, ignoring ${methodName} call:`, error);
    return null;
  }
}

export function setDarkModePreferenceExists(value) {
  document.body.dataset.darkModePreferenceExists = value ? "true" : "false";
}

export function setCookieConsent(value) {
  document.documentElement.dataset.cookieConsent = value ?? "";
  // Persist preference via Blazor (safe call — does nothing if disconnected)
  safeInvokeDotNet('SetPreference', 'cookieConsent', value || '');
}

// Set or remove 'dark-mode-preferred' marker class used by Blazor
export function setDarkModePreferred(value) {
  if (value) document.body.classList.add('dark-mode-preferred');
  else document.body.classList.remove('dark-mode-preferred');
}

// Hlavní inicializační funkce
export function initializeApp(dotNetRef) {
  console.log("[blazor-utils] initializeApp called");
  dotNetHelper = dotNetRef;
  
  // Expose dotNetHelper globally for NavMenu to access
  window.__mainLayoutRef = dotNetRef;

  // Inicializace všech funkcionalit
  initDarkMode();
  initScrollLoadingBar();
  initMobileMenu();
  initSearch();
  initCookieConsent();
  initKeyboardShortcuts();
}

// --- Reading JS fallback for when Blazor isn't connected ---
export function initReadingFallback() {
  try {
    const fontSizes = [14,17,20,24,28];
    const lineWidths = ["55ch","75ch","100ch","140ch"];

    window.reading = {
      getState() {
        const font = localStorage.getItem('readingFont') || '';
        const sz = parseInt(localStorage.getItem('readingFontSizeStep'));
        const s = Number.isInteger(sz) ? Math.min(Math.max(sz,0), fontSizes.length-1) : 2;
        const lw = parseInt(localStorage.getItem('readingLineWidthStep'));
        const w = Number.isInteger(lw) ? Math.min(Math.max(lw,0), lineWidths.length-1) : (lineWidths.length-1);
        return { font, s, w };
      },
      saveState(font, s, w) {
        if (font !== undefined) localStorage.setItem('readingFont', font);
        if (s !== undefined) localStorage.setItem('readingFontSizeStep', s.toString());
        if (w !== undefined) localStorage.setItem('readingLineWidthStep', w.toString());
      },
      apply() {
        try {
          const st = this.getState();
          const el = document.getElementById('content');
          if (!el) return;
          const font = st.font;
          const size = fontSizes[st.s];
          const width = lineWidths[st.w];
          if (font && font.length>0) el.style.setProperty('font-family', font, 'important'); else el.style.setProperty('font-family', getComputedStyle(document.documentElement).getPropertyValue('--font-family-serif') || 'serif', 'important');
          el.style.setProperty('--reading-font-size', size + 'px');
          el.style.setProperty('font-size', size + 'px', 'important');
          if (width !== '') { el.style.setProperty('max-width', width, 'important'); el.style.setProperty('margin-left','auto','important'); el.style.setProperty('margin-right','auto','important'); } else { el.style.removeProperty('max-width'); el.style.removeProperty('margin-left'); el.style.removeProperty('margin-right'); }
        } catch(e){console.log(e)}
      },
      setFont(font) { this.saveState(font, undefined, undefined); this.apply(); },
      changeSize(delta) { const st=this.getState(); let n = Math.min(Math.max(st.s+delta,0), fontSizes.length-1); this.saveState(undefined, n, undefined); this.apply(); },
      setWidth(step) { const st=this.getState(); let w = Math.min(Math.max(step,0), lineWidths.length-1); this.saveState(undefined, undefined, w); this.apply(); }
    };
    // Apply initially
    window.reading.apply();
  } catch (e) { console.log('[reading] init failed', e); }
}

// Dark mode funkcionalita
function initDarkMode() {
  const toggleButton = document.getElementById("dark-mode-toggle");
  const toggleButtonMobile = document.getElementById("dark-mode-toggle-mobile");
  const body = document.body;
  const logoElements = document.querySelectorAll(".navImg");

  function enableDarkMode() {
    body.classList.add("dark-mode");
    // V Blazoru nepoužíváme localStorage přímo
    setDarkModePreference(true);
    logoElements.forEach((logo) => {
      logo.src = "https://files.tobiso.com/images/white-logo.png";
      logo.alt = "bílé logo";
    });

    // Notifikace Blazor komponenty
    safeInvokeDotNet('OnDarkModeToggled', true);
  }

  function disableDarkMode() {
    body.classList.remove("dark-mode");
    setDarkModePreference(false);
    logoElements.forEach((logo) => {
      logo.src = "https://files.tobiso.com/images/normal-logo.png";
    });

    // Notifikace Blazor komponenty
    safeInvokeDotNet('OnDarkModeToggled', false);
  }

  // Kontrola uložené preference
  const hasDarkModePreference = () => document.body.dataset.darkModePreferenceExists === 'true';

  if (hasDarkModePreference()) {
    // Existuje uložená preference (může být true nebo false)
    if (getDarkModePreference()) {
      enableDarkMode();
      if (toggleButton) toggleButton.checked = true;
      if (toggleButtonMobile) toggleButtonMobile.checked = true;
    } else {
      // Explicitně false: ujisti se, že třída není aplikovaná a switche jsou off
      body.classList.remove('dark-mode');
      if (toggleButton) toggleButton.checked = false;
      if (toggleButtonMobile) toggleButtonMobile.checked = false;
      safeInvokeDotNet('OnDarkModeToggled', false);
    }
  } else if (!hasDarkModePreference()) {
    // Žádná uložená preference -> použij systémové téma a poslouchej změny
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    function applySystemTheme(isDark) {
        if (isDark) {
          // Aplikuj pouze třídu, neukládej do preferencí
          document.body.classList.add('dark-mode');
          logoElements.forEach((logo) => {
            logo.src = "https://files.tobiso.com/images/white-logo.png";
            logo.alt = "bílé logo";
          });
          if (toggleButton) toggleButton.checked = true;
          if (toggleButtonMobile) toggleButtonMobile.checked = true;
          safeInvokeDotNet('OnDarkModeToggled', true);
        } else {
          document.body.classList.remove('dark-mode');
          logoElements.forEach((logo) => {
            logo.src = "https://files.tobiso.com/images/normal-logo.png";
          });
          if (toggleButton) toggleButton.checked = false;
          if (toggleButtonMobile) toggleButtonMobile.checked = false;
          safeInvokeDotNet('OnDarkModeToggled', false);
        }
    }

    // Initialní nastavení podle systému
    applySystemTheme(mediaQuery.matches);

    // Poslouchej změny systémového tématu, ale jen když uživatel nemá preference
    const mqListener = (e) => {
      if (!hasDarkModePreference()) {
        applySystemTheme(e.matches);
      }
    };

    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', mqListener);
    } else if (mediaQuery.addListener) {
      mediaQuery.addListener(mqListener);
    }
  }

  // Event listenery
  if (toggleButton) {
    toggleButton.addEventListener("change", function () {
      if (body.classList.contains("dark-mode")) {
        disableDarkMode();
      } else {
        enableDarkMode();
      }
    });
  }

  if (toggleButtonMobile) {
    toggleButtonMobile.addEventListener("change", function () {
      if (body.classList.contains("dark-mode")) {
        disableDarkMode();
      } else {
        enableDarkMode();
      }
    });
  }
}

// Scroll loading bar
function initScrollLoadingBar() {
  const loadingBar = document.querySelector(".loading-bar");
  if (!loadingBar) return;

  function updateLoadingBar() {
    const scrollTop = document.documentElement.scrollTop || document.body.scrollTop;
    const scrollHeight = document.documentElement.scrollHeight || document.body.scrollHeight;
    const clientHeight = document.documentElement.clientHeight;
    const scrollPercent = (scrollTop / (scrollHeight - clientHeight)) * 100;
    loadingBar.style.width = scrollPercent + "%";
  }

  window.addEventListener("scroll", updateLoadingBar);
  updateLoadingBar();
}

// Mobile menu
function initMobileMenu() {
  const toggleButton = document.querySelector(".navbar-toggler");
  const closeButton = document.querySelector(".close-menu");
  const mobileMenu = document.querySelector(".mobile-menu");

  if (!toggleButton || !closeButton || !mobileMenu) return;

  function toggleMenu() {
    mobileMenu.classList.toggle("show");
    document.body.style.overflow = mobileMenu.classList.contains("show") ? "hidden" : "";
  }

  toggleButton.addEventListener("click", toggleMenu);
  closeButton.addEventListener("click", toggleMenu);

  const menuLinks = mobileMenu.querySelectorAll(".nav-link");
  menuLinks.forEach((link) => {
    link.addEventListener("click", toggleMenu);
  });
  
}

// Search funkcionalita
function initSearch() {
  loadSearchIndex();
  initSearchModal();
}

async function loadSearchIndex() {
  console.log("[blazor-utils] loadSearchIndex started - v3");
  try {
    console.log("[blazor-utils] About to fetch /api/Pages");
    const [postsResponse, categoriesResponse] = await Promise.all([
      fetch("/api/Pages"),
      fetch("/api/Pages/categories")
    ]);
    console.log("[blazor-utils] Fetch completed, parsing JSON");
    const data = await postsResponse.json();
    const categoriesData = await categoriesResponse.json();
    console.log("[blazor-utils] JSON parsed, mapping data");

    // Mapa categoryId -> celý objekt kategorie
    const categoryMap = {};
    categoriesData.forEach(cat => { categoryMap[cat.id] = cat; });

    // Sestavení plné cesty pro kategorii (např. "Fyzika › Mechanika")
    function buildCategoryPath(cat, depth = 0) {
      if (!cat || depth > 10) return "";
      if (cat.parentId == null) return cat.name;
      const parent = categoryMap[cat.parentId];
      const parentPath = buildCategoryPath(parent, depth + 1);
      return parentPath ? `${parentPath} › ${cat.name}` : cat.name;
    }

    // Kategorie jako samostatné výsledky
    categories = categoriesData.map(cat => {
      const path = buildCategoryPath(cat);
      // Cesta bez posledního segmentu (tj. nadřazené kategorie)
      const pathAbove = cat.parentId != null
        ? buildCategoryPath(categoryMap[cat.parentId])
        : "";
      return {
        url: `/categories/${cat.id}`,
        title: cat.name,
        fullPath: path,
        pathAbove: pathAbove
      };
    });

    // Transformace dat pro vyhledávání
    pages = data.map(post => ({
      url: `/post/${post.id}`,
      title: post.title,
      content: post.content,
      categoryName: post.categoryId != null ? (categoryMap[post.categoryId]?.name ?? "") : ""
    }));

    console.log(`[blazor-utils] Loaded ${pages.length} pages and ${categories.length} categories for search - v3 SUCCESS`);
  } catch (error) {
    console.error("Chyba při načítání indexu (v3):", error);
  }
}

function initSearchModal() {
  const modal = document.getElementById("searchModal");
  const closeBtn = document.getElementById("closeSearch");
  const searchInput = document.getElementById("search");
  const openBtns = document.querySelectorAll("#openSearch");
  const resultsContainer = document.getElementById("results");
  const emptyState = document.getElementById("search-empty");
  const noResultsState = document.getElementById("search-no-results");

  if (!modal || !searchInput) return;

  let selectedIndex = -1;
  let currentResults = [];
  let keyboardMode = false; // Flag pro rozlišení klávesnice vs myš

  openBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      modal.classList.add("active");
      toggleScrollLock(true);
      setTimeout(() => searchInput.focus(), 100);
      showEmptyState();
      resetSelection();
    });
  });

  if (closeBtn) {
    closeBtn.addEventListener("click", closeSearchModal);
  }

  // Kliknutí na backdrop zavře modal
  modal.addEventListener("click", function (event) {
    if (event.target === modal || event.target.classList.contains("search-backdrop")) {
      closeSearchModal();
    }
  });

  searchInput.addEventListener("input", function () {
    const query = searchInput.value.trim();
    resetSelection();
    
    if (query.length === 0) {
      showEmptyState();
    } else if (query.length >= 2) {
      performSearch(query);
    } else {
      hideAllStates();
    }
  });

  // Klávesové zkratky pro navigaci - pouze jeden event listener
  searchInput.addEventListener("keydown", function (event) {
    const resultElements = document.querySelectorAll(".search-result");
    
    if (event.key === "ArrowDown") {
      event.preventDefault();
      keyboardMode = true;
      disableMouseEvents();
      navigateResults(1, resultElements);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      keyboardMode = true;
      disableMouseEvents();
      navigateResults(-1, resultElements);
    } else if (event.key === "Enter") {
      event.preventDefault();
      if (selectedIndex >= 0 && currentResults[selectedIndex]) {
        window.location.href = currentResults[selectedIndex].url;
      }
    }
  });

  function resetSelection() {
    selectedIndex = -1;
    keyboardMode = false;
    enableMouseEvents();
    // Odstranit všechny selected třídy
    document.querySelectorAll(".search-result.selected").forEach(el => {
      el.classList.remove("selected");
    });
  }

  function disableMouseEvents() {
    // Přidej třídu pro CSS, která vypne hover efekty
    if (resultsContainer) {
      resultsContainer.classList.add("keyboard-navigation");
    }
  }

  function enableMouseEvents() {
    // Odeber třídu pro povolení hover efektů
    if (resultsContainer) {
      resultsContainer.classList.remove("keyboard-navigation");
    }
  }

  function showEmptyState() {
    hideAllStates();
    resetSelection();
    if (emptyState) emptyState.style.display = "flex";
  }

  function showNoResultsState() {
    hideAllStates();
    resetSelection();
    if (noResultsState) noResultsState.style.display = "flex";
  }

  function hideAllStates() {
    if (resultsContainer) resultsContainer.innerHTML = "";
    if (emptyState) emptyState.style.display = "none";
    if (noResultsState) noResultsState.style.display = "none";
    resetSelection();
  }

  function navigateResults(direction, resultElements) {
    if (resultElements.length === 0) return;

    // Odebrat všechny selected třídy
    resultElements.forEach(el => el.classList.remove("selected"));

    // Vypočítat nový index
    selectedIndex += direction;
    if (selectedIndex < 0) selectedIndex = resultElements.length - 1;
    if (selectedIndex >= resultElements.length) selectedIndex = 0;

    // Přidat selected třídu pouze k aktuálnímu elementu
    if (resultElements[selectedIndex]) {
      resultElements[selectedIndex].classList.add("selected");
      resultElements[selectedIndex].scrollIntoView({ block: "nearest" });
    }
  }

  // Uložit referenci na funkce pro použití v ostatních částech
  window.searchModalFunctions = {
    showEmptyState,
    showNoResultsState,
    hideAllStates,
    setCurrentResults: (results) => { 
      currentResults = results; 
      resetSelection();
    },
    enableMouseEvents,
    disableMouseEvents
  };
}

function closeSearchModal() {
  const modal = document.getElementById("searchModal");
  const searchInput = document.getElementById("search");

  modal.classList.remove("active");
  toggleScrollLock(false);
  searchInput.value = "";
  
  if (window.searchModalFunctions) {
    window.searchModalFunctions.hideAllStates();
    window.searchModalFunctions.setCurrentResults([]);
  }
}

async function performSearch(query) {
  if (!pages || query.length < 2) return;

  const categoryResults = searchCategories(query);
  const pageResults = searchPages(query);
  const allResults = [...categoryResults, ...pageResults];
  displaySearchResults(categoryResults, pageResults);
  
  if (window.searchModalFunctions) {
    window.searchModalFunctions.setCurrentResults(allResults);
  }

  // Notifikace Blazor komponenty
  await safeInvokeDotNet('OnSearchPerformed', query, JSON.stringify(allResults));
}

function searchCategories(query) {
  if (!categories) return [];
  const normalizedQuery = normalizeText(query);
  return categories
    .filter(cat => normalizeText(cat.title).includes(normalizedQuery))
    .map(cat => ({
      title: cat.title,
      url: cat.url,
      score: 20,
      highlightedTerm: "",
      foundInTitle: true,
      foundInContent: false,
      categoryName: "",
      isCategory: true,
      pathAbove: cat.pathAbove
    }));
}

function searchPages(query) {
  const results = [];
  const normalizedQuery = normalizeText(query);

  pages.forEach((page) => {
    let score = 0;
    let foundInTitle = false;
    let foundInContent = false;
    let highlightedTerm = "";

    if (normalizeText(page.title).includes(normalizedQuery)) {
      score += 10;
      foundInTitle = true;
    }

    if (page.categoryName && normalizeText(page.categoryName).includes(normalizedQuery)) {
      score += 7;
    }

    if (page.content && normalizeText(page.content).includes(normalizedQuery)) {
      score += 5;
      foundInContent = true;
      highlightedTerm = findAndHighlightTerm(page.content, query);
    }

    if (score > 0) {
      results.push({
        title: page.title,
        url: page.url,
        score: score,
        highlightedTerm: highlightedTerm,
        foundInTitle: foundInTitle,
        foundInContent: foundInContent,
        categoryName: page.categoryName
      });
    }
  });

  results.sort((a, b) => b.score - a.score);
  return results;
}

function displaySearchResults(categoryResults, pageResults) {
  const resultsContainer = document.getElementById("results");
  if (!resultsContainer || !window.searchModalFunctions) return;

  if (categoryResults.length === 0 && pageResults.length === 0) {
    window.searchModalFunctions.showNoResultsState();
    return;
  }

  window.searchModalFunctions.hideAllStates();

  let globalIndex = 0;
  const maxTotal = 8;
  const maxCategories = Math.min(categoryResults.length, 3);
  const maxPages = Math.min(pageResults.length, maxTotal - maxCategories);

  function appendResultItem(result) {
    const resultItem = document.createElement("a");
    resultItem.classList.add("search-result");
    resultItem.href = result.url;
    resultItem.dataset.url = result.url;
    resultItem.dataset.index = globalIndex++;

    let snippetText = result.highlightedTerm;
    if (result.isCategory) {
      snippetText = result.pathAbove ? escapeHtml(result.pathAbove) : "Kořenová kategorie";
    } else if (result.foundInTitle && !result.foundInContent) {
      snippetText = "Shoda v názvu";
    } else if (!snippetText) {
      snippetText = "Bez náhledu obsahu";
    }

    const categoryBadge = result.categoryName
      ? `<span class="search-category">${escapeHtml(result.categoryName)}</span>`
      : "";
    const typeBadge = result.isCategory
      ? `<span class="search-category search-category--type"><i class="bi bi-folder"></i> Kategorie</span>`
      : "";

    resultItem.innerHTML = `
      <div class="result-title">${escapeHtml(result.title)}${typeBadge}${categoryBadge}</div>
      <p class="search-snippet">${snippetText}</p>
    `;

    resultItem.addEventListener("click", function (event) {
      event.preventDefault();
      window.location.href = result.url;
    });

    resultItem.addEventListener("mouseenter", function () {
      const container = document.getElementById("results");
      if (!container || container.classList.contains("keyboard-navigation")) return;
      document.querySelectorAll(".search-result.selected").forEach(el => el.classList.remove("selected"));
      resultItem.classList.add("selected");
    });

    resultItem.addEventListener("mousemove", function () {
      if (window.searchModalFunctions) window.searchModalFunctions.enableMouseEvents();
    });

    resultsContainer.appendChild(resultItem);
  }

  function appendGroupLabel(label) {
    const el = document.createElement("div");
    el.classList.add("search-group-label");
    el.textContent = label;
    resultsContainer.appendChild(el);
  }

  if (maxCategories > 0) {
    appendGroupLabel("Kategorie");
    categoryResults.slice(0, maxCategories).forEach(appendResultItem);
  }

  if (maxPages > 0) {
    appendGroupLabel("Články");
    pageResults.slice(0, maxPages).forEach(appendResultItem);
  }

  const totalShown = maxCategories + maxPages;
  const totalAll = categoryResults.length + pageResults.length;
  if (totalAll > totalShown) {
    const moreResults = document.createElement("div");
    moreResults.classList.add("search-more-results");
    moreResults.innerHTML = `<p>A dalších ${totalAll - totalShown} výsledků...</p>`;
    resultsContainer.appendChild(moreResults);
  }
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// Cookie consent
function waitForGtag(callback, maxTries = 20, interval = 200) {
  let tries = 0;
  function check() {
    if (typeof gtag === "function") {
      callback();
    } else if (tries < maxTries) {
      tries++;
      setTimeout(check, interval);
    } else {
      console.log("[blazor-utils] gtag still not defined after waiting");
    }
  }
  check();
}

function initCookieConsent() {
  console.log("[blazor-utils] initCookieConsent called");
  const consent = getCookieConsent();
  // Nastavení Google Consent Mode při každém načítání stránky
  waitForGtag(() => {
    const analyticsConsent = (consent === "accepted") ? "granted" : "denied";
    gtag("consent", "update", { analytics_storage: analyticsConsent });
    console.log("[blazor-utils] Google running with consent:", analyticsConsent);
  });
  if (!consent) {
    setTimeout(() => {
      console.log("[blazor-utils] showCookieBanner called");
      showCookieBanner();
    }, 3000);
  }
}

function showCookieBanner() {
  console.log("[blazor-utils] showCookieBanner executed");
  const banner = document.getElementById("cookie-consent");
  if (banner) {
    banner.style.display = "block";
    banner.classList.add("show");
  } else {
    console.log("[blazor-utils] Cookie banner element not found");
  }
}

function hideCookieBanner() {
  const banner = document.getElementById("cookie-consent");
  if (banner) {
    banner.style.display = "none";
    banner.classList.remove("show");
  }
}

// Keyboard shortcuts
function initKeyboardShortcuts() {
  window.addEventListener("keydown", function (event) {
    const modal = document.getElementById("searchModal");
    const isModalOpen = modal && modal.classList.contains("active");
    
    if (event.key === "Escape" && isModalOpen) {
      closeSearchModal();
      return;
    }

    const activeElement = document.activeElement.tagName.toLowerCase();
    const isTyping = activeElement === "input" || activeElement === "textarea" || activeElement === "select";

    // Otevření vyhledávání klávesou K (jen když není modal otevřený)
    if (event.key.toLowerCase() === "k" && !isTyping && !isModalOpen) {
      event.preventDefault();
      event.stopPropagation();
      const searchInput = document.getElementById("search");
      if (modal && searchInput) {
        modal.classList.add("active");
        toggleScrollLock(true);
        setTimeout(() => searchInput.focus(), 100);
        if (window.searchModalFunctions) {
          window.searchModalFunctions.showEmptyState();
        }
      }
    }

    // Cmd/Ctrl + K pro otevření vyhledávání
    if (event.key.toLowerCase() === "k" && (event.ctrlKey || event.metaKey) && !isModalOpen) {
      event.preventDefault();
      event.stopPropagation();
      const searchInput = document.getElementById("search");
      if (modal && searchInput) {
        modal.classList.add("active");
        toggleScrollLock(true);
        setTimeout(() => searchInput.focus(), 100);
        if (window.searchModalFunctions) {
          window.searchModalFunctions.showEmptyState();
        }
      }
    }
  });
}

// Utility funkce
function normalizeText(text) {
  if (!text) return "";
  return text
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();
}

function findAndHighlightTerm(content, query) {
  if (!content) return "";

  const normalizedQuery = normalizeText(query);
  const terms = content.split(",").map((term) => term.trim());

  for (let term of terms) {
    if (normalizeText(term).includes(normalizedQuery)) {
      const normalizedTerm = normalizeText(term);
      const queryIndex = normalizedTerm.indexOf(normalizedQuery);

      if (queryIndex !== -1) {
        const prefix = term.substring(0, queryIndex);
        const match = term.substring(queryIndex, queryIndex + query.length);
        const suffix = term.substring(queryIndex + query.length);

        return prefix + "<strong>" + match + "</strong>" + suffix;
      }
      return term;
    }
  }
  return "";
}

function toggleScrollLock(lock) {
  if (lock) {
    document.body.classList.add("scroll-lock");
  } else {
    document.body.classList.remove("scroll-lock");
  }
}

// Exportované funkce pro volání z Blazoru
export function scrollToTop() {
  window.scrollTo({ top: 0, behavior: "smooth" });
}

export function goBack() {
  window.history.back();
}

export function acceptCookies() {
  console.log("Cookies accepted");
  setCookieConsent("accepted");
  hideCookieBanner();
  document.documentElement.dataset.cookieConsent = "accepted";
  waitForGtag(() => {
    gtag("consent", "update", { analytics_storage: "granted" });
    if (typeof loadGoogleAnalytics === "function") {
      loadGoogleAnalytics();
    }
  });
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('OnCookieConsentChanged', "accepted");
    console.log("Cookies finally accepted");
  }
}

export function declineCookies() {
  console.log("Cookies DENIED");
  setCookieConsent("declined");
  hideCookieBanner();
  document.documentElement.dataset.cookieConsent = "declined";
  waitForGtag(() => {
    gtag("consent", "update", { analytics_storage: "denied" });
  });
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('OnCookieConsentChanged', "declined");
  }
}

export function removeCookieConsent() {
  setCookieConsent(null);
  alert("Váš souhlas byl odebrán a data cookies byla vymazána.");
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('OnCookieConsentChanged', "removed");
  }
}

// Funkce pro práci s preferencemi (místo localStorage)
// Tyto funkce budou volat Blazor API pro uložení dat
function setDarkModePreference(isDark) {
  safeInvokeDotNet('SetPreference', 'darkMode', isDark.toString());
}

function getDarkModePreference() {
  // Tato hodnota by měla být předána z Blazoru při inicializaci
  return document.body.classList.contains("dark-mode-preferred");
}

// ── Image Lightbox ──
export function initImageLightbox(containerId) {
  const container = document.getElementById(containerId);
  if (!container) return;

  // Avoid double-binding
  container.querySelectorAll('img[data-lightbox-bound]').forEach(img => {
    img.removeAttribute('data-lightbox-bound');
  });

  container.querySelectorAll('img').forEach(img => {
    if (img.getAttribute('data-lightbox-bound')) return;
    img.setAttribute('data-lightbox-bound', '1');
    img.addEventListener('click', e => {
      e.stopPropagation();

      // Read caption and source from data attributes set by C# PreprocessImageGroups
      const caption = img.dataset.caption || '';
      const source  = img.dataset.source  || '';

      openLightbox(img.src, img.alt, caption, source);
    });
  });
}

// Initialize delegated click handler for <a data-person-name="..."> links inside
// dynamically rendered content. We accept an optional DotNetObjectReference so the
// handler can call back into the specific Blazor component instance; otherwise it
// falls back to the global DotNet.invokeMethodAsync (static JSInvokable).
export function initPersonLinkHandler(dotNetRef) {
  try {
    if (window.__personLinkHandlerBound) return;
    window.__personLinkHandlerBound = true;

    document.addEventListener('click', function (e) {
      try {
        var tgt = e.target;
        if (!tgt || !tgt.closest) return;
        var a = tgt.closest('[data-person-name]');
        if (!a) return;
        e.preventDefault();
        var raw = a.getAttribute('data-person-name') || (a.dataset && a.dataset.personName) || '';
        var name = raw ? decodeURIComponent(raw) : '';
        if (!name) return;
        if (dotNetRef && typeof dotNetRef.invokeMethodAsync === 'function') {
          // Prefer calling an instance method on the passed DotNetObjectReference.
          dotNetRef.invokeMethodAsync('OpenPersonByName', name);
        } else if (window.__postDetailRef && typeof window.__postDetailRef.invokeMethodAsync === 'function') {
          window.__postDetailRef.invokeMethodAsync('OpenPersonByName', name);
        } else if (window.DotNet && typeof DotNet.invokeMethodAsync === 'function') {
          // Fallback to static JSInvokable on the assembly if instance refs are not available.
          DotNet.invokeMethodAsync('Tobiso.Web.App', 'TriggerPersonByName', name);
        }
      } catch (err) { console && console.log && console.log('[initPersonLinkHandler] click handler error', err); }
    }, false);

    // Accessibility: support keyboard activation for focused anchors
    document.addEventListener('keydown', function (e) {
      try {
    if (e.key !== 'Enter' && e.key !== ' ') return;
        var el = document.activeElement;
        if (!el || !el.matches) return;
        if (!el.matches('[data-person-name]')) return;
        e.preventDefault();
        var raw = el.getAttribute('data-person-name') || (el.dataset && el.dataset.personName) || '';
        var name = raw ? decodeURIComponent(raw) : '';
        if (!name) return;
        if (dotNetRef && typeof dotNetRef.invokeMethodAsync === 'function') {
          dotNetRef.invokeMethodAsync('TriggerPersonByName', name);
        } else if (window.__postDetailRef && typeof window.__postDetailRef.invokeMethodAsync === 'function') {
          window.__postDetailRef.invokeMethodAsync('TriggerPersonByName', name);
        } else if (window.DotNet && typeof DotNet.invokeMethodAsync === 'function') {
          DotNet.invokeMethodAsync('Tobiso.Web.App', 'TriggerPersonByName', name);
        }
      } catch (err) { console && console.log && console.log('[initPersonLinkHandler] keydown handler error', err); }
    }, false);
  } catch (err) { console && console.log && console.log('[initPersonLinkHandler] init error', err); }
}

function openLightbox(src, alt, caption, source) {
  // Remove any existing overlay
  closeLightbox();

  const overlay = document.createElement('div');
  overlay.className = 'img-lightbox-overlay';

  const closeBtn = document.createElement('span');
  closeBtn.className = 'img-lightbox-close';
  closeBtn.innerHTML = '&times;';
  closeBtn.title = 'Zavřít';

  const wrapper = document.createElement('div');
  wrapper.className = 'img-lightbox-wrapper';

  const imgEl = document.createElement('img');
  imgEl.src = src;
  imgEl.alt = alt || '';

  wrapper.appendChild(imgEl);

  // Meta row: caption left, source right — plain text only (strip markdown)
  const captionPlain = stripMarkdown(caption || '');
  const sourcePlain  = stripMarkdown(source  || '');

  if (captionPlain || sourcePlain) {
    const meta = document.createElement('div');
    meta.className = 'img-lightbox-meta';

    if (captionPlain) {
      const cap = document.createElement('span');
      cap.className = 'img-lightbox-caption';
      cap.textContent = captionPlain;
      meta.appendChild(cap);
    }

    if (sourcePlain) {
      const srcEl = document.createElement('span');
      srcEl.className = 'img-lightbox-source';
      srcEl.textContent = sourcePlain;
      meta.appendChild(srcEl);
    }

    wrapper.appendChild(meta);
  }

  overlay.appendChild(closeBtn);
  overlay.appendChild(wrapper);
  document.body.appendChild(overlay);

  // Close on overlay or img click (not on meta/caption text)
  overlay.addEventListener('click', e => {
    if (e.target === overlay || e.target === closeBtn || e.target === imgEl) closeLightbox();
  });
  closeBtn.addEventListener('click', () => closeLightbox());

  // ESC key
  window.__lightboxEscHandler = e => {
    if (e.key === 'Escape') closeLightbox();
  };
  document.addEventListener('keydown', window.__lightboxEscHandler);
}

function closeLightbox() {
  const existing = document.querySelector('.img-lightbox-overlay');
  if (existing) existing.remove();
  if (window.__lightboxEscHandler) {
    document.removeEventListener('keydown', window.__lightboxEscHandler);
    delete window.__lightboxEscHandler;
  }
}

// Strip markdown syntax to plain text for lightbox labels
function stripMarkdown(text) {
  if (!text) return '';
  return text
    // [label](url) → label
    .replace(/\[([^\]]+)\]\([^)]*\)/g, '$1')
    // ![alt](url) → alt
    .replace(/!\[([^\]]*?)\]\([^)]*\)/g, '$1')
    // **bold** or __bold__
    .replace(/\*{1,2}([^*]+)\*{1,2}/g, '$1')
    .replace(/_{1,2}([^_]+)_{1,2}/g, '$1')
    // `code`
    .replace(/`([^`]+)`/g, '$1')
    // leading markup like Zdroj: Autor:
    .trim();
}

// Note: cookie preference persistence is handled in exported `setCookieConsent` above.

function getCookieConsent() {
  // Tato hodnota by měla být předána z Blazoru při inicializaci
  return document.documentElement.dataset.cookieConsent;
}
