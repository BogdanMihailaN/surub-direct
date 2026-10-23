# Monitorizarea și reluarea mesajelor

Pagina administrativă `/mesagerie` acoperă fluxul Inventory → Ordering. Afișează starea brokerului și mesajele gata de preluare, Outbox-ul Inventory (număr restant, cel mai vechi mesaj, ultima publicare), Inbox-ul Ordering și registrul erorilor. Actualizarea este manuală. Citirile se fac separat, fără acces la baza altui serviciu; o dependență indisponibilă nu este afișată ca zero mesaje.

## API și acces

- Ordering: `GET /api/v1/admin/messaging?page=1`, paginare de 25 înregistrări, cele mai recente primele.
- Ordering: `GET /api/v1/admin/messaging/broker`, verificare AMQP pasivă, fără declararea topologiei sau consum de mesaje. Include CheckedAt. Numerele includ doar mesajele ready, nu pe cele neconfirmate în curs. Neconfigurat/indisponibil folosesc numere null.
- Inventory: `GET /api/v1/admin/messaging/inventory-outbox`, citire consistentă din propria bază.
- Ordering: `POST /api/v1/admin/messaging/{id}/replay`, cu RequestId și Reason (3–500 caractere).

Toate rutele cer Administrator, inclusiv apelurile directe. Scrierea cere CSRF validat de Identity. Operatorul este preluat din sesiune. Răspunsurile sunt no-store. Gateway expune doar rutele explicite; nu expune brokerul, credențialele, corpul mesajelor sau rute interne. POST este refuzat cu 503 dacă mesageria nu este configurată.

## Arhivarea din coada de erori

`FailedMessageWorker` citește `.dead` cu ACK manual. Mai întâi salvează mesajul în `ordering.FailedReservationMessages`, apoi confirmă livrarea. Dacă salvarea eșuează sau procesul se oprește, brokerul poate relivra. Fingerprint-ul include ID, tip, hash-ul corpului și generația de reluare; lock-ul tranzacțional și indexul unic deduplică relivrările fără a șterge auditul.

Contractul este validat cu același parser folosit de Inbox. Mesajele invalide rămân vizibile, cu reluarea blocată. Corpul original este păstrat privat, inclusiv pentru mesaje invalide. Peste limita contractului de 64 KiB, reluarea este blocată; mesajul rămâne disponibil pentru investigare tehnică. Administratorul nu poate modifica un payload sau forța un contract invalid din interfață.

Scăderea numărului din coada `.dead` indică arhivarea în registru, nu rezolvarea erorii. Înregistrările nu sunt șterse automat. Numărul de încercări este antetul raportat de transport, nu un contor exact al tuturor livrărilor fizice.

## Reluare și audit

Administratorul selectează un mesaj valid, introduce motivul și bifează confirmarea. Se salvează o singură cerere de reluare per înregistrare: RequestId, operator, motiv și dată. Repetarea aceleiași cereri returnează rezultatul existent, păstrând operatorul inițial. Alt motiv, alt RequestId pentru aceeași înregistrare sau reutilizarea RequestId pentru alt mesaj produc 409.

Worker-ul publică cererile salvate cu ID-ul, tipul și corpul original, attempt resetat la zero și un antet `replay-id` egal cu RequestId. Publicarea trebuie confirmată de broker înainte de salvarea PublishedAt. Dacă răspunsul sau commitul se pierde, aceeași cerere este republicabilă; Inbox-ul existent împiedică repetarea efectelor comerciale. O indisponibilitate a brokerului lasă cererea persistentă în așteptare, fără a necesita browserul deschis.

„Republicat” înseamnă numai confirmarea publicării, nu succesul procesării. Dacă mesajul eșuează din nou, retry-ul transportă același replay-id, iar arhivarea produce o nouă înregistrare legată de cererea anterioară. Aceasta poate fi investigată și reluată separat. Nu există bulk replay, editare payload sau purjare din administrare.

## Limite și migrare

Migrarea Ordering: `FailedMessageRecovery`, aplicată local după backup privat. Inventory nu are migrare nouă. Nu se schimbă autorizarea comenzilor, regulile stocului sau checkout-ul demonstrativ.

Starea brokerului este o observație punctuală. Avertizările din panou se evaluează la fiecare încărcare manuală, fără supraveghere în fundal. Verificările UI pot reprezenta momente diferite între servicii. Nu există încă notificări externe, politici de retenție/ștergere, confirmare automată în UI a succesului unei reluări sau un panou pentru toate fluxurile viitoare. Cauza tehnică detaliată se investighează în jurnale; registrul păstrează contractul, încercările și auditul, nu stack trace-uri. Datele tehnice și eventualele corpuri invalide necesită control de acces și retenție înainte de producție.

## Avertizări operaționale în administrare

Evaluarea este separată în funcția pură `state/evaluate-messaging-alerts.ts`, cu model și componentă de prezentare proprii. Nu implică acces suplimentar la baze sau schimbări în gateway. Se folosesc contoarele globale ale API-ului, inclusiv atunci când lista afișată este paginată.

| Condiție la verificare | Nivel | Acțiune sugerată |
| --- | --- | --- |
| Broker configurat, dar indisponibil | Critic | Verifică RabbitMQ și conexiunea din Ordering |
| Citirea brokerului, registrului sau Outbox eșuează | Critic | Verifică serviciul/accesul; starea este necunoscută |
| Mesaj Outbox restant de cel puțin 5 minute | Critic | Verifică publisher-ul Inventory și conexiunea |
| Mesageria neconfigurată | Atenție | Verifică setările mediului |
| Cel puțin 100 mesaje ready | Atenție | Verifică procesarea în Ordering |
| Mesaje în dead-letter | Atenție | Verifică arhivarea în registru dacă numărul persistă |
| Mesaje fără cerere de reluare | Atenție | Investighează cauza, inclusiv contractele invalide |
| Cereri de reluare nepublicate | Atenție | Verifică worker-ul și brokerul, evită cereri duplicate |
| Outbox restant cu dată absentă, invalidă sau în viitor | Atenție | Verifică timestamp-urile și sincronizarea ceasurilor |

Pragurile inițiale sunt definite în cod, nu sunt SLA-uri comerciale. Vechimea se calculează față de ceasul browserului la terminarea citirilor; ceasurile incorecte pot influența rezultatul. Nu deducem vechimea unei reluări din pagina curentă, deoarece endpoint-ul nu furnizează cea mai veche cerere globală.

Panoul arată momentul ultimei evaluări și prioritizează avertizările critice. În timpul actualizării nu afișează concluziile vechi. Absența avertizărilor nu certifică procesarea fiecărui mesaj. Închiderea paginii oprește observarea; nu există polling, email, notificări externe, istoric de incidente sau confirmare de rezolvare. Arhivarea și reluarea continuă prin worker-ele existente, independent de browser.
