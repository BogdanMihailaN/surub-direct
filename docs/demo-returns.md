# Retur integral demonstrativ

Flux administrativ local pentru comenzile Confirmed cu expedierea Delivered. Nu activează retururi comerciale, nu emite documente fiscale și nu încasează/rambursează bani reali. Clientul autentificat poate solicita returul integral din istoricul propriului cont; decizia și recepția rămân administrative.

## Etape

Nesolicitat → Solicitat → Aprobat sau Respins. Aprobarea și respingerea nu modifică stocul. Respingerea este terminală în această versiune; o cerere nouă sau redeschiderea nu sunt implementate.

Din Aprobat, administratorul poate confirma primirea integrală numai dacă toate produsele au ajuns în cantitățile originale și sunt revandabile. Interfața cere motiv, confirmarea operației și confirmarea explicită a primirii/stării produselor. API-ul cere `restockConfirmed: true`. Produsele lipsă, deteriorate sau retururile parțiale nu sunt suportate și nu trebuie înregistrate prin această operație.

Confirmarea salvează atomic auditul, starea Restoring și intenția de restituire a planului original. Stocul nu este actualizat direct de Ordering. Coordonatorul existent solicită restaurarea consumului original în Inventory, cu aceeași cheie de rezervare. Inventory păstrează jurnalul și deduplică restituirea integrală. Rezultatul se reconciliază prin HTTP sau Inbox; până la confirmare se afișează „Restituire stoc în curs”, nu un succes presupus. O dependență indisponibilă păstrează intenția și mecanismul existent de retry.

După rezultatul autorizat Restored, Ordering afișează Stoc restituit și data confirmării. Reconcilierea repetată nu schimbă data. Comanda rămâne Confirmed și expedierea Delivered; returul are stare separată. Planul este terminal și nu mai inițiază alte restituiri.

Rambursarea este independentă: aprobarea sau primirea returului nu modifică plata simulată. Restituirea stocului nu cere ca rambursarea să fi avut deja loc. Anularea comenzii rămâne interzisă după expediere, inclusiv după retur.

## API și persistență

`POST /api/v1/admin/sales-orders/{id}/return` primește operationId, expectedVersion, target (Requested/Approved/Rejected/Restoring), reason și restockConfirmed. Sesiunea Administrator și CSRF sunt verificate inclusiv direct pe Ordering; modificările sunt disponibile numai în Development. Operatorul provine din sesiune, iar răspunsurile sunt no-store.

Migrarea DemoIntegralReturns adaugă ReturnStatus, ReturnVersion, ReturnRestoredAt pe DemoOrders și tabela ReturnChanges. Cheile unice deduplică operația și versiunea per comandă. Același lock ReservationPlans serializează plata, expedierea, anularea, returul și reconcilierea. Replay-ul exact confirmă rezultatul salvat, inclusiv după restituirea stocului, fără evenimente sau recepții suplimentare; alt conținut/versiune și tranzițiile invalide produc 409.

Auditul conține solicitarea, decizia și confirmarea recepției cu motiv/operator/dată. Confirmarea finală Inventory este reflectată separat în stare și ReturnRestoredAt, fără inventarea unui operator administrativ. Versiunea urmărește operațiile administratorului, nu livrările repetate ale rezultatului Inventory. Jurnalul Inventory folosește descrierea neutră „Restituire integrală coordonată de Ordering”; motivul de afaceri se consultă în auditul comenzii după referința OrderId.

Clientul vede numai starea și data restituirii în istoricul propriu, inclusiv după asocierea la cont. Auditul administrativ nu este expus. La răspuns pierdut, interfața recitește detaliul fără retransmitere automată; în starea Restoring, actualizarea detaliilor verifică progresul.

## Restant

Retururi parțiale, mai multe cereri per comandă, redeschidere/contestație, produse nerevandabile, control cantitativ pe linii, carantină, retur de la curier înainte de livrare, documente comerciale/fiscale și integrarea reală a rambursării. Această etapă nu reprezintă politica legală/comercială de retur a magazinului.

## Solicitare din contul clientului

Din Contul meu, numai comenzile Confirmed, Delivered și cu retur NotRequested afișează butonul de solicitare. Formularul confirmă comanda și contul, cere un motiv de 3–500 caractere și descrie limitele returului integral. Comenzile anonime trebuie mai întâi asociate explicit contului prin fluxul existent.

`POST /api/v1/ordering/account/orders/{id}/return` primește operationId, expectedAccountId și reason. Sesiunea autentificată, CSRF Identity și limitarea de trafic sunt verificate inclusiv direct pe Ordering. ExpectedAccountId este obligatoriu și trebuie să coincidă cu sesiunea; schimbarea contului răspunde 409. Repository-ul verifică proprietarul comenzii înainte de consultarea operației anterioare, inclusiv pentru replay; comenzile neasociate sau ale altui cont răspund 404.

Portul de client permite exclusiv tranziția inițială Requested, fără parametri pentru aprobare, recepție, cantități sau rambursare. Actorul auditului este generat pe server ca customer:<accountId>. Replay-ul identic confirmă operația deja salvată și după aprobarea administrativă, fără a redeschide returul sau a modifica stocul/plata. Alt conținut sau o cerere nouă după prima tranziție răspunde 409.

Interfața recitește listele după succes/conflict/răspuns pierdut, fără retransmitere automată. Datele și formularul sunt recreate la schimbarea contului afișat; contul confirmat este trimis explicit pentru a detecta și schimbarea sesiunii între afișare și POST. Eșecul citirii retrage acțiunile vechi. Cererea este demonstrativă și nu definește politica comercială ori legală de retur.

## Lista administrativă de retururi

Pagina Comenzi permite filtrarea după toate stările returului: Nesolicitat, Solicitat, Aprobat, Respins, Restituire stoc în curs și Stoc restituit. Filtrul se combină cu starea și numărul comenzii, se aplică pe server înainte de count/paginare și păstrează ordinea descrescătoare a numerelor comenzilor. Un filtru nou revine la pagina 1; paginarea și reîncărcarea păstrează filtrul ales.

Endpoint-ul administrativ de listare acceptă parametrul opțional `returnStatus`. Absența sau valoarea goală păstrează comportamentul anterior; valorile necunoscute răspund 400. Fiecare rând include `returnStatus`, fără a încărca auditul returului în listă. După o operație administrativă de retur se recitesc detaliul și lista: aprobarea elimină comanda din filtrul Solicitat, dar păstrează detaliul selectat pentru verificare. Pentru schimbările făcute de alte sesiuni sau de reconciliere se folosește Actualizează lista.
