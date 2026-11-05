# Tobiso.Web: Security & Compliance Audit (November 2026)

_Date: 2026-11-05 · Scope: whole solution at commit `63c951e` (87 commits since the July audit in `/AUDIT.md`)_
_Method: static code and configuration review. No penetration test, no access to production config, database, hosting, or the Terms and Privacy Policy texts (those are stored in the database, not in the repo)._

> **Remediation status (2026-11-05):** every code-level finding except S10 (admin accounts/MFA), L3 (per-account login throttle) and L5 (CSP nonces) is fixed on branch `ccr-95159bd0-la5y1h`. See **section 6** for what changed and what is left for you to do, mostly configuration, deployment and documentation.

> **Not legal advice.** The compliance sections map what the code does against Czech/EU legal requirements and ISO/IEC 27001:2022 controls. Have a lawyer or DPO confirm the conclusions before you rely on them, especially anything about minors and AI.

---

## 1. Executive summary

The authentication and session layer has improved a lot since July: HttpOnly session cookies, security-stamp revocation, signed single-use email/reset tokens, fail-closed admin policy, Sec-Fetch-Site CSRF hardening, and PBKDF2 hashing. The July C1, C3 and H1–H3 fixes are still in place.

**One July fix has regressed and is again Critical:** anonymous AI usage is once more keyed on the client-controlled `X-Device-Id` header. That allows unlimited OpenAI spend (see S1).

| Area | Verdict |
|---|---|
| Application security | 1 Critical, 3 High, 7 Medium, several Low |
| GDPR / Czech data protection (110/2019 Sb.) | **Partially aligned.** Core rights (access, export, erasure, age 15 consent) are implemented. Gaps: cookie consent before GA loads, no way to withdraw consent, no retention rules, unencrypted DB transport, processor/transfer documentation. |
| Czech ePrivacy (§89(3) zák. 127/2005 Sb.) | **Gap:** gtag.js loads and pings Google before consent; there is no way to change the choice later. |
| EU AI Act | **Needs action:** Art. 50 transparency has applied since 2 Aug 2026; the Annex III (education) assessment is not documented. |
| ISO/IEC 27001:2022 | **Not aligned. There is no ISMS.** Some technical controls are good, but the organisational controls (risk assessment, SoA, policies, incident, backup, supplier management) have no evidence. Admin identity and authentication (A.5.16, A.8.5) and change management (A.8.32) are weak. |

---

## 2. Application security findings

### 🔴 S1 (Critical, regression of July C2): anonymous AI quota is bypassable, so OpenAI spend is unbounded
`AiController.GetBonusKey()` (`Tobiso.Web.App/Controllers/AiController.cs:466`) returns `device:{X-Device-Id}` whenever the header is present. Every AI endpoint passes this key to `AiUsageGuard.TryConsumeAsync`. For anonymous callers the guard consumes the **daily allowance on that key** (`Tobiso.Web.App/Services/AiUsageGuard.cs:79-81` → `AnonymousUsageService.TryConsumeAsync(deviceId, …)`).

- **Exploit:** send each request with a fresh random `X-Device-Id`. Each new value gets 20 free requests, so there is no effective limit. This works on ~20 `[AllowAnonymous]` endpoints (`ask`, `ask-stream`, `flashcards`, `what-if`, `compare`, …).
- **Side effect:** each random ID inserts a row into `AnonymousAiUsages`, which is never purged. This means unbounded DB growth.
- The comment on `GetRateKey()` ("anchored to the server-observed client IP") is now false. `GetRateKey()` is computed but not used for consumption.

**Fix:** consume the anonymous allowance on the IP key (`GetRateKey()`). Use the device key only to look up purchased bonus (`GetBonusTotal`). Add a global daily OpenAI request/token budget as a circuit breaker. Purge `AnonymousAiUsages` rows older than a few days.

### 🟠 S2 (High): logged-in students get unlimited free AI on most endpoints
`AiUsageGuard` returns `Allowed=true` for every student, on the assumption that "callers deduct credits" (`AiUsageGuard.cs:49-50`). Only `ask` and `ask-stream` call `DeductCreditsAsync` (`AiController.cs:101,261`). `explain-sentence`, `evaluate-answer`, `flashcards`, `practice-problems`, `rewrite-grade`, `what-if`, `compare`, `why`, `step-solver`, etc. go through `TryConsumeRateLimit()` and never charge. One verified account therefore has unlimited OpenAI use. The Blazor pages call `DeductAiCreditIfLoggedInAsync` themselves, but the HTTP API does not.

**Fix:** deduct credits in a shared place, e.g. move the deduction into `IAiUsageGuard` and make it atomic with the check. Also add a per-account daily hard cap.

### 🟠 S3 (High): no input-size limits on AI prompts
`AiChatRequest.Question`, `ExplainSentenceRequest.Sentence`, `compare` terms and similar inputs have no length cap. One request can carry a very large prompt, and combined with S1/S2 this multiplies the cost. The default Kestrel body limit (~28 MB) is the only bound.

**Fix:** validate the length of every AI input, e.g. ≤ 2,000 characters for questions. Set `[RequestSizeLimit]` on `AiController`. Set `max_tokens` on every upstream call.

### 🟠 S4 (High): database connection is unencrypted
`appsettings.template.json` specifies `Encrypt=False`. If SQL Server is on another host, which is typical for shared hosting, all personal data travels in clear text: emails, password hashes, chats and notes of minors. This affects GDPR Art. 32 and ISO A.8.24.

**Fix:** use `Encrypt=True;TrustServerCertificate=False` (or `Encrypt=Strict`), and confirm the production value.

### 🟡 S5 (Medium): AI output rendered as raw HTML again (partial regression of July H4)
`PostDetail.razor:386,495,548` renders AI-generated `_examSummary`, `_compareMarkdown` and `_rewrittenContent` through `MarkdownContent`. That component uses Markdig's default pipeline, which passes raw HTML through (`MarkdownContent.razor:119`). A prompt-injected answer such as a `compare` term asking the model to emit `<img src=x onerror=…>` executes in the page. CSP allows `'unsafe-inline'` scripts, so this is not blocked. Today the output only reaches the user who triggered it (self-XSS). It becomes stored XSS if any of these outputs is ever cached and shared, which exam summaries already are per post.

**Fix:** add a `TrustedHtml=false` mode to `MarkdownContent` that uses `.DisableHtml()` (as `AiChatBox`/`AiMarkdown` do), and use it for every AI-sourced string.

### 🟡 S6 (Medium): anonymous feedback endpoint has no rate limit or size limits
`POST /api/feedback` (`FeedbackController.cs:22`) is anonymous, has no `[EnableRateLimiting]`, and `CreateFeedbackDto` has no `[MaxLength]`/`[EmailAddress]` attributes. This allows spam and DB flooding. It also collects names and emails with no retention rule (see C-4).

### 🟡 S7 (Medium): TLS validation for OpenAI can be disabled in production
`Program.cs` disables certificate validation for the OpenAI client when `OpenAI:IgnoreInvalidCertificates=true` **in any environment**. A single config flag would expose the API key and all student prompts to MITM.

**Fix:** allow this only in Development, or remove it.

### 🟡 S8 (Medium): deployment over plain FTP
`build/build-ftp*.cake` uses `FluentFTP.FtpClient` without `EncryptionMode`. Credentials and binaries travel unencrypted, so anyone on-path can replace the deployed DLLs. There is no CI pipeline (`.github/workflows/` contains only `.md` notes).

**Fix:** use FTPS (`EncryptionMode.Explicit`) or SFTP / Web Deploy. Better: deploy from CI with build provenance.

### 🟡 S9 (Medium): .NET 9 reaches end of support on 10 Nov 2026
All projects target `net9.0`, and STS support ends in 5 days. After that there are no security patches, which affects ISO A.8.8.

**Fix:** move to .NET 10 (LTS). Package pins such as EF Core 9.0.5 and Google auth 9.0.6 are also behind the latest 9.0.x patches.

### 🟡 S10 (Medium): single shared admin credential, no MFA, admin sessions can't be revoked
There is one `Auth:Basic` username/password for all admins. There is no per-person identity, no audit trail of who changed what, and no second factor. Admin JWTs carry no security stamp, so a leaked admin token stays valid for `ExpiryHours`. Basic auth is also accepted on every admin API call. The admin app can read every student's data, chats included.

**Fix:** use individual admin accounts with TOTP/WebAuthn and a revocable admin session (stamp). Disable Basic on everything except automation endpoints.

### 🟡 S11 (Medium): password hashing below current guidance
PBKDF2-SHA256 runs at 100k iterations. OWASP currently recommends ≥ 600k for PBKDF2-SHA256, or Argon2id. The stored format already includes the iteration count, so you can upgrade on next login.

### 🔵 Low / informational
- **L1:** Login timing reveals whether an email is registered. `UserService.LoginAsync` skips PBKDF2 when the user is missing. Fix: verify against a dummy hash.
- **L2:** 34 places return `ex.Message` to clients (e.g. `AiController` 502 responses), which leaks internal or upstream error details. Return a generic message and log the details.
- **L3:** Student login has only a per-IP limit (20/min). There is no per-account throttle, so credential stuffing from many IPs isn't slowed.
- **L4:** `BasicAuthHandler` logs every request at Information level and splits on `:`, so passwords containing `:` fail.
- **L5:** CSP keeps `'unsafe-inline'` for scripts, which weakens XSS defence in depth. Move to nonces/hashes.
- **L6:** No security-focused tests and no SAST/dependency scanning (the test project covers renderers and streaks only).
- **L7:** The dev private key `certs/localhost-key.pem` is still in git history. It has low impact, but rotate it if it was ever reused.

### ✅ Verified as good
HttpOnly/Secure/SameSite session cookies; security-stamp revocation and "log out everywhere"; signed, expiring, stamp-bound email/reset tokens; enumeration-safe register/forgot-password; Google ID token audience and email-verified checks; fail-closed admin default policy; `Sec-Fetch-Site` CSRF guard; Files service auth, path-traversal and SVG fixes; fail-closed credit HMAC with replay protection; PDF `generate` limited to admins; 256-bit calendar tokens; security headers (HSTS, XFO, nosniff, CSP, frame-ancestors); no real secrets found in git history.

---

## 3. GDPR and Czech law

Applicable framework: **GDPR**; **zákon č. 110/2019 Sb.** (zpracování osobních údajů); **zákon č. 127/2005 Sb. §89(3)** (cookies); **zákon č. 480/2004 Sb.** (služby informační společnosti); **zákon č. 89/2012 Sb.** (občanský zákoník); **zákon č. 634/1992 Sb.** (ochrana spotřebitele); **Nařízení (EU) 2024/1689** (AI Act). The users are school pupils, so **data of minors** raises the bar throughout (GDPR recital 38, Art. 8).

| # | Requirement | Status | Evidence / gap |
|---|---|---|---|
| C-1 | Age of digital consent is 15 in CZ (§7 zák. 110/2019) | ✅ Partly | The registration checkbox confirms "≥ 15 or parental consent" (`Register.razor:56`). Self-declaration only, and no age is stored, which is acceptable for low risk. Google sign-in follows the same `terms` flag. |
| C-2 | Cookie consent **before** non-essential processing (§89(3) ZEK, ÚOOÚ/EDPB guidance) | ❌ Gap | `App.razor:10-22` loads `gtag.js` and runs `gtag('config')` before consent (Consent Mode "advanced"). Google still receives cookieless pings with IP and device data. **Fix:** inject gtag only after "Souhlasím", or drop GA for a cookieless self-hosted tool. |
| C-3 | Consent can be withdrawn as easily as it was given (GDPR Art. 7(3)) | ❌ Gap | After the first choice, the banner never reappears and there is no "Nastavení cookies" link. **Fix:** add a footer link that clears `tobiso-cookie-consent` and reopens the banner. |
| C-4 | Storage limitation (Art. 5(1)(e)) | ❌ Gap | There are no retention jobs at all. Unverified accounts, inactive accounts, AI chat history, `AnonymousAiUsages` (device IDs), feedback (name and email) and `DeviceTokens` are kept forever. **Fix:** define retention periods in the privacy policy and implement a nightly cleanup. |
| C-5 | Security of processing (Art. 32) | ⚠️ | See S4 (DB encryption), S8 (FTP), S10 (shared admin). |
| C-6 | Right of access/portability (Art. 15/20) | ✅ Mostly | `GET /api/user/export` covers most tables. It omits the selected duty-roster name and any feedback the user sent. |
| C-7 | Right to erasure (Art. 17) | ✅ Mostly | Self-service delete with re-authentication, cascading via the FK. Not covered: `Feedback` rows (not linked to the user), events the user created are deleted (fine), and backups/logs need a documented policy. |
| C-8 | Processors and third-country transfers (Art. 28, 44+) | ⚠️ Verify | Data goes to **OpenAI** (student questions, notes context: possibly personal data of minors), **Google** (Analytics, OAuth, Firebase FCM), the SMTP provider and the hosting provider. You need a DPA with each and an EU–US DPF/SCC basis, and the privacy policy must list them. For OpenAI, consider EU data residency or zero-retention. Tell users not to put personal data into the chat. |
| C-9 | Records of processing (Art. 30) and DPIA (Art. 35) | ⚠️ Not evident | Large-scale processing of minors' data plus AI plus behavioural tracking (streaks, read progress) is a strong DPIA trigger under the ÚOOÚ list. Write a short DPIA. |
| C-10 | Duty roster names (`DutyPerson`) | ⚠️ | Admins type real names of classmates, possibly people without accounts, and every approved class member can see them. Document the legal basis (legitimate interest) and the notice to those pupils. |
| C-11 | Breach notification within 72 h (Art. 33/34) | ❌ Not evident | There is no incident procedure, monitoring or alerting. |
| C-12 | Operator identification (§435 OZ; §5 zák. 480/2004) | ⚠️ Verify | No operator name, IČO, address or contact appears in the app chrome. It may be in the DB-hosted Terms; make it reachable from every page. |
| C-13 | Selling AI credits (consumer law, §1837 l) OZ) | ⚠️ Verify | Credits are bought via the Android app (signed grants). If sold through Google Play, Play handles the checkout. The Terms still need price and credit-expiry terms and the digital-content withdrawal waiver. Purchased bonus is in-memory only (`AiRateLimitService`) and **lost on restart**, so users paid for something that can vanish. That is a consumer-law risk as well as a bug. |
| C-14 | Commercial communications (§7 zák. 480/2004) | ⚠️ Verify | Push notifications go to all registered devices. If any are promotional, they need opt-in consent. |
| C-15 | EU AI Act Art. 50 (in force since 2 Aug 2026) | ⚠️ | Users must be clearly told they are interacting with AI. Make sure every AI panel says so explicitly, not only through an icon. AI-generated text published to others should be marked. |
| C-16 | AI Act Annex III(3) (education) | ⚠️ Assess | `evaluate-answer`, `evaluate-comprehension` and `grammar-check` evaluate learning outcomes. Tobiso is self-study, not an educational institution, so it is probably **not** high-risk. Record that reasoning. Art. 4 (AI literacy) also applies to you as deployer. |
| C-17 | Accessibility (zák. 424/2023 Sb., EAA) | ℹ️ Likely exempt | It applies to consumer e-commerce services, with microenterprises exempt. `lang="cs"` and ARIA usage are present. |
| C-18 | Cybersecurity act (zák. 264/2025 Sb., NIS2) | ℹ️ Likely out of scope | The provider is below the size thresholds and not in a regulated sector. Re-check if Tobiso is ever contracted by schools at scale. |

---

## 4. ISO/IEC 27001:2022 alignment

ISO 27001 certifies an **organisation's ISMS**, not code. Nothing in the repo shows the mandatory clauses 4–10: scope, risk assessment and treatment, Statement of Applicability, information security policy, internal audit, management review. **Tobiso is not aligned with ISO 27001 today.** The table maps the relevant Annex A controls to what the code shows.

| Control | Status | Notes |
|---|---|---|
| A.5.1 Policies | ❌ | No information security policy. `.github/workflows/security-analysis.md` is an outdated note, not a policy. |
| A.5.9/5.12 Asset inventory and classification | ❌ | No register of data and assets. Children's data should be classified as confidential. |
| A.5.15–5.18 Access control and identity | ⚠️ | Students: good. Admins: one shared account (S10). |
| A.5.19–5.22 Supplier security | ❌ | OpenAI, Google, hosting and SMTP have no documented assessment or DPAs (C-8). |
| A.5.24–5.28 Incident management | ❌ | No procedure, no alerting (C-11). |
| A.5.34 Privacy and PII | ⚠️ | See section 3. Consider ISO/IEC 27701 as the privacy extension. |
| A.8.2/8.5 Privileged access, secure authentication | ⚠️ | No admin MFA (S10). PBKDF2 iteration count low (S11). |
| A.8.8 Technical vulnerabilities | ❌ | .NET 9 EOS (S9). No dependency scanning (Dependabot / `dotnet list package --vulnerable`). |
| A.8.9 Configuration management | ⚠️ | Good template and secrets separation, but there is a dangerous TLS flag (S7). |
| A.8.12 Data leakage prevention | ⚠️ | `ex.Message` leaks (L2). |
| A.8.13 Backup | ❌ Not evident | No documented and tested DB backup and restore. |
| A.8.15/8.16 Logging and monitoring | ⚠️ | Serilog file logs with 30-day retention are good. No alerting, and no audit log of admin actions. |
| A.8.20–8.23 Network security, web filtering | ✅ | Security headers, CSP, rate limiting, forwarded-headers hygiene. |
| A.8.24 Cryptography | ⚠️ | DB `Encrypt=False` (S4), FTP (S8). JWT/HMAC usage is sound. |
| A.8.25–8.29 Secure SDLC and testing | ⚠️ | A test project exists but has no security tests and no CI. The S1 regression shows the need for them. |
| A.8.32 Change management | ❌ | Manual FTP deploy from a workstation, with no review gate or CI (S8). |

---

## 5. Recommended order of work

1. **Now:** S1 (re-anchor the anonymous AI quota to IP and add a global budget), S2 (charge credits on all AI endpoints), S3 (input caps).
2. **This week:** S9 (upgrade to .NET 10 before 10 Nov), S4 (DB encryption), S7 (remove the TLS-off flag).
3. **Privacy quick wins:** C-2/C-3 (load GA only after consent, add a withdrawal link), C-4 (retention cleanup job), C-13 (persist purchased credits).
4. **Hardening:** S5, S6, S8, S10, S11, then the Low items.
5. **Paperwork (GDPR/ISO):** records of processing, DPIA, processor DPAs, incident and breach procedure, backup policy, and a short ISMS (scope, risk register, SoA) if ISO certification is a goal.
6. **Prevent regressions:** add tests for the AI usage guard (anonymous key rotation, student charging) and CI with `dotnet test` plus vulnerable-package checks.

---

## 6. Remediation log (2026-11-05)

Build: whole solution on .NET 10, 72 tests passing (15 new security tests). The consent flow was checked in a headless browser.

| Finding | Status | What changed |
|---|---|---|
| **S1** anonymous AI quota bypass | ✅ Fixed | `AiUsageGuard` enforces a per-IP daily cap (`OpenAI:AnonymousIpDailyRequests`, default 200, sized for a school NAT) before the per-device allowance, plus a global daily circuit breaker (`OpenAI:GlobalDailyRequests`, default 5000). Regression tests in `AiUsageGuardTests`. |
| **S2** students free on most AI endpoints | ✅ Fixed | The guard refuses students with no credits. `AiController.TryConsumeRateLimit` deducts one credit atomically, and a failed generation refunds it (`IUserService.RefundCreditsAsync`). |
| **S3** unbounded prompts | ✅ Fixed | 64 KB request limit on `AiController` (2 MB for admin authoring endpoints). Question capped at 4,000 chars and history at 20 turns × 4,000 chars. **Also fixed:** client-sent `system` messages in `ConversationHistory` were forwarded to OpenAI and could override the system prompt. Only `user`/`assistant` turns are kept now. |
| **S4** DB unencrypted | ⚠️ Template fixed | Production templates use `Encrypt=True`. **You:** set the same in the production `appsettings.json` / env var and make sure the SQL Server certificate is trusted. |
| **S5** AI output as raw HTML | ✅ Fixed | This had fully regressed: `AiMarkdown` was unused, and the AI person bio rendered raw HTML. `<MarkdownContent Untrusted="true">` escapes `<` and strips `javascript:`/`data:` URLs. It is used for the person bio, exam summary, comparison and rewrite. Chat output also strips script URLs. |
| **S6** feedback spam | ✅ Fixed | 5 per 10 min per IP, bounded fields (DTO attributes and service-side caps). |
| **S7** OpenAI TLS flag | ✅ Fixed | Honoured only in Development. |
| **S8** plain FTP deploy | ✅ Fixed | Cake scripts use explicit FTPS with OS certificate validation (`ftpEncryption` in `build-ftp*.json`). CI added (`.github/workflows/ci.yml`: build, tests, vulnerable-package gate), plus Dependabot. |
| **S9** .NET 9 EOS | ✅ Fixed | All projects on `net10.0`, EF Core and related packages on 10.0.14. **Also fixed:** a vulnerable transitive `System.Security.Cryptography.Pkcs` 9.0.11 (GHSA-846f-f764-76mx, high), now pinned to 10.0.14. **You:** install the .NET 10 Hosting Bundle on the IIS server before deploying. |
| **S10** shared admin, no MFA | ❌ Open | This needs a design decision: individual admin accounts with TOTP or passkeys. |
| **S11** PBKDF2 100k | ✅ Fixed | 600k iterations. Old hashes still verify and are upgraded on next login. The admin `Auth:Basic:PasswordHash` in config keeps its own count, so regenerate it to upgrade. |
| **L1** login timing | ✅ Fixed | A dummy PBKDF2 run for unknown emails. |
| **L2** `ex.Message` leaks | ✅ Fixed in `AiController` | Generic 502 message. A few non-AI controllers still return validation-style messages, which is intended. |
| **L3** per-account login throttle | ❌ Open | |
| **L5** CSP `unsafe-inline` | ❌ Open | |
| **L6** no security tests / CI | ✅ Fixed | See S8 and the new tests. |
| **C-2** GA before consent | ✅ Fixed | `gtag.js` is injected only after "Souhlasím". Declining switches GA off and deletes the `_ga` cookies. |
| **C-3** withdraw consent | ✅ Fixed | "Nastavení cookies" in both menus reopens the banner. |
| **C-4** retention | ✅ Mechanism added | `DataRetentionService` runs daily. Defaults: unverified accounts 7 d, anonymous AI counters 7 d, push tokens 180 d, feedback 730 d, expired credit grants 1 d. `AiChatDays` and `InactiveAccountDays` are **off** until you state periods in the Privacy Policy. **You:** set them under `Retention` in config and mirror them in the policy. |
| **C-6** export gaps | ✅ Fixed | The export now includes the duty-roster name and feedback sent from the account's email. |
| **C-13** purchased credits lost on restart | ✅ Fixed | New `AiBonusGrants` table (migration `AddAiBonusGrants`). It also makes signed-grant replay protection survive restarts. **You:** apply the migration (`dotnet ef database update`, or generate a script). |
| **C-15** AI disclosure | ✅ Fixed | The chat and AI tool panels state that the content is AI-generated and may be wrong. The chat also asks users not to enter personal data. |
| C-8, C-9, C-11, C-12, C-14, C-16 and the ISO organisational controls | ❌ Open, documentation | DPAs and transfer basis for OpenAI and Google, records of processing, DPIA, breach procedure, operator identification in the legal texts, push-notification consent review, AI Act Annex III reasoning, ISMS scope, risk register and SoA. |

### New configuration keys
```json
"OpenAI": { "AnonymousIpDailyRequests": "200", "GlobalDailyRequests": "5000" },
"Retention": { "UnverifiedAccountDays": 7, "AnonymousAiUsageDays": 7, "DeviceTokenDays": 180,
               "FeedbackDays": 730, "AiChatDays": 0, "InactiveAccountDays": 0 }
```
All keys have code defaults, so they are optional.
