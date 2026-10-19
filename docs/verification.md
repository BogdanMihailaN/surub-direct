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
