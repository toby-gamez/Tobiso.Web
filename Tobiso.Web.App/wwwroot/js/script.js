document.addEventListener("DOMContentLoaded", function () {
  // Dark mode toggle functionality
  function initDarkMode() {
    const toggleButton = document.getElementById("dark-mode-toggle");
    const toggleButtonMobile = document.getElementById(
      "dark-mode-toggle-mobile",
    );
    const body = document.body;
    const logoElements = document.querySelectorAll(".navImg");

    function enableDarkMode() {
      body.classList.add("dark-mode");
      document
        .querySelectorAll(
          "body",
        )
        .forEach((element) => {
          element.classList.add("dark-mode");
        });
      localStorage.setItem("darkMode", "enabled");
      logoElements.forEach((logo) => {
        logo.src = "https://tobiso.com/images/white-logo.png";
      });
    }

    function disableDarkMode() {
      body.classList.remove("dark-mode");
      document
        .querySelectorAll(
          "body",
        )
        .forEach((element) => {
          element.classList.remove("dark-mode");
        });
      localStorage.setItem("darkMode", "disabled");
      logoElements.forEach((logo) => {
        logo.src = "https://tobiso.com/images/normal-logo.png";
      });
    }

    // Apply dark mode if previously enabled
    if (localStorage.getItem("darkMode") === "enabled") {
      enableDarkMode();
      if (toggleButton) toggleButton.checked = true;
      if (toggleButtonMobile) toggleButtonMobile.checked = true;
    }

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
  initDarkMode();

  var loadingBar = document.querySelector(".loading-bar");

  // Funkce pro aktualizaci šířky loading baru podle pozice scrollování
  function updateLoadingBar() {
    var scrollTop =
      document.documentElement.scrollTop || document.body.scrollTop;
    var scrollHeight =
      document.documentElement.scrollHeight || document.body.scrollHeight;
    var clientHeight = document.documentElement.clientHeight;
    var scrollPercent = (scrollTop / (scrollHeight - clientHeight)) * 100;
    loadingBar.style.width = scrollPercent + "%";
  }

  // Aktualizace loading baru při scrollování
  window.addEventListener("scroll", updateLoadingBar);

  // Prvotní nastavení při načtení stránky
  updateLoadingBar();

  const toggleButton = document.querySelector(".navbar-toggler");
  const closeButton = document.querySelector(".close-menu");
  const mobileMenu = document.querySelector(".mobile-menu");

  function toggleMenu() {
    mobileMenu.classList.toggle("show");
    document.body.style.overflow = mobileMenu.classList.contains("show")
      ? "hidden"
      : "";
  }

  toggleButton.addEventListener("click", toggleMenu);
  closeButton.addEventListener("click", toggleMenu);

  // Zavření menu při kliknutí na odkaz
  const menuLinks = mobileMenu.querySelectorAll(".nav-link");
  menuLinks.forEach((link) => {
    link.addEventListener("click", toggleMenu);
  });
});

function scrollToTop() {
  window.scrollTo({ top: 0, behavior: "smooth" });
}
function goBack() {
  window.history.back();
}

document.addEventListener("DOMContentLoaded", function () {
  let index, pages;

  // Funkce pro odstranění diakritiky
  function normalizeText(text) {
    return text
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();
  }

  // Načtení JSON indexu
  fetch("javastripts/search-index.json")
    .then((response) => response.json())
    .then((data) => {
      pages = data;
      index = lunr(function () {
        this.ref("url");
        this.field("title");
        this.field("content");

        data.forEach((page) => {
          page.title = normalizeText(page.title);
          page.content = normalizeText(page.content);
          this.add(page);
        });
      });
    });

  // Událost při psaní do vyhledávače
  document.getElementById("search").addEventListener("input", function () {
    let query = normalizeText(this.value.trim());
    let resultsContainer = document.getElementById("results");
    resultsContainer.innerHTML = "";

    if (query.length < 2) return; // Minimální délka dotazu

    let results = index.search(`*${query}*`); // Hledá části slov (fuzzy search)

    if (results.length === 0) {
      resultsContainer.innerHTML = "<p>Žádné výsledky.</p>";
      return;
    }

    results.forEach((result) => {
      let page = pages.find((p) => p.url === result.ref);
      let div = document.createElement("div");
      div.innerHTML = `<a href="${page.url}">${page.title}</a>`;
      resultsContainer.appendChild(div);
    });
  });
});

document.addEventListener("DOMContentLoaded", function () {
  let index, pages;

  // Funkce pro normalizaci textu (odstranění diakritiky a převedení na malá písmena)
  function normalizeText(text) {
    if (!text) return "";
    return text
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();
  }

  // Funkce pro nalezení a zvýraznění celého pojmu
  function findAndHighlightTerm(content, query) {
    if (!content) return "";

    const normalizedQuery = normalizeText(query);
    // Rozdělení obsahu podle čárek a následné ořezání mezer
    const terms = content.split(",").map((term) => term.trim());

    for (let term of terms) {
      if (normalizeText(term).includes(normalizedQuery)) {
        // Najdeme pozici hledaného výrazu v normalizovaném termínu
        const normalizedTerm = normalizeText(term);
        const queryIndex = normalizedTerm.indexOf(normalizedQuery);

        if (queryIndex !== -1) {
          // Rozdělíme podle přesné pozice výrazu
          const prefix = term.substring(0, queryIndex);
          const match = term.substring(queryIndex, queryIndex + query.length);
          const suffix = term.substring(queryIndex + query.length);

          return prefix + "<strong>" + match + "</strong>" + suffix;
        }

        return term; // Vrátíme celý pojem, i když nemůžeme přesně lokalizovat hledaný výraz
      }
    }

    return ""; // Pokud nic nenajdeme
  }

  // Funkce pro přepínání scrollování
  function toggleScrollLock(lock) {
    if (lock) {
      document.body.classList.add("scroll-lock");
    } else {
      document.body.classList.remove("scroll-lock");
    }
  }

  // Načtení dat z indexu
  fetch("js/search-index.json")
    .then((response) => response.json())
    .then((data) => {
      pages = data;
      index = lunr(function () {
        this.ref("url");
        this.field("title");
        this.field("content");

        data.forEach((page) => {
          this.add(page);
        });
      });
    })
    .catch((error) => {
      console.error("Chyba při načítání indexu:", error);
    });

  // Ovládání modalního okna pro hledání
  const modal = document.getElementById("searchModal");
  const closeBtn = document.getElementById("closeSearch");
  const searchInput = document.getElementById("search");
  const openBtns = document.querySelectorAll("#openSearch");
  openBtns.forEach((btn) => {
    btn.addEventListener("click", function () {
      modal.classList.add("active");
      toggleScrollLock(true); // Zamkne scrollování
      searchInput.focus();
    });
  });

  closeBtn.addEventListener("click", function () {
    modal.classList.remove("active");
    toggleScrollLock(false); // Odemkne scrollování
    searchInput.value = "";
    document.getElementById("results").innerHTML = "";
  });

  window.addEventListener("click", function (event) {
    if (event.target === modal) {
      modal.classList.remove("active");
      toggleScrollLock(false); // Odemkne scrollování
      searchInput.value = "";
      document.getElementById("results").innerHTML = "";
    }
  });

  window.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      modal.classList.remove("active");
      toggleScrollLock(false); // Odemkne scrollování
      searchInput.value = "";
      document.getElementById("results").innerHTML = "";
    }

    const activeElement = document.activeElement.tagName.toLowerCase();
    const isTyping =
      activeElement === "input" ||
      activeElement === "textarea" ||
      activeElement === "select";

    if (event.key.toLowerCase() === "k" && !isTyping) {
      event.preventDefault();
      event.stopPropagation();
      modal.classList.add("active");
      toggleScrollLock(true); // Zamkne scrollování
      searchInput.focus();
    }
  });

  // Otevření prvního výsledku při stisknutí Enter
  window.addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
      const firstResult = document.querySelector(".search-result"); // Hledáme první výsledek místo odkazu
      if (firstResult && firstResult.dataset.url) {
        window.location.href = firstResult.dataset.url; // Použijeme URL z data atributu
      }
    }
  });

  // Funkce pro hledání
  function search(query) {
    if (!pages || query.length < 2) return [];

    const results = [];
    const normalizedQuery = normalizeText(query);

    pages.forEach((page) => {
      let score = 0;
      let foundInTitle = false;
      let foundInContent = false;
      let highlightedTerm = "";

      // Hledání v názvu
      if (normalizeText(page.title).includes(normalizedQuery)) {
        score += 10;
        foundInTitle = true;
      }

      // Hledání v obsahu
      if (
        page.content &&
        normalizeText(page.content).includes(normalizedQuery)
      ) {
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
        });
      }
    });

    // Seřazení výsledků podle skóre
    results.sort((a, b) => b.score - a.score);
    return results;
  }

  // Funkce pro zobrazení výsledků vyhledávání
  function displaySearchResults(query) {
    if (query.length < 2) return;

    const results = search(query);
    const resultsContainer = document.getElementById("results");
    resultsContainer.innerHTML = "";

    if (results.length === 0) {
      resultsContainer.innerHTML =
        "<div class='no-results'>Žádné výsledky nenalezeny</div>";
      return;
    }

    results.forEach((result) => {
      const resultItem = document.createElement("div");
      resultItem.classList.add("search-result");
      resultItem.dataset.url = result.url; // Uložíme URL jako data atribut
      resultItem.style.cursor = "pointer"; // Změníme kurzor na pointer pro indikaci klikatelnosti

      // Připravíme text pro pravý sloupec
      let snippetText = result.highlightedTerm;

      // Pokud jsme našli výsledek v titulku, ale ne v obsahu, zobrazíme informační text
      if (result.foundInTitle && !result.foundInContent) {
        snippetText = "V textu není zmínka";
      }

      // Vytvoříme HTML pro výsledek: vlevo odkaz na stránku, vpravo ukazatel
      resultItem.innerHTML = `
                <div class="result-title">${result.title}</div>
                <p class="search-snippet">${snippetText}</p>
            `;

      // Přidáme event listener pro kliknutí na celý div
      resultItem.addEventListener("click", function () {
        window.location.href = result.url;
      });

      resultsContainer.appendChild(resultItem);
    });
  }

  // Vyhledávání při zadání textu
  searchInput.addEventListener("input", function () {
    const query = searchInput.value;
    if (query.length >= 2) {
      displaySearchResults(query);
    } else {
      document.getElementById("results").innerHTML = "";
    }
  });
});

function acceptCookies() {
  localStorage.setItem("cookieConsent", "true");
  document.getElementById("cookie-consent").style.display = "none";

  console.log("User accepted cookies.");
  gtag("consent", "update", {
    analytics_storage: "granted",
  });

  loadGoogleAnalytics();
}

function declineCookies() {
  localStorage.setItem("cookieConsent", "false");
  document.getElementById("cookie-consent").style.display = "none";

  console.log("User declined cookies.");
  gtag("consent", "update", {
    analytics_storage: "denied",
  });
}

window.onload = function () {
  let consent = localStorage.getItem("cookieConsent");

  if (consent === "true") {
    console.log("Consent found: Accepted");
    acceptCookies();
  } else if (consent === "false") {
    console.log("Consent found: Declined");
    declineCookies();
  } else {
    console.log("No consent found, showing cookie banner.");
    document.getElementById("cookie-consent").style.display = "flex";
  }

  // Zpožděné zobrazení cookie banneru
  setTimeout(function() {
    var cookieBanner = document.getElementById("cookie-consent");
    if (cookieBanner) {
      cookieBanner.style.display = "block";
      cookieBanner.classList.add("show"); // pokud používáte třídu pro zobrazení
    }
  }, 3000);
};

document
  .getElementById("removeConsentBtn")
  .addEventListener("click", function () {
    localStorage.removeItem("cookieConsent"); // Vymaže pouze položku cookieConsent
    alert("Váš souhlas byl odebrán a data cookies byla vymazána.");
    location.reload();
  });
