# Starea implementării

Actualizat la 18 octombrie 2026. Proiect de dezvoltare locală, cu servicii separate; nu este un magazin pregătit pentru vânzare.

## Implementat

- API Gateway YARP pe portul 5099, cu rute explicite spre Catalog, Inventory, Identity și hostul Ordering. Gateway fără bază, fără referințe la proiectele comerciale și fără expunerea endpoint-urilor interne.
- Catalog API, Inventory API și Identity API rulează independent. Clean Architecture este păstrată în logica comercială; niciun serviciu nu referă proiectele altuia.
- PostgreSQL: câte o bază și un utilizator distinct pentru fiecare serviciu. Conectarea la bazele celorlalte servicii și la baza legacy este revocată.
- Catalog de produse, variante SKU, ambalaje, prețuri curente net/brut, publicare, căutare, filtre pe aceeași variantă, paginare și pagină de produs.
- Admin: editarea denumirii, categoriei și descrierii, cu URL stabil; arhivare fără ștergerea variantelor, ofertelor, prețurilor sau stocului; restaurare ca ciornă și republicare explicită. Arhivarea elimină produsul din lista, detaliul și categoriile publice.
- Editarea variantelor existente (denumire, dimensiuni, material și finisaj) și a ambalajelor (denumire, bucăți/ambalaj, minim și pas), cu identificatori, SKU și cod ofertă stabili. Căutarea variantei este recalculată la editare. Formularele sunt separate de cele de creare.
- Schimbarea bucăților/ambalaj readuce întregul produs ca ciornă în aceeași salvare cu ambalajul; administratorul verifică prețul și republică explicit. Prețul pe ambalaj nu se recalculează, iar Inventory păstrează soldul și recepțiile în bucăți. Editarea altor câmpuri nu schimbă starea publicării.
- Dimensiunile acceptă maximum 3 zecimale și limita reprezentabilă în baza de date; cantitățile sunt întregi pozitive, cu minimul total în bucăți reprezentabil. Endpoint-urile verifică apartenența variantei/ambalajului și blochează produsele arhivate.
- Căutare admin după denumire/slug/SKU, inclusiv ciorne fără variante, filtru de stare și paginare de 25 produse. Produsul selectat se încarcă după ID și rămâne accesibil după schimbarea poziției în listă ori a stării.
- Produsele arhivate nu pot fi editate, extinse cu variante/ambalaje sau publicate înainte de restaurare. Arhivarea nu anulează recepțiile; Inventory păstrează istoricul și soldul. Prețurile și recepțiile au în continuare propriile operații administrative în serviciile lor.
- Inventory: recepții imuabile/idempotente și incrementare atomică a stocului. Verificarea existenței SKU se face prin API-ul Catalog.
- Catalog citește stocul prin API-ul Inventory. Timeout și erorile de dependență sunt raportate explicit, fără a afișa fals stoc zero.
- Identity deține conturile, cookie-ul HttpOnly și tokenurile CSRF. Catalog și Inventory validează sesiunea și modificările prin Identity, apoi aplică rolul Administrator. Accesul direct la servicii are aceleași verificări.
- Prețurile și Outbox rămân în serviciul Catalog. Worker-ul actualizează jurnalul local de publicare, cu tranzacție și SKIP LOCKED; nu publică mesaje RabbitMQ.
- Ordering este doar un host separat cu verificare DB și endpoint de capabilități care declară checkout dezactivat. Nu conține fluxuri comerciale simulate.
- Angular storefront SSR și admin Material în `frontend/src/apps/`, cu fișiere granulare și verificări stricte. Contractele publice și URL-urile existente sunt păstrate prin gateway.
- Transferul datelor existente a copiat schemele și a verificat numărul de rânduri pentru fiecare tabel. Baza veche și backup-urile private sunt păstrate.
- Scripturi pentru pornire independentă sau grupată, migrare legacy, verificarea izolării și a diferențelor de model EF; CI adaptat noilor directoare.

- Organizare simetrică: `backend/src`, `backend/tests`, `frontend/src`, `frontend/tests`; soluția .NET și configurația de compilare sunt în `backend/`. Scripturile și CI folosesc noile căi.

## Limite și etapele următoare

- Coșul, comenzile, checkout-ul, rezervările, expirările, retururile, plățile, curierul, facturarea, importurile și furnizorii nu sunt implementați. Coloana Reserved nu implică existența operațiilor de rezervare.
- RabbitMQ, consumatori de evenimente comerciale, Inbox, retry și dead-letter urmează odată cu Ordering. Nu există încă un flux distribuit de comandă.
- Identity folosește validarea la distanță a cookie-ului, nu OAuth/OIDC. API-urile administrative depind de disponibilitatea Identity. Sunt necesare TLS intern, rotație de credențiale, MFA și gestionarea cheilor înainte de producție.
- Catalog depinde sincron de Inventory pentru răspunsul agregat. Nu există cache/proiecție de disponibilitate, circuit breaker sau orchestrator de producție.
- Categoriile sunt etichete, nu ierarhii cu atribute dinamice. Se vând doar bucăți și ambalaje convertibile; fără kituri sau cantități fracționare.
- Administrarea imaginilor, ierarhiile de categorii, istoricul prețurilor, auditul complet și discounturile cantitative rămân de implementat. Slug-ul, SKU-ul și codul ofertei sunt stabile. Corectarea caracteristicilor unei variante este destinată aceluiași articol fizic; pentru alt articol se creează un SKU nou. În afara schimbării bucăților/ambalaj, modificările produselor publicate apar imediat, fără un flux separat de aprobare.
- DTO-urile frontend sunt întreținute manual. Generarea automată din OpenAPI rămâne în backlog.
- Datele, prețurile, TVA-ul și ilustrațiile sunt demonstrative. `noindex` rămâne activ. Baza locală poate include produse create de testele de browser în categoria „Verificare automată”.

## Verificări ale reorganizării

Rezultatele finale sunt în `verification.md`. Testele PostgreSQL rulează în baze distincte; testele de browser exercită procesele reale prin gateway, inclusiv accesul direct protejat la API-uri.
