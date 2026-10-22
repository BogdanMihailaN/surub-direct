# Starea implementării

Actualizat la 22 octombrie 2026. Proiect de dezvoltare locală, cu servicii separate; nu este un magazin pregătit pentru vânzare.

## Implementat

- Inventory: consumarea atomică și idempotentă a rezervărilor, cu jurnal per SKU și protecție la anulare/expirare concurentă. Ordering recunoaște starea terminală; confirmarea comenzii nu invocă încă operația. [Detalii](stock-consumption.md).

- Pregătire demonstrativă în Development: snapshot PF/PJ și prețuri revalidate, plan de rezervare atomic cu blocarea coșului, recuperare după răspuns pierdut și reluarea editării după eliberare. [Detalii](cart-preparation.md).

- Ordering coordonează intern rezervările prin intenții persistente, worker de retry/reconciliere și anulări anticipate. Comunică prin HTTP cu Inventory; pregătirea demonstrativă a coșului generează planuri. [Detalii](reservation-coordination.md).

- Inventory: rezervări atomice pe mai multe SKU-uri, idempotență, eliberare și expirare persistentă. Anulările sosite înaintea creării blochează mesajele întârziate. API intern cu cheie dedicată; pregătirea demonstrativă a coșului le folosește prin Ordering. [Detalii](stock-reservations.md).

- Formular PF/PJ și ciornă atașată coșului în Ordering: contact, livrare, facturare distinctă, validări server, rezumat și ștergere explicită/la golirea coșului. Aceeași versiune protejează datele și articolele. Nu plasează comenzi. [Detalii](customer-draft.md).

- Persistența coșului anonim în Ordering, import din copia locală, protecție CSRF inclusiv direct pe API, recuperare după răspuns pierdut și conflicte între taburi. [Detalii](cart-persistence.md).

- Ajustări de stoc plus/minus în Inventory, cu motiv, document, operator, cheie idempotentă și sold așteptat. Jurnal paginat al recepțiilor și ajustărilor în administrarea produsului; stocul negativ și scăderea sub rezervări sunt respinse. [Detalii](stock-adjustments.md).

- Închiderea comenzilor de aprovizionare: rest nelivrat pe SKU, motiv obligatoriu la livrare incompletă, audit și blocarea recepțiilor/corecțiilor noi. Operațiile în așteptare trebuie confirmate înainte de închidere. [Reguli și recuperare](purchase-order-closure.md).

- Corecții parțiale ale recepțiilor: jurnal legat de original, scădere idempotentă în Inventory și cantități nete în Purchasing. Intențiile neconfirmate rămân reluabile manual. [Flux și recuperare](receipt-corrections.md).

- Recepții parțiale pe liniile comenzilor trimise: jurnal persistent în Purchasing, actualizare idempotentă în Inventory, reluare manuală după eroare și protecție împotriva suprarecepției inclusiv pentru cantitățile în așteptare. Comenzile cu recepții nu mai pot fi anulate. [Flux și limite](purchase-receipts.md).

- Comenzi de aprovizionare în Purchasing: ciornă, linii cu cantități și costuri negociate, snapshot-uri, total net, marcarea manuală ca trimisă și anulare cu motiv. Versiune pentru conflicte concurente, autentificare/CSRF și verificare Catalog; fără modificarea stocului. Vezi [fluxul complet](purchase-orders.md).

- API Gateway YARP pe portul 5099, cu rute explicite spre Catalog, Inventory, Identity, Purchasing și hostul Ordering. Gateway fără bază, fără referințe la proiectele comerciale și fără expunerea endpoint-urilor interne.
- Catalog API, Inventory API, Identity API și Purchasing API rulează independent. Clean Architecture este păstrată în logica comercială; niciun serviciu nu referă proiectele altuia.
- PostgreSQL: câte o bază și un utilizator distinct pentru fiecare serviciu. Conectarea la bazele celorlalte servicii și la baza legacy este revocată.
- Catalog de produse, variante SKU, ambalaje, prețuri curente net/brut, publicare, căutare, filtre pe aceeași variantă, paginare și pagină de produs.
- Categorii și subcategorii administrabile, cu ordonare și atribute tehnice moștenite. Formulare separate pentru categorii, definiții și valorile fiecărui SKU; selectarea categoriei la creare/editare produs.
- Filtre publice dinamice pentru categoria selectată, inclusiv subcategoriile: text exact fără diferențiere majuscule/minuscule și intervale numerice, combinate pe aceeași variantă. Filtrele sunt păstrate în URL. Detaliul produsului afișează valorile tehnice suplimentare.
- Migrarea taxonomiei păstrează produsele și câmpurile existente; dimensiunile, materialul și finisajul folosesc sursele originale. Debifarea unui atribut îl ascunde fără ștergerea valorilor. Editările ierarhiei sunt serializate în PostgreSQL pentru a împiedica ciclurile concurente.
- Admin: editarea denumirii, categoriei și descrierii, cu URL stabil; arhivare fără ștergerea variantelor, ofertelor, prețurilor sau stocului; restaurare ca ciornă și republicare explicită. Arhivarea elimină produsul din lista, detaliul și categoriile publice.
- Editarea variantelor existente (denumire, dimensiuni, material și finisaj) și a ambalajelor (denumire, bucăți/ambalaj, minim și pas), cu identificatori, SKU și cod ofertă stabili. Căutarea variantei este recalculată la editare. Formularele sunt separate de cele de creare.
- Schimbarea bucăților/ambalaj readuce întregul produs ca ciornă în aceeași salvare cu ambalajul; administratorul verifică prețul și republică explicit. Prețul pe ambalaj nu se recalculează, iar Inventory păstrează soldul și recepțiile în bucăți. Editarea altor câmpuri nu schimbă starea publicării.
- Dimensiunile acceptă maximum 3 zecimale și limita reprezentabilă în baza de date; cantitățile sunt întregi pozitive, cu minimul total în bucăți reprezentabil. Endpoint-urile verifică apartenența variantei/ambalajului și blochează produsele arhivate.
- Căutare admin după denumire/slug/SKU, inclusiv ciorne fără variante, filtru de stare și paginare de 25 produse. Produsul selectat se încarcă după ID și rămâne accesibil după schimbarea poziției în listă ori a stării.
- Produsele arhivate nu pot fi editate, extinse cu variante/ambalaje sau publicate înainte de restaurare. Arhivarea nu anulează recepțiile; Inventory păstrează istoricul și soldul. Prețurile și recepțiile au în continuare propriile operații administrative în serviciile lor.
- Imagini produse: încărcare JPEG/PNG/WebP, text alternativ, asociere SKU, ordonare, alegerea imaginii principale și eliminare cu confirmare. Catalog procesează fișierele în WebP (1600 px) și miniaturi (400 px); galeria și cardurile magazinului folosesc imaginile încărcate, cu ilustrație de rezervă când lipsesc.
- Imaginile ciornelor și produselor arhivate sunt accesibile doar în administrare. Citirea publică verifică starea produsului la fiecare cerere; fără cache public persistent. Fișierele locale sunt în directorul privat `work/media/catalog`, în afara Git.
- Import CSV administrativ pentru produse noi, variante, ambalaje și prețuri: model, previzualizare, validări pe linii și confirmare înainte de salvare. Maximum 500 de rânduri/1 MB; categoriile existente sunt identificate prin slug. Salvare atomică Catalog + Pricing, protecție împotriva duplicatelor, ciorne fără modificarea stocului. [Ghid și limite](product-import.md).
- Purchasing: furnizori administrabili (date firmă/contact, stare activă), căutare/paginare și asociere furnizor–SKU cu cod extern, ambalaj de achiziție, cost net și monedă. Catalog este verificat prin HTTP; baza și credențialul Purchasing sunt separate. Dezactivarea păstrează asocierile, iar modificările sunt serializate per furnizor. [Flux și limite](purchasing.md).
- Inventory: recepții imuabile/idempotente și incrementare atomică a stocului. Verificarea existenței SKU se face prin API-ul Catalog.
- Catalog citește stocul prin API-ul Inventory. Timeout și erorile de dependență sunt raportate explicit, fără a afișa fals stoc zero.
- Identity deține conturile, cookie-ul HttpOnly și tokenurile CSRF. Catalog și Inventory validează sesiunea și modificările prin Identity, apoi aplică rolul Administrator. Accesul direct la servicii are aceleași verificări.
- Prețurile și Outbox rămân în serviciul Catalog. Worker-ul actualizează jurnalul local de publicare, cu tranzacție și SKIP LOCKED; nu publică mesaje RabbitMQ.
- Ordering recalculează coșul demonstrativ din datele publice Catalog, cu validarea cantităților și stocului agregat pe SKU. Selecția anonimă este persistentă în PostgreSQL, cu cookie HttpOnly, CSRF și versiuni concurente; cu rezervări prin pregătirea demonstrativă separată, fără creare de comenzi. [Coș și limite](cart-preview.md).
- Angular storefront SSR și admin Material în `frontend/src/apps/`, cu fișiere granulare și verificări stricte. Contractele publice și URL-urile existente sunt păstrate prin gateway.
- Transferul datelor existente a copiat schemele și a verificat numărul de rânduri pentru fiecare tabel. Baza veche și backup-urile private sunt păstrate.
- Scripturi pentru pornire independentă sau grupată, migrare legacy, verificarea izolării și a diferențelor de model EF; CI adaptat noilor directoare.

- Organizare simetrică: `backend/src`, `backend/tests`, `frontend/src`, `frontend/tests`; soluția .NET și configurația de compilare sunt în `backend/`. Scripturile și CI folosesc noile căi.

## Limite și etapele următoare

- Coșul demonstrativ este persistent pe server, fără asociere cu un cont sau sincronizare între dispozitive. Curățarea fizică a coșurilor expirate urmează. Comenzile, checkout-ul, retururile, plățile, curierul și facturarea nu sunt implementate. Rezervările, expirările și legarea la ciorna clientului există în fluxul demonstrativ; operația internă de consum este implementată, iar legarea la confirmarea comenzii și compensările comerciale urmează.
- RabbitMQ, consumatori de evenimente comerciale, Inbox, retry și dead-letter urmează odată cu Ordering. Nu există încă un flux distribuit de comandă.
- Identity folosește validarea la distanță a cookie-ului, nu OAuth/OIDC. API-urile administrative depind de disponibilitatea Identity. Sunt necesare TLS intern, rotație de credențiale, MFA și gestionarea cheilor înainte de producție.
- Catalog depinde sincron de Inventory pentru răspunsul agregat. Nu există cache/proiecție de disponibilitate, circuit breaker sau orchestrator de producție.
- Categoriile au nume unice în întregul catalog, maximum 5 niveluri și maximum 50 de atribute efective (proprii și moștenite). Atributele sunt opționale, Text sau Number; nu există încă liste de valori, atribute obligatorii, conversii de unități sau ștergere de categorii/definiții. Codul, tipul și unitatea atributului rămân stabile. Se vând doar bucăți și ambalaje convertibile; fără kituri sau cantități fracționare.
- Istoricul prețurilor, auditul complet și discounturile cantitative rămân de implementat. Slug-ul, SKU-ul și codul ofertei sunt stabile. Corectarea caracteristicilor unei variante este destinată aceluiași articol fizic; pentru alt articol se creează un SKU nou. În afara schimbării bucăților/ambalaj, modificările produselor publicate apar imediat, fără un flux separat de aprobare.
- Stocarea imaginilor este locală, printr-un port în Application; adaptorul cloud/object storage și CDN nu sunt implementate. Maximum 12 imagini/produs, 8 MB/fișier, 6000 px/latură și 24 MP; imaginile animate sunt respinse. Originalele nu se păstrează. În caz de eroare la salvarea DB sau la ștergerea fizică pot rămâne fișiere private nereferențiate; curățarea automată nu este implementată.
- DTO-urile frontend sunt întreținute manual. Generarea automată din OpenAPI rămâne în backlog.
- Datele, prețurile, TVA-ul și ilustrațiile sunt demonstrative. `noindex` rămâne activ. Baza locală poate include produse create de testele de browser în categoria „Verificare automată”.

## Verificări ale reorganizării

Rezultatele finale sunt în `verification.md`. Testele PostgreSQL rulează în baze distincte; testele de browser exercită procesele reale prin gateway, inclusiv accesul direct protejat la API-uri.

Planul de continuare și condițiile automatizării sunt în [roadmap.md](roadmap.md).
