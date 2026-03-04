# AI Endpoint — client usage

Tento dokument popisuje, co posílat na API a jak z aplikace (Blazor / C#) volat endpointy.

## Endpoints

- `POST /api/ai/ask` — hlavní endpoint pro dotazy (public).
- `GET  /api/ai/diag` — diagnostika volání externího OpenAI API.

## Request payload (`AiChatRequest`)

Předpokládaná struktura JSON (dle `Shared/DTOs`):

```json
{
  "postId": 123,
  "question": "Jak funguje potenciální energie?",
  "conversationHistory": [
    { "role": "user", "content": "Dřívější otázka..." }
  ]
}
```

## Headers

- `X-Client-Id: <id>` — volitelná hlavička pro označení důvěryhodné klientské aplikace. Pokud je server nakonfigurován (viz níže), umožní per‑client limit (např. 20 dotazů místo výchozích 10).
- Standardní HTTP hlavičky (`Content-Type: application/json`) apod.

Poznámka: `X-Client-Id` lze snadno spoofovat. Pokud potřebujete bezpečně rozlišovat klienty, používejte ověřené API klíče nebo BasicAuth a validate na serveru.

## Per‑client limit (20 pro konkrétní app)

Na serveru přidejte do konfigurace (např. `appsettings.json` nebo `appsettings.Development.json`):

```json
"OpenAI": {
  "MaxDailyRequests": "10",
  "ClientLimits": {
    "trusted-app": "20"
  }
}
```

Klient, který chce limit 20, pošle při každém požadavku hlavičku:

```
X-Client-Id: trusted-app
```

Server použije `OpenAI:ClientLimits:trusted-app` jako denní limit pro tento `clientId`.

## Příklad volání z Blazor / C# (HttpClient)

```csharp
var payload = new {
    postId = 123,
    question = "Jak funguje potenciální energie?",
    conversationHistory = new[] { new { role = "user", content = "Předchozí" } }
};

var json = JsonSerializer.Serialize(payload);
using var req = new HttpRequestMessage(HttpMethod.Post, "api/ai/ask")
{
    Content = new StringContent(json, Encoding.UTF8, "application/json")
};
req.Headers.Add("X-Client-Id", "trusted-app");

var resp = await httpClient.SendAsync(req);
if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    // 429 - limit reached
}
else if (!resp.IsSuccessStatusCode)
{
    // handle error (502, 500...)
}
else
{
    var body = await resp.Content.ReadAsStringAsync();
    // očekávaná odpověď: { answer: "...", remainingQuestions: 7 }
}
```

## Response

- `200 OK` — JSON s odpovědí a polem `remainingQuestions` (kolik dotazů zbývá).
- `429 Too Many Requests` — překročen limit (body obsahuje zprávu).
- `502` nebo `500` — problémy s voláním externí služby nebo serverem.

## Doporučení pro produkci

- Pokud chcete zabránit spoofingu `X-Client-Id`, používejte ověřené API klíče nebo BasicAuth. Server může mapovat API klíč → `clientId` a použít ten k přiřazení limitu.
- Pro distribuované nasazení a přesné rate‑limity zvažte Redis místo paměťového cache.

---
S tímto souborem můžete poslat klientovi jasný návod, jak volat endpoint a jak získat vyšší limit (20) pro konkrétní aplikaci.