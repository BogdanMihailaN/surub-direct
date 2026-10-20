# Verificarea reorganizării — 18 octombrie 2026

Verificări efectuate local după separarea serviciilor:

| Verificare | Rezultat |
|---|---|
| Build soluție .NET | Reușit, 0 erori, 0 avertismente |
| xUnit, Testcontainers și limite de arhitectură | 25 teste trecute |
| Build de producție Angular storefront și admin | Ambele reușite |
| Playwright prin gateway și API-urile directe | 9 scenarii trecute |
| Migrații Catalog, Pricing, Inventory și Identity | Fără diferențe de model |
| Permisiuni PostgreSQL | Fiecare rol poate conecta doar baza proprie dintre cele comerciale; accesul la legacy este refuzat |
| Transfer legacy | Numărul de rânduri verificat pentru fiecare tabel copiat; sursa păstrată |
| Publicare independentă .NET | Catalog, Inventory, Identity, Ordering și Gateway publicate separat, fără DLL-urile serviciilor vecine |
| Endpoint-uri locale și frontend | HTTP 200 pentru serviciile pornite, catalog prin gateway și cele două aplicații Angular |
| Secrete | Nicio valoare din credențialele locale găsită în fișierele versionabile |
| Pornire/oprire grupată | Închiderea grupului oprește API-urile și worker-ul; procesele pot fi repornite |

Scenariile de browser acoperă crearea produsului, adăugarea variantei și ambalajului, prețul, recepția, publicarea, afișarea publică, filtrele dimensionale, căutarea fără rezultate, mobilul și refuzul datelor numerice invalide. Verificările noi confirmă că gateway-ul nu expune `/internal`, API-urile directe cer autentificare, iar o sesiune validă nu poate ocoli CSRF în Catalog sau Inventory.

Testele de integrare PostgreSQL folosesc baze distincte pentru Catalog și Inventory într-un container temporar. Adaptoarele între module din proiectul de teste sunt exclusiv pentru testare; producția folosește HTTP, verificat separat de Playwright.

Limite: nu au fost testate deployment-ul în cloud, încărcarea de producție, disponibilitatea ridicată, integrarea unui broker sau checkout-ul. Componentele respective nu sunt implementate. Ordering a fost verificat ca host independent cu checkout dezactivat, nu ca serviciu de comenzi finalizat.

## Reorganizare backend/frontend

Ulterior separării în servicii, codul a fost grupat în `backend/src` și `frontend/src`, iar testele în `backend/tests` și `frontend/tests/e2e`. Soluția .NET este acum `backend/SurubDirect.sln`.

După mutare au fost rulate din nou build-ul complet .NET, build-urile Angular, cele 25 de teste backend și cele 9 scenarii Playwright: toate au trecut. Referințele `.csproj`, căile aplicațiilor din Angular și ale serviciilor din scripturile locale au fost verificate. CI și documentația folosesc noua structură. Serviciile și ambele interfețe au fost repornite din noile directoare.

## Editare, arhivare și restaurare produse — 18 octombrie 2026

- Build complet .NET: reușit, fără erori sau avertismente.
- `python3 scripts/dev.py test`: 30 teste trecute. Prima încercare a găsit Docker oprit; după pornirea Docker, toate testele PostgreSQL au trecut.
- Build-uri de producție Angular storefront și admin: reușite, cu verificările stricte active.
- `python3 scripts/dev.py e2e`: 9 scenarii trecute. Scenariul administrativ este extins cu filtrarea ciornei fără SKU, editare și anulare, validarea formularului, arhivare și anulare, invizibilitate publică, filtrul de arhivate, restaurare ca ciornă și republicare. Produsul automat este arhivat la sfârșitul scenariului.
- Testele noi verifică persistența editării și păstrarea identității, variantelor, ambalajelor, prețurilor și stocului; arhivarea repetată, restaurarea ca ciornă și interzicerea editării/publicării produsului arhivat; erori 404 pentru ID inexistent; căutarea a 101 ciorne fără variante și accesul la pagina a cincea.
- Accesul anonim și lipsa CSRF sunt refuzate pentru noile endpoint-uri atât prin gateway, cât și direct la Catalog API.
- Formularul de editare a fost verificat vizual la 1440 px și 390 px; fără depășire orizontală pe mobil. Capturile locale sunt în `work/product-edit-desktop.png` și `work/product-edit-mobile.png` (neversionate).

Modificarea folosește starea `Archived` deja existentă și nu schimbă schema bazei. Nu adaugă audit complet sau mecanisme noi pentru concurența între mai mulți administratori. Verificările istorice de migrare și publicare de mai sus nu au fost repetate pentru această etapă.

## Editarea variantelor și ambalajelor — 18 octombrie 2026

- Build .NET complet: 0 erori, 0 avertismente. Build-urile de producție Angular storefront și admin au trecut.
- `python3 scripts/dev.py test`: 44 teste trecute, inclusiv validarea atomică a dimensiunilor/cantităților, recalcularea căutării, păstrarea SKU/codurilor și a relațiilor, verificarea apartenenței, protecția arhivatelor și trecerea în ciornă la schimbarea bucăților/ambalaj.
- Integrarea PostgreSQL confirmă că identificatorii, prețul și soldul rămân aceleași, iar retransmiterea unei recepții înregistrate înainte de editare nu dublează stocul.
- Playwright: 10 scenarii verificate cu succes. Cele 9 scenarii existente au trecut la rularea completă; scenariul nou a necesitat corectarea unui selector de test pentru textul compus al celulei ambalajului și a trecut la rerularea separată. Nicio modificare de aplicație nu a fost necesară pentru acea corecție.
- Noul scenariu verifică editarea și anularea, dimensiuni cu prea multe zecimale și cantități fracționare respinse, confirmarea schimbării ambalajului, invizibilitatea ciornei, păstrarea prețului și stocului și afișarea după republicare. Produsul automat este arhivat la final.
- Protecțiile fără autentificare și fără CSRF includ noile rute PUT, prin gateway și direct la Catalog.
- Formularele au fost inspectate la 1440 px și 390 px, fără depășire orizontală; capturile sunt locale în `work/variant-edit-*.png` și `work/offer-edit-*.png`.

Nu s-a modificat schema DB și nu s-au introdus apeluri de scriere în Inventory. Schimbarea bucăților/ambalaj și trecerea în ciornă se salvează împreună în Catalog. Verificarea prețului înainte de republicare este o acțiune a administratorului; nu există recalculare automată sau audit complet al editărilor.

## Categorii și atribute tehnice — 19 octombrie 2026

- Build complet .NET: 0 erori și 0 avertismente. Ambele build-uri Angular de producție au trecut; storefront a fost recompilat după corectarea reîncercării cererilor eșuate.
- `python3 scripts/dev.py test`: 50 de teste trecute. Acoperire nouă: cicluri, părinți inexistenți, adâncime, limita atributelor moștenite, tipuri și precizie numerică, filtrare pe aceeași variantă, moștenire, redenumire, păstrarea valorilor inactive, apartenența SKU și produse arhivate. Două mutări concurente care ar forma un ciclu nu se pot salva împreună.
- Migrarea a fost verificată pe o bază temporară cu schema veche și produse/variante/ambalaje deja existente. Identificatorii, SKU, dimensiunile, starea și ambalajele sunt păstrate; nu sunt inventate valori pentru atributele noi.
- Baza locală Catalog a fost salvată în `work/backups` înainte de migrare. Imediat după aplicare s-au verificat aceleași 23 de produse, 24 de variante și 25 de oferte, fără produse cu categorie lipsă. Testele de browser ulterioare creează date demonstrative suplimentare. Baza legacy nu a fost modificată.
- `python3 scripts/dev.py check-migrations`: Catalog, Pricing, Inventory și Identity fără diferențe între model și migrații.
- `python3 scripts/dev.py e2e`: rularea finală completă, 13 scenarii trecute. Include categorii/subcategorii, atribut numeric moștenit, editarea valorii pe SKU, intervale în storefront, păstrarea filtrelor la reîncărcare, afișarea în detaliul produsului și reîncercarea unei cereri 503 cu același URL.
- Noile mutații administrative refuză accesul anonim și lipsa CSRF atât prin gateway, cât și direct la serviciu. Listele cu elemente `null` și valorile de tip greșit primesc 400.
- Prima rulare de browser a identificat doi pași de test de adaptat: selectorul pentru lista de categorii și așteptarea resetării formularului înainte de completarea subcategoriei. După corectare, toate scenariile au trecut împreună.
- Administrarea a fost inspectată vizual la 1440 px și 390 px, fără depășire orizontală. Capturile locale sunt `work/taxonomy-admin-desktop.png`, `work/taxonomy-admin-mobile.png` și `work/taxonomy-storefront.png`.

Produsele create de noile scenarii sunt arhivate la final; definițiile și categoriile de test rămân vizibile în administrare. Categorii fără produse publicate nu apar în magazin. Limitele comerciale (date demonstrative, noindex, checkout dezactivat) rămân valabile.

## Imagini produse — 19 octombrie 2026

- Build complet .NET: 0 erori și 0 avertismente; build-uri Angular de producție storefront și admin reușite, fără avertismente de șablon la verificarea finală.
- `python3 scripts/dev.py test`: 67 teste trecute. Testele noi acoperă ordinea și imaginea principală, limita de 12 imagini, textul alternativ, asocierea SKU, produse arhivate, redimensionarea celor trei formate acceptate, cele opt orientări EXIF, respingerea fișierelor deteriorate/SVG/prea mari și a dimensiunilor excesive.
- Integrarea PostgreSQL și filesystem confirmă păstrarea metadatelor, citirea privată a ciornelor, accesul public doar după publicare, ascunderea după arhivare, ștergerea fișierului și promovarea altei imagini principale. Două încărcări simultane păstrează o singură imagine principală și poziții distincte.
- Pornirea completă a identificat inițial înregistrarea handler-elor media și în worker-ul Outbox, care nu are nevoie de storage. Înregistrarea lor a fost mutată în `AddCatalogMedia`; un test separat validează configurația worker-ului fără dependențe media. După corecție toate procesele au pornit.
- Backup privat Catalog creat înainte de migrare. Cele 27 produse, 28 variante și 29 oferte au rămas identice imediat după aplicare; migrarea adaugă doar tabelul ProductImages, indexurile și relațiile. Testele de browser ulterioare adaugă date demonstrative. Baza legacy a fost păstrată.
- `python3 scripts/dev.py check-migrations`: Catalog, Pricing, Inventory și Identity fără diferențe de model.
- `python3 scripts/dev.py e2e`: 15 scenarii trecute în rularea finală completă. Upload real prin browser, miniatură WebP, ordine, principală, text alternativ, SKU, refuz de SVG cu MIME fals, galerie și card public, eliminare/anulare și blocarea imaginii după arhivare.
- Endpoint-urile de imagini cer autentificare și CSRF pentru mutații atât prin gateway, cât și direct la Catalog. Imaginile administrative nu sunt accesibile anonim. Răspunsurile media sunt no-store și nu servesc fișiere fără metadate.
- Panoul admin și galeria au fost inspectate pe desktop și la 390 px, fără depășire a viewport-ului. Capturile `work/images-admin-*.png` și `work/images-storefront-*.png` folosesc o imagine geometrică generată exclusiv ca fixture de test, fără a o prezenta ca fotografie comercială.
- Verificarea fișierelor versionabile nu a găsit valorile secretelor locale. Fișierele media, backup-urile și logurile rămân ignorate de Git. Scenariul nou își elimină imaginile și arhivează produsul la final.

Limite: adaptor local, fără cloud/CDN sau curățare automată a fișierelor orfane; nu s-au verificat upload-uri sub încărcare de producție. Nu se păstrează originalele. Limitele și procedura de backup pentru fișiere sunt documentate în architecture.md și README.md.

## Import CSV — 19 octombrie 2026

- `python3 scripts/dev.py test`: **87 teste trecute**, fără teste omise. Parserul verifică BOM, ghilimele/escape, separatori, linii multiple, numere și limite. Testele PostgreSQL verifică previzualizarea fără scrieri, ciorne cu mai multe ambalaje per SKU, conflicte în fișier și cu date existente, rollback la eșecul Pricing și două importuri prevalidate concurente. Limitele de dependențe includ și Store.Application.
- Build .NET reușit; build-uri Angular de producție pentru storefront/admin și `format:check` reușite.
- `python3 scripts/dev.py e2e`: **17 scenarii trecute**, inclusiv import cu previzualizare, erori pe linii, confirmare, prețuri, lipsa modificărilor de stoc, reimport respins și protecție autentificare/CSRF prin gateway și Catalog direct. Produsele testului de import sunt arhivate la final.
- Pagina de import inspectată vizual pe desktop și mobil, fără depășirea viewport-ului; tabelul are derulare proprie. Capturile sunt private în `work/import-desktop.png` și `work/import-mobile.png`.
- `git diff --check` fără probleme și nicio valoare de secret local găsită în fișierele versionabile. Nu s-au adăugat migrații sau modificat schemele pentru import.
- După întreruperea sesiunii, prima reluare Playwright a întâlnit serviciile oprite. Docker, PostgreSQL, API-urile și aplicațiile Angular au fost repornite; rezultatul de 17/17 este al rulării complete ulterioare.

## Purchasing: furnizori și articole — 19 octombrie 2026

- Build .NET reușit, fără avertismente; `python3 scripts/dev.py test`: **106 teste trecute**, fără teste omise. Acoperă validările furnizorului și costurilor, normalizarea identității fiscale, unicitatea, editarea/dezactivarea, apartenența articolelor și asocieri concurente. Testele HTTP disting un SKU lipsă de indisponibilitatea/răspunsul invalid al Catalogului; testul PostgreSQL pentru directorul SKU verifică includerea ciornelor și excluderea produselor arhivate.
- Migrarea inițială Purchasing a fost generată și aplicată în baza nouă `surub_purchasing`. `python3 scripts/dev.py check-migrations`: fără diferențe pentru Catalog, Pricing, Inventory, Identity și Purchasing. Nu au fost modificate schemele comerciale existente sau baza legacy.
- `python3 scripts/dev.py verify-isolation`: fiecare dintre cele cinci roluri are acces numai la propria bază, fără acces la celelalte baze comerciale sau legacy.
- Publicare Release independentă Purchasing reușită în `work/publish/purchasing`, fără DLL-uri Catalog, Inventory, Identity sau Ordering.
- Build-uri de producție Angular storefront/admin și `format:check` reușite. `python3 scripts/dev.py e2e`: **19 scenarii trecute**, inclusiv fluxul furnizor–SKU, două surse pentru același SKU, editarea ambalajului/costului, inactivare/reactivare, lipsa schimbărilor în Catalog/Inventory și protecțiile autentificare/CSRF direct/prin gateway. Rutele interne de căutare nu sunt expuse de gateway.
- Aspect verificat pe desktop și mobil; tabelul articolelor are derulare proprie, fără depășirea viewport-ului. Capturi private: `work/purchasing-desktop.png`, `work/purchasing-mobile.png`. Datele demonstrative ale testelor sunt dezactivate/arhivate, fără ștergere fizică.
- Un test PostgreSQL a identificat o proiecție LINQ netraductibilă; interogarea a fost corectată și suita completă a trecut ulterior. Prima încercare a noului test browser a folosit un selector nepotrivit pentru lista de monede; selectorul a fost corectat și datele acelui test au fost dezactivate/arhivate.
- `git diff --check` fără probleme; scanarea fișierelor versionabile nu a găsit valorile secretelor locale. `.env`, datele runtime și artefactele publicării rămân excluse din Git.

## Comenzi de aprovizionare — 20 octombrie 2026

- `python3 scripts/dev.py test`: **111 teste trecute**, fără teste omise. Noile teste verifică snapshot-urile, costurile și cantitățile, stările documentului, blocarea editării după trimitere, persistența/ștergerea liniilor, numărul generat și conflictele între editări concurente. Lipsa sau indisponibilitatea Catalog produce rollback la adăugare și păstrează ciorna la încercarea de trimitere.
- Migrarea `PurchaseOrders` adaugă două tabele în Purchasing. Aplicată local după un backup privat PostgreSQL; furnizorii existenți și baza legacy sunt păstrați.
- Buildurile Angular de producție storefront/admin și verificarea Prettier au trecut.
- `python3 scripts/dev.py e2e`: **21 scenarii trecute**. Fluxul nou creează ciorna, adaugă linia, negociază costul, modifică ulterior asocierea furnizorului, provoacă un conflict de versiune, reîncarcă, marchează trimiterea și anulează. Verifică snapshot-urile, blocarea editării, lipsa modificărilor de stoc/preț și autentificarea/CSRF pe toate mutările noi, direct și prin gateway.
- Aspect desktop și mobil inspectat; tabelul are derulare proprie fără depășirea paginii. Capturi private în `work/orders-desktop.png` și `work/orders-mobile.png`. Comenzile de test sunt anulate, furnizorii dezactivați și produsele arhivate.
- Prima încercare backend a găsit Docker oprit; după pornire și provisionarea PostgreSQL, suita completă a trecut. Artefactele runtime, backupurile și secretele nu sunt versionate.
- Verificarea modelelor EF: fără diferențe față de migrări pentru Catalog, Pricing, Inventory, Identity și Purchasing. Scanarea secretelor în fișierele versionabile a trecut.

## Recepții parțiale Purchasing → Inventory — 20 octombrie 2026

- `python3 scripts/dev.py test`: **122 teste trecute**, fără teste omise. Acoperă rezervarea persistentă a cantității, pierderea răspunsului după aplicarea stocului, reluarea cu aceeași cheie, refuzul unui payload diferit, unicitatea documentului pe linie, concurența între recepții și refuzul anulării după recepție. Clientul HTTP are teste pentru transmiterea sesiunii/CSRF, confirmarea unei scrieri originale sau duplicate și răspunsuri neconfirmate.
- Migrarea `PurchaseReceipts` a fost inspectată și aplicată după backup privat al bazei Purchasing. Adaugă jurnalul cu relații locale, index unic linie/document și limită de cantitate; nu modifică schema Inventory sau baza legacy.
- Builduri Angular de producție storefront/admin și Prettier trecute. Modelele EF ale celor cinci contexte nu au diferențe față de migrări.
- Fluxul browser pentru recepții verifică două livrări de 40 și 160 bucăți, pierderea intenționată a răspunsului către browser, reluarea identică, respingerea dublării/suprarecepției/anulării și soldul real final de 200 bucăți. Autentificarea și CSRF sunt verificate direct și prin gateway pentru creare și retry.
- Aspectul desktop/mobil a fost inspectat; tabelele au derulare proprie. La recepția integrală, formularul este înlocuit cu confirmarea cantității. Capturile sunt private în `work/receipts-desktop.png` și `work/receipts-mobile.png`.
- Două scenarii din prima rulare completă au fost întrerupte de reload-ul Angular în timpul ultimei ajustări vizuale; suita a fost reluată după terminarea editărilor. Furnizorii demonstrativi sunt dezactivați și produsele arhivate; documentele și recepțiile rămân în istoric, fără ștergerea stocului.
- Rularea finală izolată `python3 scripts/dev.py e2e`: **23 scenarii trecute**. O reluare intermediară a primit 429 la taxonomia publică, confirmat în trace: rulările succesive au consumat limita de 600 cereri/minut a gateway-ului. După resetarea ferestrei, suita completă a trecut fără modificarea politicii de limitare.
- Scanarea secretelor în fișierele versionabile și `git diff --check` au trecut; backupurile, logurile și capturile rămân excluse din Git.

## Ajustări de stoc și jurnal — 20 octombrie 2026

- `python3 scripts/dev.py test`: **129 teste trecute**, fără teste omise. Verifică inclusiv ajustări concurente, retry identic, payload modificat, rollback la sold insuficient, protecția rezervărilor, overflow și paginarea istoricului mixt de recepții/ajustări.
- Migrarea `StockAdjustments` a fost inspectată și aplicată după backup privat Inventory. Adaugă o tabelă; păstrează soldurile și recepțiile. Verificarea modelelor EF a trecut pentru toate cele cinci contexte.
- Ambele builduri Angular de producție și Prettier au trecut. `python3 scripts/dev.py e2e`: **25 scenarii trecute**; fluxul nou verifică pierderea răspunsului, retry fără dublare, sold schimbat de o recepție concurentă, ajustare plus/minus, jurnal și protecțiile autentificare/CSRF direct/prin gateway.
- Capturile private `work/stock-adjustments-desktop.png` și `work/stock-adjustments-mobile.png` verifică formularul și istoricul. Testul confirmă lipsa overflow-ului orizontal al paginii; tabelul are scroll propriu.
- Scanarea secretelor și `git diff --check` au trecut. Datele demonstrative sunt arhivate; istoricul stocului rămâne intact. Automatizarea rămâne activă pentru etapele din `roadmap.md`, conform confirmării explicite a utilizatorului; activarea comercială și publicarea în producție necesită acord separat.

## Corecții legate de recepții — 20 octombrie 2026

- `python3 scripts/dev.py test`: **141/141** teste trecute. Corecții concurente/idempotente, răspuns pierdut, cantitate netă și capacitate blocată până la confirmare, protecția rezervărilor, rollback fără jurnal fals, păstrarea originalului și contractul HTTP.
- Build .NET: 0 erori și 0 avertismente. Build-uri Angular de producție storefront și admin reușite; verificarea Prettier și `git diff --check` trecute.
- Cele **28 de scenarii Playwright** au fost verificate: 26 au trecut în rularea completă; ultimele două au atins limita gateway (429) și au trecut separat, împreună cu scenariul principal de taxonomie, după resetarea ferestrei. Limita de trafic nu a fost modificată. Cele trei scenarii noi verifică istoricul, răspunsul pierdut și retry fără scădere dublă, recepționarea diferenței, CSRF și izolarea endpoint-ului intern.
- În prima rulare țintită, serverul Angular de dezvoltare păstrase un șablon gol pentru istoricul stocului după actualizare; repornirea a eliminat cache-ul învechit. Scenariul de ajustări a trecut ulterior în suita completă. Build-ul de producție nu a avut această problemă.
- Capturi desktop/mobil și verificare de overflow pentru corecții, păstrate privat în `work/corrections-*.png`; inspecția mobilă confirmă organizarea formularelor și istoricului.
- Toate cele cinci contexte EF sunt fără diferențe de model. Migrațiile additive Inventory/Purchasing au fost aplicate local după backup privat al ambelor baze; legacy a fost păstrată.
- Verificarea fișierelor versionabile nu a găsit valorile secretelor locale. Automatizarea rămâne **PAUSED**, conform cererii utilizatorului; această etapă a fost continuată manual.

Limite: reluarea corecțiilor este manuală; nu există anulare a unei intenții în așteptare sau flux de retur către furnizor. Nu au fost activate checkout-ul sau integrarea comercială și nu s-a făcut deployment în producție.

## Închiderea comenzilor de aprovizionare — 20 octombrie 2026

- `python3 scripts/dev.py test`: **146/146** teste trecute. Închidere cu rest motivat, livrare integrală, operații în așteptare, restricții după închidere, audit persistent și filtrare Closed. Concurența închidere/recepție permite o singură operație pe versiunea inițială. Sumele multi-linie peste limita Int32 folosesc Int64.
- Build .NET: 0 erori și 0 avertismente. Ambele build-uri Angular de producție reușite, cu verificări TypeScript/template stricte.
- **11/11 scenarii Playwright afectate** trecute: închidere, comenzi, recepții, corecții și ajustări de stoc. Noile scenarii acoperă închiderea parțială după corecție, motivul obligatoriu, pierderea răspunsului și reîncărcarea, filtrarea comenzilor închise, blocarea documentelor noi, stocul păstrat, închiderea integrală fără motiv și autentificare/CSRF direct și prin gateway.
- Capturi desktop și mobil în `work/closure-*.png`; verificare de overflow și inspecție vizuală mobilă efectuate.
- Cinci modele EF fără diferențe față de migrații. Migrația Purchasing adaugă trei coloane, după backup privat al bazei, fără modificări ale recepțiilor sau bazei legacy.
- Fișierele modificate au trecut verificarea secretelor locale și `git diff --check`. Automatizarea rămâne oprită.

Închiderea este operațională, fără integrare contabilă sau notificare a furnizorului. Redeschiderea și corecțiile noi post-închidere nu sunt implementate; interfața comunică această restricție înainte de confirmare.
