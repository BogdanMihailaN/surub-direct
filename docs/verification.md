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
