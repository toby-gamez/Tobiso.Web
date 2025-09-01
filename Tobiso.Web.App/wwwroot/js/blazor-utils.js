// blazor-utils.js - JavaScript module pro Blazor
let dotNetHelper;
let index, pages;

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
      logo.src = "https://tobiso.com/images/white-logo.png";
      logo.alt = "bílé logo";
    });

    // Notifikace Blazor komponenty
    if (dotNetHelper) {
      dotNetHelper.invokeMethodAsync('OnDarkModeToggled', true);
    }
  }

  function disableDarkMode() {
    body.classList.remove("dark-mode");
    setDarkModePreference(false);
    logoElements.forEach((logo) => {
      logo.src = "https://tobiso.com/images/normal-logo.png";
    });

    // Notifikace Blazor komponenty
    if (dotNetHelper) {
      dotNetHelper.invokeMethodAsync('OnDarkModeToggled', false);
    }
  }

  // Kontrola uložené preference
  if (getDarkModePreference()) {
    enableDarkMode();
    if (toggleButton) toggleButton.checked = true;
    if (toggleButtonMobile) toggleButtonMobile.checked = true;
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
  try {
    const response = await fetch("/api/posts");
    const data = await response.json();
    // Transformace dat pro Lunr index
    pages = data.map(post => ({
      url: `/post/${post.id}`,
      title: post.title,
      content: post.content
    }));

    // Inicializace Lunr indexu
    index = lunr(function () {
      this.ref("url");
      this.field("title");
      this.field("content");

      pages.forEach((page) => {
        this.add(page);
      });
    });
  } catch (error) {
    console.error("Chyba při načítání indexu:", error);
  }
}

function initSearchModal() {
  const modal = document.getElementById("searchModal");
  const closeBtn = document.getElementById("closeSearch");
  const searchInput = document.getElementById("search");
  const openBtns = document.querySelectorAll("#openSearch");

  if (!modal || !searchInput) return;

  openBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      modal.classList.add("active");
      toggleScrollLock(true);
      searchInput.focus();
    });
  });

  if (closeBtn) {
    closeBtn.addEventListener("click", closeSearchModal);
  }

  window.addEventListener("click", function (event) {
    if (event.target === modal) {
      closeSearchModal();
    }
  });

  searchInput.addEventListener("input", function () {
    const query = searchInput.value;
    if (query.length >= 2) {
      performSearch(query);
    } else {
      document.getElementById("results").innerHTML = "";
    }
  });
}

function closeSearchModal() {
  const modal = document.getElementById("searchModal");
  const searchInput = document.getElementById("search");

  modal.classList.remove("active");
  toggleScrollLock(false);
  searchInput.value = "";
  document.getElementById("results").innerHTML = "";
}

function performSearch(query) {
  if (!pages || query.length < 2) return;

  const results = searchPages(query);
  displaySearchResults(results);

  // Notifikace Blazor komponenty
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('OnSearchPerformed', query, JSON.stringify(results));
  }
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
  if (!resultsContainer) return;

  resultsContainer.innerHTML = "";

  if (results.length === 0) {
    resultsContainer.innerHTML = "<div class='no-results'>Žádné výsledky nenalezeny</div>";
    return;
  }

  results.forEach((result) => {
    const resultItem = document.createElement("div");
    resultItem.classList.add("search-result");
    resultItem.dataset.url = result.url;
    resultItem.style.cursor = "pointer";

    let snippetText = result.highlightedTerm;
    if (result.foundInTitle && !result.foundInContent) {
      snippetText = "V textu není zmínka";
    }

    resultItem.innerHTML = `
            <div class="result-title">${result.title}</div>
            <p class="search-snippet">${snippetText}</p>
        `;

    resultItem.addEventListener("click", function () {
      window.location.href = result.url;
    });

    resultsContainer.appendChild(resultItem);
  });
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
    if (event.key === "Escape") {
      closeSearchModal();
    }

    const activeElement = document.activeElement.tagName.toLowerCase();
    const isTyping = activeElement === "input" || activeElement === "textarea" || activeElement === "select";

    if (event.key.toLowerCase() === "k" && !isTyping) {
      event.preventDefault();
      event.stopPropagation();
      const modal = document.getElementById("searchModal");
      const searchInput = document.getElementById("search");
      if (modal && searchInput) {
        modal.classList.add("active");
        toggleScrollLock(true);
        searchInput.focus();
      }
    }

    if (event.key === "Enter") {
      const firstResult = document.querySelector(".search-result");
      if (firstResult && firstResult.dataset.url) {
        window.location.href = firstResult.dataset.url;
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
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('SetPreference', 'darkMode', isDark.toString());
  }
}

function getDarkModePreference() {
  // Tato hodnota by měla být předána z Blazoru při inicializaci
  return document.body.classList.contains("dark-mode-preferred");
}

function setCookieConsent(consent) {
  if (dotNetHelper) {
    dotNetHelper.invokeMethodAsync('SetPreference', 'cookieConsent', consent || '');
  }
}

function getCookieConsent() {
  // Tato hodnota by měla být předána z Blazoru při inicializaci
  return document.documentElement.dataset.cookieConsent;
}
