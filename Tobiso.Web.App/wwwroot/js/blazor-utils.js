// blazor-utils.js - JavaScript module pro Blazor
let dotNetHelper;
let index, pages;

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

// Hlavní inicializační funkce
export function initializeApp(dotNetRef) {
  console.log("[blazor-utils] initializeApp called");
  dotNetHelper = dotNetRef;

  // Inicializace všech funkcionalit
  initDarkMode();
  initScrollLoadingBar();
  initMobileMenu();
  initSearch();
  initCookieConsent();
  initKeyboardShortcuts();
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
    const response = await fetch("/api/Pages");
    console.log("[blazor-utils] Fetch completed, parsing JSON");
    const data = await response.json();
    console.log("[blazor-utils] JSON parsed, mapping data");
    // Transformace dat pro vyhledávání
    pages = data.map(post => ({
      url: `/post/${post.id}`,
      title: post.title,
      content: post.content
    }));

    console.log(`[blazor-utils] Loaded ${pages.length} pages for search - v3 SUCCESS`);
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

  const results = searchPages(query);
  displaySearchResults(results);
  
  if (window.searchModalFunctions) {
    window.searchModalFunctions.setCurrentResults(results);
  }

  // Notifikace Blazor komponenty
  await safeInvokeDotNet('OnSearchPerformed', query, JSON.stringify(results));
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
        foundInContent: foundInContent
      });
    }
  });

  results.sort((a, b) => b.score - a.score);
  return results;
}

function displaySearchResults(results) {
  const resultsContainer = document.getElementById("results");
  if (!resultsContainer || !window.searchModalFunctions) return;

  if (results.length === 0) {
    window.searchModalFunctions.showNoResultsState();
    return;
  }

  window.searchModalFunctions.hideAllStates();

  results.slice(0, 8).forEach((result, index) => {
    const resultItem = document.createElement("a");
    resultItem.classList.add("search-result");
    resultItem.href = result.url;
    resultItem.dataset.url = result.url;
    resultItem.dataset.index = index; // Přidat index pro snadnější navigaci

    let snippetText = result.highlightedTerm;
    if (result.foundInTitle && !result.foundInContent) {
      snippetText = "Shoda pouze v názvu";
    } else if (!snippetText) {
      snippetText = "Bez náhledu obsahu";
    }

    resultItem.innerHTML = `
      <div class="result-title">${escapeHtml(result.title)}</div>
      <p class="search-snippet">${snippetText}</p>
    `;

    resultItem.addEventListener("click", function (event) {
      event.preventDefault();
      window.location.href = result.url;
    });

    // Hover efekty pro myš - pouze pokud není keyboard mode
    resultItem.addEventListener("mouseenter", function () {
      const container = document.getElementById("results");
      if (!container || container.classList.contains("keyboard-navigation")) {
        return; // Neprovádět hover efekty v keyboard módu
      }
      
      // Odstranit všechny selected třídy pouze při mouse hover
      document.querySelectorAll(".search-result.selected").forEach(el => {
        el.classList.remove("selected");
      });
      resultItem.classList.add("selected");
    });

    // Reset keyboard módu při pohybu myši
    resultItem.addEventListener("mousemove", function () {
      if (window.searchModalFunctions) {
        window.searchModalFunctions.enableMouseEvents();
      }
    });

    resultsContainer.appendChild(resultItem);
  });

  if (results.length > 8) {
    const moreResults = document.createElement("div");
    moreResults.classList.add("search-more-results");
    moreResults.innerHTML = `<p>A dalších ${results.length - 8} výsledků...</p>`;
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

function setCookieConsent(consent) {
  safeInvokeDotNet('SetPreference', 'cookieConsent', consent || '');
}

function getCookieConsent() {
  // Tato hodnota by měla být předána z Blazoru při inicializaci
  return document.documentElement.dataset.cookieConsent;
}
