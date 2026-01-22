# Admin rozhraní pro interaktivní cvičení - Návod

## Přehled funkcí

Administrátor může:
1. **Vytvářet** nová interaktivní cvičení pro články
2. **Upravovat** existující cvičení
3. **Mazat** cvičení
4. **Aktivovat/deaktivovat** cvičení (skrýt před uživateli)
5. **Řadit** cvičení pomocí OrderIndex

---

## Přístup k správě cvičení

### Cesta 1: Z domovské stránky Admin panelu

1. Přihlaste se do Admin panelu (`/`)
2. V seznamu článků klikněte na ikonu **puzzle** (<i class="bi bi-puzzle"></i>) u daného článku
3. Otevře se seznam cvičení pro tento článek

### Cesta 2: Přímý odkaz

```
/admin/exercises/{postId}
```

Například pro článek s ID 123:
```
/admin/exercises/123
```

---

## Správa cvičení - Seznam

### URL: `/admin/exercises/{postId}`

Na této stránce vidíte:
- **Název článku** (v horní části)
- **Seznam všech cvičení** (včetně neaktivních)
- **Akční tlačítka**

### Tlačítka v seznamu:

| Ikona | Funkce | Popis |
|-------|--------|-------|
| <i class="bi bi-plus-circle"></i> **Nové cvičení** | Vytvoří nové cvičení | Otevře editor s prázdným formulářem |
| <i class="bi bi-pencil"></i> **Upravit** | Upraví cvičení | Načte existující data do editoru |
| <i class="bi bi-play-circle"></i> **Testovat** | Otestuje cvičení | TODO: Otevře náhled cvičení |
| <i class="bi bi-trash"></i> **Smazat** | Smaže cvičení | Potvrzovací dialog → smazání |
| <i class="bi bi-arrow-left"></i> **Zpět na články** | Návrat | Zpět na seznam článků |

### Stav cvičení v seznamu:

- ✅ **Aktivní** (zelený badge) - viditelné pro uživatele
- ❌ **Neaktivní** (šedý badge) - skryté, pouze admin vidí

---

## Editor cvičení

### URL pro nové cvičení:
```
/admin/exercises/{postId}/new
```

### URL pro úpravu:
```
/admin/exercises/{postId}/edit/{exerciseId}
```

---

## Formulář editoru

### 1. Základní informace

#### **Název cvičení*** (povinné)
```
Např: Zapoj sériový obvod
```

#### **Typ cvičení*** (povinné)
Vyberte z:
- **Fyzikální obvod** (`circuit`)
- **Časová osa** (`timeline`)
- **Přetahování do kategorií** (`drag-drop`)
- **Chemická molekula** (`molecule`)
- **Párování** (`matching`)

#### **Instrukce (Markdown)** (volitelné)
Zobrazí se nad cvičením jako popis/zadání.
```markdown
## Úkol
Zapoj jednoduchý obvod s jednou žárovkou...
```

#### **Pořadí** (číslo)
Čím nižší číslo, tím výše se cvičení zobrazí.
- `0` = první cvičení
- `1` = druhé cvičení
- atd.

#### **Stav** (switch)
- ☑️ **Aktivní** - uživatelé vidí cvičení
- ☐ **Neaktivní** - cvičení je skryté

---

### 2. Konfigurace cvičení (JSON)

Zde definujete obsah a strukturu cvičení podle typu.

**Tlačítka:**
- 📝 **Načíst příklad** - vloží ukázkový JSON podle vybraného typu
- ✅ **Validovat JSON** - zkontroluje syntaxi JSON

#### Příklad: Fyzikální obvod (`circuit`)

```json
{
  "components": [
    { "id": "battery-1", "type": "battery", "voltage": 12, "x": 50, "y": 100 },
    { "id": "bulb-1", "type": "bulb", "resistance": 6, "x": 200, "y": 100 },
    { "id": "switch-1", "type": "switch", "state": "off", "x": 350, "y": 100 }
  ],
  "availableComponents": [
    { "type": "bulb", "label": "Žárovka" },
    { "type": "resistor", "label": "Odpor" },
    { "type": "switch", "label": "Přepínač" }
  ]
}
```

#### Příklad: Časová osa (`timeline`)

```json
{
  "events": [
    { "id": "event-1", "label": "Bitva u Lipan", "year": 1434 },
    { "id": "event-2", "label": "Bitva na Bílé hoře", "year": 1620 },
    { "id": "event-3", "label": "Založení UK", "year": 1348 }
  ],
  "timeRange": { "start": 1300, "end": 1700 }
}
```

---

### 3. Správné řešení (JSON)

Zde definujete, jaká odpověď je správná. Backend použije toto pro validaci řešení od uživatele.

**⚠️ Důležité:** Uživatelé **nikdy neuvidí** tento JSON (je skrytý na backendu).

#### Příklad: Fyzikální obvod

```json
{
  "correctConnections": [
    { "from": "battery-1", "to": "switch-1" },
    { "from": "switch-1", "to": "bulb-1" },
    { "from": "bulb-1", "to": "battery-1" }
  ],
  "explanation": "Správně zapojený sériový obvod s přepínačem."
}
```

#### Příklad: Časová osa

```json
{
  "correctOrder": ["event-3", "event-1", "event-2"],
  "explanation": "Chronologické pořadí: 1348 → 1434 → 1620"
}
```

---

### 4. Nápověda (boční panel)

Po vybrání typu cvičení se zobrazí:
- **Příklad ConfigJson** (zmenšený náhled)
- **Odkaz na kompletní dokumentaci** ([INTERACTIVE_EXERCISES_DOCS.md](../INTERACTIVE_EXERCISES_DOCS.md))

---

## Workflow - Vytvoření cvičení krok za krokem

### Krok 1: Přejděte na seznam článků
```
/
```

### Krok 2: Najděte článek a klikněte na ikonu puzzle
```
Např. článek "Fyzika - Elektrický proud"
→ ikona puzzle
```

### Krok 3: Klikněte na "Nové cvičení"
```
/admin/exercises/123/new
```

### Krok 4: Vyplňte formulář

1. **Název:** `Zapoj sériový obvod`
2. **Typ:** Vyberte `Fyzikální obvod`
3. **Instrukce:**
   ```markdown
   ## Úkol
   Zapoj baterii (12V) s jednou žárovkou a přepínačem tak, aby žárovka svítila pouze při zapnutém přepínači.
   ```
4. **Pořadí:** `0` (první cvičení)
5. **Stav:** ☑️ Aktivní

### Krok 5: Načtěte příklad ConfigJson

Klikněte na tlačítko **"Načíst příklad"** u ConfigJson.

Upravte podle potřeby (např. změňte napětí baterie):
```json
{
  "components": [
    { "id": "battery-1", "type": "battery", "voltage": 9, "x": 50, "y": 100 },
    ...
  ],
  ...
}
```

### Krok 6: Načtěte příklad SolutionJson

Klikněte na tlačítko **"Načíst příklad"** u SolutionJson.

Upravte `correctConnections` podle vaší konfigurace:
```json
{
  "correctConnections": [
    { "from": "battery-1", "to": "switch-1" },
    { "from": "switch-1", "to": "bulb-1" },
    { "from": "bulb-1", "to": "battery-1" }
  ],
  "explanation": "Sériový obvod: baterie → přepínač → žárovka → zpět"
}
```

### Krok 7: Validujte JSONy

Klikněte na tlačítka **"Validovat JSON"** u obou polí.

Pokud je JSON chybný, zobrazí se červená chybová hláška.

### Krok 8: Uložte cvičení

Klikněte na **"Vytvořit cvičení"**.

Po úspěšném uložení se přesměrujete zpět na seznam cvičení.

---

## Zobrazení cvičení pro uživatele

### Uživatelská stránka článku:
```
/post/{postId}
```

Pokud článek má aktivní cvičení:
1. Pod obsahem článku se zobrazí sekce **"Interaktivní cvičení"**
2. Každé cvičení je v samostatné kartě s:
   - Názvem
   - Ikonou podle typu
   - Instrukcemi (pokud jsou)
   - **TODO:** Interaktivní komponentou (zatím placeholder)

### Prozatímní zobrazení:
- ⚠️ **"Vývoj probíhá: Interaktivní komponenta pro tento typ cvičení bude brzy dostupná."**
- 🔍 Detail `<details>` s konfigurací (pro debugging)

---

## API endpointy (pro referenci)

### Uživatelské (veřejné):
```http
GET /api/InteractiveExercises/post/{postId}
→ Získá aktivní cvičení

POST /api/InteractiveExercises/{id}/validate
→ Validuje řešení od uživatele
```

### Admin (vyžaduje autentizaci):
```http
GET /api/InteractiveExercises/post/{postId}/all
→ Všechna cvičení (i neaktivní)

POST /api/InteractiveExercises
→ Vytvoří nové cvičení

PUT /api/InteractiveExercises/{id}
→ Upraví cvičení

DELETE /api/InteractiveExercises/{id}
→ Smaže cvičení

GET /api/InteractiveExercises/{id}/solution
→ Získá správné řešení (pouze admin)
```

---

## Tipy a triky

### 💡 Tip 1: Použijte příklady
Vždy začněte tlačítkem "Načíst příklad" a pak upravujte.

### 💡 Tip 2: Validujte JSON před uložením
I když backend provádí validaci, předejděte chybám kliknutím na "Validovat JSON".

### 💡 Tip 3: Pořadí cvičení
- Začněte od `0` pro první cvičení
- Pokud chcete přidat cvičení mezi existující, změňte pořadí ostatních

### 💡 Tip 4: Testování
1. Vytvořte cvičení jako **Neaktivní**
2. Otestujte ho (až bude funkce dostupná)
3. Až je vše OK, změňte na **Aktivní**

### 💡 Tip 5: Kopírování cvičení
Zatím není funkce "duplikovat" - můžete ale:
1. Otevřít existující cvičení
2. Zkopírovat JSON
3. Vytvořit nové cvičení
4. Vložit JSON a upravit

---

## Další kroky (TODO)

### Frontend komponenty (zatím nedostupné):
- [ ] `<CircuitSimulator>` - interaktivní obvod
- [ ] `<TimelineExercise>` - přetahovací časová osa
- [ ] `<DragDropExercise>` - drag & drop UI
- [ ] `<MoleculeBuilder>` - stavění molekul
- [ ] `<MatchingExercise>` - párování prvků

### Admin funkce:
- [ ] Testovací stránka (`/admin/exercises/{postId}/test/{exerciseId}`)
- [ ] Duplikace cvičení
- [ ] Náhled cvičení v adminu
- [ ] Hromadné operace (smazat více cvičení najednou)

---

## Troubleshooting

### Problém: "Článek nenalezen"
**Řešení:** Zkontrolujte, že `PostId` v URL existuje v databázi.

### Problém: "Chybný ConfigJson"
**Řešení:**
1. Klikněte na "Validovat JSON"
2. Opravte syntax podle chybové hlášky
3. Ujistěte se, že všechny řetězce jsou v uvozovkách `""`

### Problém: Cvičení se nezobrazuje na stránce článku
**Možné příčiny:**
1. Cvičení je **Neaktivní** - zkontrolujte stav v editoru
2. PostId je špatné - ověřte správnost ID článku
3. Chyba při načítání - zkontrolujte browser console (F12)

### Problém: "Nepodařilo se aktualizovat cvičení"
**Řešení:**
1. Zkontrolujte připojení k API
2. Ověřte, že jste přihlášeni jako admin
3. Zkontrolujte logy serveru

---

## Kontakt & Podpora

Pro dotazy nebo nahlášení chyb kontaktujte vývojový tým.
