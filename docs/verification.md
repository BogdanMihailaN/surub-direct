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

## Coș demonstrativ și recalculare Ordering — 20 octombrie 2026

- `python3 scripts/dev.py test`: **162/162** teste trecute, inclusiv arhitectura Ordering, cantități minime/pași, limite, lipsă produs/preț, monede diferite, stoc agregat pe SKU și răspunsuri Catalog nevalide. Prima rulare a fost blocată de Docker oprit; după pornirea Docker Desktop și PostgreSQL, suita a trecut.
- Build .NET reușit, fără erori sau avertismente; storefront SSR și admin compilate în producție cu verificări stricte.
- **12/12 scenarii Playwright afectate** trecute: coș, catalog, administrarea produselor și protecțiile gateway/API. Selecția persistă după reload fără prețuri salvate, prețul se recalculează după modificarea administrativă, stocul aceluiași SKU este cumulat, arhivarea invalidează linia, 503 elimină totalul precedent și permite reluarea. Coșul corupt local este recuperabil. Checkout-ul rămâne fals, ruta de creare comandă nu există și soldul/rezervările nu se modifică.
- Capturi desktop/mobil în `work/cart-*.png`, cu inspecție vizuală și verificare de overflow. Legătura către coș este accesibilă și pe mobil.
- Nu sunt introduse tabele sau migrații; bazele și credentialele rămân separate. Nu se salvează prețuri sau date personale în localStorage.
- Prettier, `git diff --check` și verificarea secretelor locale trecute. Automatizarea rămâne oprită.

Limite: selecție locală în browser, fără persistență server sau cont; totaluri demonstrative, fără rezervare, date PF/PJ, comandă, plăți, transport sau facturare. Verificarea stocului nu este atomică între produse și nu garantează disponibilitatea la un checkout viitor.

## Persistența coșului anonim — 21 octombrie 2026

- Backend: 166 de teste trecute, inclusiv persistență PostgreSQL între scope-uri, izolarea proprietarilor, concurență cu un singur câștigător, refuzul retry-ului cu versiune veche, expirare și validarea selecțiilor.
- Buildurile de producție Angular storefront și admin au trecut; verificarea Prettier a trecut.
- 16 scenarii Playwright distincte trecute: suita inițială de 15 pentru coș, persistență, catalog, administrare și servicii, apoi cele 6 scenarii afectate de recuperarea localStorage (inclusiv un scenariu nou). CSRF și izolarea sunt verificate atât prin gateway, cât și direct pe Ordering. Importul local, răspunsul pierdut după salvare, notificarea între taburi și indisponibilitatea citirii sunt acoperite.
- Migrarea Ordering a fost aplicată după backup privat al bazei. EF raportează că nu există diferențe între model și migrare.
- Izolarea bazelor a trecut pentru toate cele cinci servicii; accesul la bazele vecine și legacy este refuzat.
- Capturile desktop și mobil ale coșului au fost verificate; mobilul nu depășește viewport-ul.

Limite: fără teste de încărcare sau deployment de producție; fără curățare fizică periodică a coșurilor expirate, cont de client sau comandă. Checkout-ul rămâne dezactivat.

## Ciornă PF/PJ — 21 octombrie 2026

- 180 de teste backend trecute. Cazurile noi verifică normalizarea și validarea contactului/adreselor/firmei, coșul gol, expirarea, versiunea, izolarea proprietarilor, citirea din PostgreSQL în alt scope și ștergerea datelor.
- Ambele builduri Angular de producție au trecut.
- 13 scenarii Playwright pentru datele clientului, persistența coșului, recalculare și catalog au trecut. Verificate: PF → PJ → PF, facturare separată, reluare după reload, ștergere explicită, CSRF/ownership direct și prin gateway, versiune depășită, golire și recuperarea răspunsului pierdut fără repetarea scrierii.
- Formularul mobil a fost inspectat vizual; nu depășește viewport-ul.
- Migrarea CartCustomerDraft a fost aplicată după backup privat al bazei Ordering. EF nu raportează diferențe între model și migrare.
- Nu se creează comenzi, rezervări, plăți sau facturi. Datele testelor sunt fictive. Retenția fizică automată și configurația de producție rămân în backlog.

## Rezervări de stoc — 21 octombrie 2026

- 190 de teste backend trecute, inclusiv rollback după actualizarea primei linii, concurență fără suprarezervare, retry simultan, payload schimbat, ajustare sub Reserved, anulare înaintea creării și concurență între expirare/eliberare.
- 7 scenarii Playwright trecute pentru rezervări, coș și protecția serviciilor. Testul de expirare a așteptat durata reală de 60 de secunde și a verificat soldul prin API-ul de citire, fără a declanșa expirarea prin citirea rezervării.
- Cheia de citire Inventory și sesiunea administrativă nu autorizează rezervările; cheia dedicată funcționează numai direct, ruta internă fiind blocată la gateway.
- Ambele builduri Angular și verificarea Prettier au trecut.
- Migrarea Inventory a fost aplicată după backup privat. Nu există diferențe între model și migrare.
- Nu s-au activat checkout-ul, integrarea Ordering sau consumarea rezervărilor. Nu s-a efectuat test de încărcare ori de failover distribuit.

## Coordonarea rezervărilor în Ordering — 21 octombrie 2026

- 199 de teste backend trecute după separarea coordonării în handler-ul Application. Cazurile noi acoperă răspunsul pierdut la rezervare/eliberare, reluarea din alt scope, anularea înaintea creării, refuzul Inventory, termenul local depășit și răspunsuri HTTP neconforme.
- 8 scenarii Playwright trecute pentru coordonare, coș persistent și limitele de securitate. Fluxul real verifică Ordering → Inventory → sold Reserved, anulare, retry, payload schimbat și refuzul cheilor nepotrivite; gateway-ul nu expune rutele interne.
- Buildurile de producție Angular au trecut; modelul Ordering nu are diferențe față de migrare.
- Migrarea ReservationCoordination a fost aplicată după backup privat al bazei Ordering.
- Nu s-au activat generarea planului din coș, confirmarea comenzii, consumarea rezervării, RabbitMQ sau checkout-ul. Nu s-au efectuat teste de încărcare/failover de producție.


## Pregătirea demonstrativă a coșului — 21 octombrie 2026

- `python3 scripts/dev.py test`: 204 teste backend trecute, inclusiv tranzacția snapshot/plan/coș, SKU-uri cumulate, replay fără recalculare, concurență, versiune stale la commit, dependență indisponibilă și anulare fără deblocare prematură.
- Build-uri Angular de producție storefront SSR și admin reușite; verificarea Prettier trecută.
- 15 scenarii Playwright trecute: 2 noi pentru pregătire și protecțiile HTTP, plus 13 regresii pentru ciorna PF/PJ, persistența coșului, recalculare și catalog.
- Browserul confirmă recuperarea după răspuns pierdut, 35 de bucăți rezervate pentru două ambalaje ale aceluiași SKU, prețul snapshot-ului păstrat după editarea Catalog, eliberarea stocului și recalcularea după reluarea editării. Checkout false și endpoint-ul orders 404.
- Protecții CSRF și cookie proprietar verificate prin gateway și direct pe Ordering. Captura mobilă a componentei inspectată; fără depășire orizontală la 390 px.
- Migrarea PreparedCartSnapshots aplicată local după backup privat al bazei Ordering; verificarea EF Core confirmă că modelul nu are modificări fără migrare. Baza legacy păstrată.

Fluxul este demonstrativ și disponibil numai în Development. Nu au fost testate comenzi comerciale, plăți, consumarea rezervărilor, broker sau lansarea în producție.


## Consumarea rezervărilor — 22 octombrie 2026

- `python3 scripts/dev.py test`: 213 teste trecute, inclusiv consumarea concurentă fără dublare, rollback pe mai multe SKU-uri, concurență cu release/expiry, identificatori greșiți, termen inclusiv și jurnalul paginat. Docker era oprit la prima încercare; după pornire, suita completă a trecut.
- Ordering: testele verifică acceptarea Consumed din HTTP, oprirea procesării planului terminal și refuzul detașării pregătirii consumate.
- Build-uri Angular storefront SSR și admin reușite; Prettier trecut.
- 6 scenarii Playwright trecute: consum intern/jurnal admin, ajustări/protecții, pregătirea coșului și coordonatorul de rezervări. Noul scenariu verifică cheia dedicată, refuzul cheii de citire și al sesiunii admin, gateway 404, replay fără dublare și afișarea scăderii de 12 bucăți.
- Captura mobilă a jurnalului a fost inspectată; pagina nu depășește viewportul de 390 px, tabelul permite derulare internă.
- Backup privat Inventory înainte de migrarea StockConsumptionLedger. Migrarea aplicată local; EF Core confirmă absența diferențelor de model. Baza legacy păstrată.

Confirmarea comenzii în Ordering nu solicită încă acest consum. Nu sunt activate checkout-ul, plățile sau indexarea. Procesele aplicațiilor pornite temporar pentru verificare sunt oprite după încheiere.


## Confirmarea comenzilor demonstrative — 22 octombrie 2026

- `python3 scripts/dev.py test`: 228 teste trecute. Noile verificări acoperă crearea atomică a comenzii/intentei, confirmări concurente, păstrarea snapshot-ului, răspuns pierdut după consum, rollback local după succes remote, reluare după termenul local, expirare remote, concurență anulare/confirmare, izolarea proprietarului și limitele temporale inclusive.
- Build-urile de producție Angular storefront SSR și admin reușite; Prettier și verificarea modelului Ordering trecute.
- 13 scenarii browser verificate: 11 regresii pentru coș/ciornă/pregătire și 2 scenarii noi pentru confirmare/istoric/protecții HTTP. Lotul inițial a trecut cele 11 regresii și a atins limita cart-storage la scenariile noi (429); cele două au trecut la rularea separată, cu limita aplicației păstrată.
- Browserul confirmă o singură comandă și un singur consum de 35 de bucăți după pierderea răspunsului de confirmare; totalul rămâne 42,35 RON, iar coșul poate fi golit fără pierderea istoricului. Checkout comercial false, orders 404.
- Capturile mobile pentru confirmare și istoricul extins au fost inspectate; fără depășire orizontală la 390 px.
- Migrarea DemoOrderConfirmation aplicată local după backup privat Ordering; baza legacy păstrată. Procesele pornite temporar pentru testare sunt oprite la final.

Nu au fost activate plăți/livrări/facturi sau checkout comercial. Comenzile sunt demonstrative și accesibile prin cookie-ul anonim; administrarea comenzilor de vânzare și politicile de retenție rămân în backlog.


## Consultarea administrativă a comenzilor — 22 octombrie 2026

- `python3 scripts/dev.py test`: 235 teste backend trecute. Acoperirea nouă verifică paginarea cu 28 de comenzi, ordinea numerelor salvate, filtrele, snapshot-ul clientului și produsele, starea rezervării, identificatorii lipsă și parametrii invalizi.
- Build-urile Angular de producție storefront SSR și admin și verificarea Prettier au trecut.
- 7 scenarii browser distincte validate: administrarea comenzilor (2), comenzile demonstrative (2) și serviciile/protecțiile existente (3). Prima rulare a trecut 6 scenarii și a identificat eticheta ambiguă a selectorului de stare; după corecția accesibilității, ambele scenarii administrative au trecut.
- Verificate filtrele, detaliile PJ cu adrese distincte, recuperarea după 503 fără afișarea unei liste false goale, accesul autentificat și 401 fără sesiune prin gateway și direct pe Ordering, precum și `Cache-Control: no-store`.
- Capturile desktop și mobil inspectate; pagina nu depășește viewportul de 390 px, iar tabelul permite derulare internă.
- Nu sunt modificări de model sau migrări noi. Comenzile rămân demonstrative, disponibile numai în Development; plățile, livrările și facturarea comercială sunt inactive.


## Anularea comenzilor demonstrative — 22 octombrie 2026

- `python3 scripts/dev.py test`: 247 teste backend trecute. Noile teste acoperă restituiri concurente fără dublare, păstrarea consumului original, refuzul rezervărilor neconsumate/identificatorilor greșiți, rollback integral la depășirea soldului maxim, audit nemodificat la replay și motive conflictuale.
- Coordonarea Ordering verificată cu răspuns pierdut după restituire, termen original depășit, eșec al commitului local după succes remote, coș blocat pe durata anulării și istoric păstrat după golire. Clientul HTTP refuză stări diferite de Restored pentru confirmarea restituirii.
- 7 scenarii Playwright trecute: anulare (2), consultare administrativă (2), confirmare/istoric/protecții (2), consum și jurnal (1). Browserul pierde răspunsul după salvarea intenției, recuperează Cancelled și verifică sold 100, Reserved 0 și o singură restituire de 35 de bucăți, inclusiv după replay direct pe Ordering.
- CSRF fără token refuzat atât prin gateway cât și direct; utilizatorul anonim nu poate anula, cheia internă este obligatorie și gateway-ul nu expune restore.
- Ambele build-uri Angular de producție trecute; Prettier verificat. Formularul de anulare mobil inspectat, fără depășire orizontală la 390 px.
- Migrările StockRestorationLedger și DemoOrderCancellation aplicate local după backup-uri private; modelele EF Inventory și Ordering nu au diferențe fără migrare. Baza legacy păstrată.

Flux exclusiv demonstrativ: fără retur parțial, rambursări, anulări de expediere/factură sau checkout comercial. Procesele aplicațiilor pornite temporar sunt oprite la final.


## RabbitMQ pentru rezultatele rezervărilor — 23 octombrie 2026

- `python3 scripts/dev.py test`: 253 teste backend trecute, cu PostgreSQL și RabbitMQ reale în containere izolate. La reluarea din 23 octombrie Docker era oprit; după pornire, suita completă a trecut.
- Verificate Outbox atomic cu stocul, rollback fără eveniment, publicare repetată după pierderea confirmării, Inbox concurent fără aplicare dublă, reconectare înainte de ACK, retry persistent și dead-letter, contract invalid, ID reutilizat cu alt corp și eveniment Consumed întârziat după Restored.
- Testul explicit fără rută confirmă refuzul mandatory cu PublishReturnException/NO_ROUTE. Aserțiunea inițială cerea exact clasa de bază PublishException; a fost corectată pentru excepția concretă a clientului 7.2.2.
- 7 scenarii Playwright trecute: confirmare/istoric/protecții (2) cu brokerul oprit; consultare administrativă (2), anulare (2) și consum/jurnal (1) cu brokerul activ.
- Oprirea brokerului a lăsat un mesaj nepublicat în Outbox fără blocarea confirmării. După repornire, worker-ele au recuperat automat: pending Outbox 0, Inbox 1. Conturile distincte Inventory/Ordering s-au conectat cu permisiunile configurate local.
- Migrările ReservationOutcomeOutbox și ReservationOutcomeInbox aplicate după backup privat. EF confirmă că nu există diferențe de model fără migrare. Baza legacy păstrată.
- Sintaxa scripturilor Python, scanarea pentru secrete și `git diff --check` trecute. Nu sunt modificări frontend în această etapă. Porturile aplicațiilor de test sunt închise la final.

Brokerul transportă rezultatele Consumed/Restored; comenzile și reconcilierea rămân HTTP. Nu sunt implementate încă panoul de monitorizare/reluare a mesajelor, propagarea tuturor evenimentelor, clusterul de producție sau checkout-ul comercial.


## Administrarea mesageriei — 23 octombrie 2026

- `python3 scripts/dev.py test`: 261 teste backend trecute. Acoperă deduplicarea arhivării concurente, o nouă generație de eșec după replay, auditul nemodificat, request-id reutilizat, motiv invalid, payload invalid/supradimensionat păstrat fără reluare și publicare reluabilă după confirmare pierdută.
- Broker real: monitorizare pasivă fără creare de cozi, diferențiere între indisponibil și gol, păstrarea replay-id în retry/dead-letter. Migrarea FailedMessageRecovery aplicată după backup privat Ordering; EF nu raportează diferențe de model fără migrare.
- Ambele build-uri Angular de producție și Prettier trecute. Captura mobilă a paginii Mesagerie inspectată, fără depășire orizontală la 390 px.
- 6 scenarii browser distincte trecute: confirmare (2), anulare (2), monitorizare/reluare și securitate (2). Cele două de mesagerie au fost reluate după corectarea antetului numeric RabbitMQ primit ca Int64, cu verificarea explicită a celor 5 încercări afișate.
- Browserul verifică mesaj invalid blocat, confirmare explicită, răspuns pierdut după salvarea cererii, publicare ulterioară, audit, replay idempotent direct pe Ordering, conflict pentru alt request-id, CSRF și autentificare inclusiv direct. Inventory 503 și broker indisponibil nu sunt afișate ca zero; reîncărcarea recuperează starea.
- Scanarea pentru secrete și `git diff --check` trecute. Înregistrările demonstrative folosite de scenariile browser rămân în registrul local cu audit; nu s-au purjat mesaje sau tabele. Aplicațiile pornite temporar sunt oprite la final.

Monitorizarea are actualizare manuală. Publicarea confirmată nu reprezintă confirmarea procesării; alertele, retenția și extinderea la alte fluxuri rămân în backlog. Checkout-ul comercial și indexarea rămân dezactivate.

## Avertizări operaționale în mesagerie — 23 octombrie 2026

- Ambele aplicații Angular compilate în producție; administrarea recompilată după ajustarea finală a textelor.
- 3 scenarii Playwright trecute: avertizări cu răspunsuri API controlate și ceas fix, reluare reală cu răspuns pierdut, autentificare/CSRF prin gateway și direct.
- Praguri verificate exact la 5 minute și 100 mesaje, precum și imediat sub prag; totalurile sunt globale, independente de pagina registrului. Verificate răspunsuri 503, broker indisponibil/neconfigurat și recuperare prin actualizare manuală.
- Testul de avertizări acoperă și timestamp absent, invalid sau în viitor. Captură mobilă la 390 px inspectată, fără depășire orizontală.
- Prima încercare a testului UI suspenda și timer-ele de randare Angular; ceasul testului a fost schimbat la setFixedTime. Prima pornire a regresiei a precedat disponibilitatea Identity; după verificarea readiness, toate cele 3 scenarii au trecut.
- Modificări numai în frontend și documentație; backend-ul și schema bazelor nu s-au schimbat. Nu s-au trimis notificări externe și nu s-au șters înregistrări.

## Monitorizare mesagerie în fundal — 23 octombrie 2026

- `python3 scripts/dev.py test`: **271 teste backend trecute**, inclusiv PostgreSQL/RabbitMQ în containere izolate. Verificate agregatele globale fără încărcarea payload-urilor, tranzițiile Raised/Reminder/Recovered, deduplicarea, date necunoscute, independența probelor, timeout, anulare la oprire, dezactivarea worker-ului și respingerea intervalelor invalide.
- Test cu gazdele Inventory/Ordering reale și interval temporar de 5 secunde: brokerul local oprit controlat, `ordering.broker-unavailable: Raised` observat, broker repornit, apoi `Recovered` observat. Nu a fost deschis niciun browser. Gazdele temporare au fost oprite, RabbitMQ readus în stare healthy.
- Registrul existent a produs o singură alertă Raised pentru mesajele de investigat, fără repetare la fiecare probă. Memento-ul de 30 minute este verificat prin ceas controlat în testul unitar.
- Fără modificări Angular, API public sau schemă de baze; nu sunt necesare migrări. Nicio ștergere de mesaje/date și nicio notificare externă. Deduplicarea alertelor este locală procesului și se resetează la repornire.

## Retenția payload-urilor Outbox — 23 octombrie 2026

- `python3 scripts/dev.py test`: **281 teste backend trecute**. Acoperire nouă: simulare fără modificări, limitarea lotului, excluderea mesajelor nepublicate/publicate recent, păstrarea hash-ului și auditului, idempotență, loturi concurente, SKIP LOCKED și confirmare de publicare pierdută.
- Constrângerea SQL refuză marcarea unui payload nepublicat drept eliminat; indexul unic păstrat refuză recrearea aceluiași eveniment. Publisher-ul nu selectează payload-urile eliminate. Parametrii de retenție/lot invalizi sunt refuzați.
- Migrarea PublishedOutboxPayloadRetention aplicată local după backup privat Inventory; EF confirmă lipsa diferențelor de model fără migrare. Baza legacy nu a fost modificată.
- Gazda Inventory pornită cu setările implicite: eveniment DryRun=True observat. Amprenta payload-urilor și metadatelor de retenție identică înainte/după. Gazda temporară oprită.
- Eliminarea reală verificată exclusiv în baze izolate de test; instalația locală rămâne în simulare. Fără modificări frontend, API public, Inbox sau registru de erori.

## Retenția mesajelor eșuate procesate — 23 octombrie 2026

- `python3 scripts/dev.py test`: **292 teste backend trecute**. Cazuri noi: ID normalizat, dovadă Inbox cu același hash, Inbox absent/hash diferit/procesare recentă, publicare lipsă, corp schimbat, contract invalid, simulare, concurență și rânduri blocate.
- După eliminarea corpului în baza izolată: auditul rămâne identic, recapturarea nu reintroduce payload-ul, aceeași cerere de reluare păstrează operatorul, publisher-ul nu republică corpul gol și Inbox deduplică livrarea originală.
- Ambele aplicații Angular compilate în producție; administrarea recompilată după corectarea afișării datelor invalide. **4 scenarii browser trecute**: reluare reală/răspuns pierdut, securitate, avertizări și audit după retenție.
- Testul avertizărilor a evidențiat o eroare DatePipe pentru timestamp invalid. MetricDatePipe afișează acum Necunoscută; regresia verifică lipsa erorii de randare.
- Migrarea ProcessedFailedMessageRetention aplicată local după backup privat Ordering; hash-urile și identificatorii eligibili ai rândurilor existente sunt completate fără eliminarea corpurilor. EF confirmă model fără diferențe. Prima încercare --no-build avea o dependență lipsă în directorul executabil; build-ul complet a remediat artefactele, apoi migrarea a trecut.
- Worker-ul real a raportat DryRun=True, Candidates=0, Removed=0. Eliminările efective s-au executat exclusiv în baza izolată de test. Inbox și erorile nerezolvate rămân păstrate; aplicațiile temporare au fost oprite.

## Jurnal operațional comun local — 23 octombrie 2026

- `python3 scripts/dev.py test`: **292 teste .NET trecute**, urmate de testele Python ale colectorului. Suita Python finală, după adăugarea lock-ului, are **10 teste trecute**.
- Verificate allowlist-ul de categorii/ID/nivel/cod, eliminarea mesajelor/excepțiilor/scopes/câmpurilor sensibile, tipurile și limitele contoarelor, intrări invalide, scrieri concurente, rotație, permisiuni 0700/0600, un singur colector, citire filtrată/limitată și disc indisponibil.
- Serviciile reale au produs alerte când PostgreSQL/RabbitMQ erau oprite. După pornirea Docker și a dependențelor, același jurnal a înregistrat Recovered pentru broker, Outbox și registrul Ordering.
- Repornire cu dependențe healthy: evenimentele reale 7200 și 7300 au fost colectate din Inventory/Ordering cu DryRun=true, Removed=0 și câmpuri tipizate, fără corpuri sau excepții. Istoricul a rămas consultabil după oprirea supervisor-ului.
- Sintaxa Python verificată cu cache în work/pycache; încercarea inițială folosea cache-ul macOS din afara workspace-ului și a fost înlocuită cu această cale locală. git diff --check trecut.
- Nu sunt modificări în codul C#, frontend, scheme sau date comerciale. Aplicațiile de test sunt oprite; dependențele locale rămân disponibile. Jurnalul este local, limitat ca dimensiune și exclus din Git.

## Curățarea coșurilor anonime expirate — 23 octombrie 2026

- `python3 scripts/dev.py test`: **301 teste .NET și 10 teste Python trecute**. Cazuri noi: simulare fără modificarea ciornei/selecțiilor, perioadă de grație, coșuri active, pregătiri atașate sau detașate, comenzi asociate, idempotență, loturi concurente și rânduri blocate/reevaluate.
- Coșul eliminat nu poate fi modificat cu vechiul identificator; citirea nu recreează datele și un proprietar nou poate primi un coș gol. Snapshot-urile, comenzile și planurile de rezervare au rămas intacte în testul de curățare.
- Build backend fără erori sau avertismente. Migrarea CartCleanupOrderIndex aplicată după backup privat Ordering; EF nu raportează diferențe de model fără migrare.
- **9 scenarii browser trecute**: persistența coșului (4), ciorna clientului PF/PJ (3), pregătire/rezervare și securitate (2).
- Worker-ul real a emis evenimentul 7400 în jurnalul comun: DryRun=true, Candidates=0, Removed=0. Eliminarea efectivă a fost testată numai în baza izolată; coșurile locale nu au fost șterse.
- Frontend-ul nu a fost modificat. Aplicațiile pornite pentru verificări au fost oprite la final; automatizarea chatului rămâne oprită.

## API pentru conturi de client — 29 octombrie 2026

- `python3 scripts/dev.py test`: **319 teste .NET și 10 teste Python trecute**. Prima rulare după reluarea lucrului a identificat o configurație nullable incorectă, corectată; următoarea nu a avut Docker disponibil. După pornirea Docker, suita finală a trecut integral.
- Cazuri noi: validarea datelor și limitelor, CSRF direct pe Identity, înregistrare fără roluri, autentificare/deconectare, acces administrativ refuzat, duplicate fără schimbarea parolei, înregistrări concurente, dezactivare în afara Development și protecția inițializării administratorului împotriva promovării unui client existent.
- Build complet backend: **0 erori, 0 avertismente**. Nu există schimbări de schemă sau migrații noi.
- **4 scenarii HTTP Playwright trecute**, prin `customer-account-api.spec.ts` și `services.spec.ts`: înregistrare/autentificare prin gateway, refuzul accesului administrativ pentru clienți inclusiv direct pe servicii, izolarea rutelor interne și regresie CSRF pentru administrator.
- Nu s-a modificat sursa Angular; interfața contului rămâne în backlog. Scenariul HTTP a creat un cont demonstrativ fără privilegii, cu adresă `example.test`, în baza locală Identity. Aplicațiile pornite pentru verificări au fost oprite; PostgreSQL și RabbitMQ rămân disponibile.
- `git diff --check` și verificarea fișierelor modificate împotriva secretelor locale: trecute. Automatizarea chatului rămâne oprită.

## Pagina contului în Angular — 29 octombrie 2026

- `npm run build`: build-uri de producție storefront (browser + SSR) și admin finalizate cu succes, cu verificările stricte TypeScript/template păstrate.
- **12 scenarii Playwright trecute**: 4 pentru cont, 4 pentru catalog și 4 pentru persistența coșului. Contul acoperă confirmarea parolei, înregistrare reală, parolă greșită, autentificare, reîncărcare, deconectare, serviciu indisponibil, înregistrare indisponibilă, răspuns incert la login, eroare la logout și reverificare la focus.
- Prima verificare a identificat o sincronizare incorectă a testului cu schimbarea formularului Angular. Testele așteaptă acum afișarea formularului selectat înainte să completeze datele; suita finală a trecut integral.
- Aspectul mobil la 390 px a fost inspectat vizual; navigația include Contul meu și nu depășește viewport-ul. Screenshot-ul local este în `work/account-mobile.png`, exclus din Git.
- Nu sunt schimbări backend sau de schemă; suita backend nu a fost rerulată în această etapă. Conturile create de scenariile reale folosesc adrese fictive `example.test` și nu au privilegii administrative.
- Asocierea comenzilor/coșului, confirmarea e-mailului și recuperarea parolei rămân în backlog. Aplicațiile de test sunt oprite la final; automatizarea rămâne oprită.

## API pentru asocierea comenzilor cu contul — 29 octombrie 2026

- `python3 scripts/dev.py test`: **324 teste .NET și 10 teste Python trecute**. Testele noi acoperă asocierea idempotentă, păstrarea snapshot-ului și momentului inițial, excluderea din istoricul anonim, blocarea retry-urilor anonime, proprietar greșit, coș nefinalizat, conturi concurente, paginare invalidă și acces după expirarea coșului.
- Build complet backend: **0 erori, 0 avertismente**. Migrarea aditivă AccountOrderOwnership a fost aplicată local după backup privat Ordering în `work/backups/account-ownership-*`; EF nu raportează modificări de model fără migrare.
- **3 scenarii Playwright trecute**: înregistrare fără privilegii administrative, flux complet de comandă cu asociere și acces din alt browser, securitatea comenzilor prin gateway și direct pe Ordering. Helper-ul verifică și CSRF, lipsa sesiunii, ignorarea unui cont destinație trimis în corp, repetarea asocierii, lipsa cookie-ului original și refuzul preluării de către alt cont.
- Nu s-a schimbat sursa Angular; scenariul existent de comandă a fost extins. Checkout-ul comercial rămâne dezactivat, iar datele create sunt demonstrative.
- Testele au fost reluate după indisponibilitatea temporară a verificării automate de aprobare. Aplicațiile de test sunt oprite la final, iar automatizarea chatului rămâne oprită.

## Istoricul și asocierea în pagina contului — 29 octombrie 2026

- `python3 scripts/dev.py test`: **324 teste .NET și 10 teste Python trecute**. Build backend complet: **0 erori, 0 avertismente**. Nu sunt modificări de schemă.
- `npm run build`: build-uri de producție storefront (browser + SSR) și admin trecute, cu verificări stricte TypeScript/template.
- **10 scenarii Playwright trecute**: istorice cu erori independente, paginare separată, confirmare legată de contul afișat, eliminarea istoricului la logout, regresii de înregistrare/autentificare și fluxul real de comandă/asociere.
- Fluxul real verifică Renunță fără mutare, confirmarea contului destinație, răspuns pierdut după commit recuperat prin recitire, persistență după reload, acces din alt browser și izolarea față de al doilea cont. Cererile cu expectedAccountId diferit sunt respinse de API cu 409. `/api/auth/me` include identificatorul și interzice cache-ul.
- Screenshot mobil la 390 px inspectat vizual, fără depășirea viewport-ului: `work/account-order-history-mobile.png`, exclus din Git. Datele afișate și comenzile create sunt demonstrative.
- Aplicațiile pornite pentru testare sunt oprite la final. Automatizarea chatului rămâne oprită. Coșul activ rămâne anonim.

## Reverificarea coșului și invalidarea răspunsurilor vechi — 29 octombrie 2026

- `npm run build`: storefront (browser + SSR) și admin compilate pentru producție, cu verificările stricte păstrate.
- **19 scenarii Playwright distincte trecute**, în două rulări: persistența coșului, ciorna PF/PJ, pregătirea rezervărilor, comenzile demonstrative, contul clientului și 4 scenarii noi de reverificare. Acestea verifică eroarea la focus fără afișarea unui coș gol, invalidarea unei citiri în curs din alt tab, răspunsul întârziat după o salvare și recitirea după login/logout fără reload de pagină.
- Prima rulare a avut 15 reușite și 3 eșecuri de mediu: adminul oprit și limita de cereri atinsă. După pornirea adminului și expirarea ferestrei de limitare, cele două suite afectate și cele 4 teste noi au trecut integral (8/8).
- Buildul a necesitat execuție în afara sandboxului pentru runtime-ul Sass. Verificările au fost reluate după indisponibilitatea temporară a aprobării automate.
- Fără modificări backend sau de schemă; suita .NET nu a fost rerulată. Aplicațiile de test sunt oprite la final, iar automatizarea rămâne oprită. Coșul activ rămâne anonim; nu este încă sincronizat între dispozitive.
