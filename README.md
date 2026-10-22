# Șurub Direct

Magazin de șuruburi și elemente de fixare, reorganizat în servicii independente cu API Gateway. Proiect local de dezvoltare; date demonstrative, fără checkout activ.

## Structura actuală

- `backend/src/services/catalog`: produse, categorii, atribute, imagini, variante, ambalaje, prețuri, publicare și worker Outbox.
- `backend/src/services/inventory`: stoc și recepții idempotente, în bază separată.
- `backend/src/services/identity`: conturi, autentificare, roluri și CSRF, în bază separată.
- `backend/src/services/purchasing`: furnizori, asocieri SKU și condiții curente de achiziție, în bază proprie.
- `backend/src/services/ordering`: host separat, pregătit pentru etapa comenzilor; fără checkout implementat.
- `backend/src/gateway`: YARP, singura adresă API folosită de frontend.
- `frontend/src/apps`: aplicațiile Angular storefront și admin; codul comun este în `frontend/src/shared`.
- `backend/tests` și `frontend/tests`: testele backend, respectiv scenariile browser.
- `backend/src/shared`: infrastructură tehnică minimală; fără entități comerciale comune.

[Arhitectură și limite](docs/architecture.md) · [Organizarea fișierelor](docs/file-organization.md) · [Starea implementării](docs/implementation-status.md)

.NET 10, ASP.NET Core, EF Core/Npgsql, PostgreSQL 17, ASP.NET Identity, YARP 2.3, Angular 22, Angular Material, SSR. xUnit/Testcontainers și Playwright pentru verificare. RabbitMQ nu este încă implementat; va fi introdus împreună cu fluxurile de comandă și rezervare.

Soluția .NET este în `backend/SurubDirect.sln`. Configurația Angular și comenzile npm sunt în `frontend/`.

## Pornire locală

Necesită Docker Desktop pornit, .NET SDK din `global.json`, Node 24, npm și Python 3. Pe acest calculator scripturile folosesc automat runtime-urile locale din `.tools`.

```sh
# La un checkout nou, instalează dependențele Angular:
cd frontend
npm ci
cd ..

# Generează secretele locale lipsă și provisionează bazele distincte:
python3 scripts/dev.py db

# Compilează backend-ul și cele două aplicații Angular:
python3 scripts/dev.py build

# Aplică migrațiile fiecărui serviciu și asigură contul de administrator:
python3 scripts/dev.py init

# Pornește toate API-urile, gateway-ul și worker-ul:
python3 scripts/dev.py up
```

În alte două terminale, din rădăcina proiectului:

```sh
python3 scripts/dev.py storefront
python3 scripts/dev.py admin
```

| Componentă | Adresă locală |
|---|---|
| Magazin | http://localhost:4200 |
| Administrare | http://localhost:4201 |
| Gateway | http://127.0.0.1:5099 |
| Catalog | http://127.0.0.1:5101 |
| Inventory | http://127.0.0.1:5102 |
| Identity | http://127.0.0.1:5103 |
| Ordering | http://127.0.0.1:5104 |
| Purchasing | http://127.0.0.1:5105 |

API-urile interne sunt listate pentru depanare. Frontend-ul apelează gateway-ul. Documentația OpenAPI este disponibilă în Development la `/openapi/catalog/v1.json`, `/openapi/inventory/v1.json` și `/openapi/identity/v1.json` prin gateway.

Datele administratorului sunt în `.env`: `ADMIN_EMAIL`, `ADMIN_PASSWORD`. Nu publica fișierul și nu îl suprascrie pe acest calculator. `db` generează secrete doar dacă lipsesc; nu schimbă parolele unei baze existente. Un fișier `.env` nou este creat cu parole aleatoare și permisiuni restrictive. Nu folosi valorile exemplificative din `.env.example` ca parole reale.

Pentru o bază Catalog goală, poți rula `python3 scripts/dev.py seed-demo` după build. Creează produse și prețuri demonstrative; stocul se înregistrează prin Inventory din administrare. Pe calculatorul acesta, datele existente, inclusiv stocul, au fost transferate din baza veche.

## Datele vechi

Migrarea existentă a păstrat baza `surub_direct` și backup-uri private în `work/backups`. Bazele active sunt `surub_catalog`, `surub_inventory`, `surub_identity`, `surub_ordering`, `surub_purchasing`, fiecare cu utilizator propriu.

Pentru un alt checkout cu vechea bază, oprește vechiul API și rulează `python3 scripts/dev.py migrate-legacy` înainte de `init`. Copiază numai în ținte goale, verifică numărul rândurilor și nu șterge sursa. Nu utiliza migrarea ca mecanism de sincronizare sau rollback după apariția de scrieri noi.

## Verificări

```sh
python3 scripts/dev.py build
python3 scripts/dev.py test
python3 scripts/dev.py verify-isolation
python3 scripts/dev.py check-migrations
# Cu serviciile și ambele aplicații Angular pornite:
python3 scripts/dev.py e2e
```

Testele backend folosesc un container PostgreSQL temporar și baze distincte. Testele browser adaugă un produs în baza locală; categoria „Verificare automată” îl identifică. La prima utilizare: `cd frontend && npx playwright install chromium`.

Poți porni independent un proces: `python3 scripts/dev.py catalog`, `inventory`, `identity`, `ordering`, `purchasing`, `gateway` sau `worker`. `api` este un alias pentru gateway; nu mai pornește monolitul. Fiecare API se poate publica separat cu `dotnet publish <calea-proiectului>`.

Ctrl+C în terminalul `up` închide procesele backend ale acelui grup. Oprește separat Angular; `docker compose stop postgres` păstrează datele PostgreSQL.

## Stadiu comercial

Fluxul funcțional este: produs → variantă → ambalaj/preț → recepție → publicare. Coșul persistent și ciorna PF/PJ sunt implementate; Inventory are rezervări atomice și expirări, integrate cu pregătirea demonstrativă a coșului în Development. Vezi [pregătirea coșului](docs/cart-preparation.md). Confirmarea demonstrativă este disponibilă în Development; comenzile comerciale, plățile, curierul și facturarea rămân de finalizat. Ilustrațiile și TVA-ul din demo nu constituie date comerciale verificate. `noindex` rămâne activ; domeniul nu a fost înregistrat.

Pentru producție sunt necesare configurații dedicate: HTTPS și TLS intern, secrete gestionate, backup, monitorizare, chei Identity persistente, MFA, migrații controlate și domenii permise pentru SSR. `STORE_API_URL` indică gateway-ul pentru SSR, iar `SSR_ALLOWED_HOSTS` lista hosturilor permise. `scripts/dev.py` este exclusiv pentru Development.

### Imagini locale

Administrarea fiecărui produs include panoul „Imaginile produsului”. Sunt acceptate JPEG, PNG și WebP statice, maximum 8 MB, 6000 px/latură și 24 MP; maximum 12 imagini/produs. Textul alternativ este obligatoriu. Fișierele sunt convertite în WebP și miniaturi, în `work/media/catalog` (neversionat). La pornirea manuală a Catalog API configurează `Media__Root` cu un director privat persistent; scripturile locale îl configurează automat. Backup-ul bazei trebuie însoțit de backup-ul fișierelor.

## Import produse

Pagina **Import CSV** din administrare permite încărcarea de produse noi cu variante, ambalaje și prețuri. Include model descărcabil, previzualizare și validări; salvarea creează numai ciorne și nu modifică stocul. Vezi [formatul CSV și limitele](docs/product-import.md).

## Furnizori

Pagina **Furnizori** gestionează datele firmelor și articolele cumpărate: cod furnizor, SKU, ambalaj, cost net și monedă. Purchasing verifică variantele prin HTTP, fără acces la baza Catalog. Vezi [fluxul și limitele actuale](docs/purchasing.md).

Pagina **Aprovizionare** permite pregătirea comenzilor către furnizori, costuri negociate, marcarea manuală a trimiterii și anulare. Vezi [comenzile de aprovizionare](docs/purchase-orders.md). Sunt disponibile [recepții parțiale](docs/purchase-receipts.md) cu protecție împotriva dublării și suprarecepției, plus reluare manuală după eroare.

În administrarea produselor, **Stoc și mișcări** oferă [jurnalul și ajustările de stoc](docs/stock-adjustments.md). [Planul de continuare](docs/roadmap.md) urmărește etapele rămase și automatizarea autorizată.

Rezervările Inventory necesită cheia locală RESERVATIONS_SERVICE_KEY, completată de python3 scripts/dev.py db. [Protocol și limite](docs/stock-reservations.md).

Ordering coordonează intern rezervările prin planuri persistente. Necesită COORDINATOR_SERVICE_KEY și RESERVATIONS_SERVICE_KEY, generate local de comanda db. Fluxul public de checkout rămâne dezactivat. [Detalii](docs/reservation-coordination.md).

Inventory permite consumarea atomică/idempotentă a rezervărilor și o afișează în jurnalul administrativ. Operația este legată la confirmarea demonstrativă cu istoric în coș. [Protocol și limite](docs/stock-consumption.md).

[Comenzile demonstrative](docs/demo-orders.md) păstrează număr unic, datele clientului și prețurile, consumă rezervarea idempotent și păstrează istoricul după golirea coșului. Checkout-ul comercial rămâne dezactivat.

Panoul de administrare include [lista și detaliile comenzilor demonstrative](docs/admin-sales-orders.md), cu filtre, paginare și acces Administrator verificat de Identity.
