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

Contractul este validat cu același parser folosit de Inbox. Mesajele invalide rămân vizibile, cu reluarea blocată. Corpul original este păstrat privat, inclusiv pentru mesaje invalide. Corpurile mesajelor valide reluate și confirmate în Inbox pot fi eliminate după perioada configurată; [retenția are simulare implicită](failed-message-retention.md). Peste limita contractului de 64 KiB, reluarea este blocată; mesajul rămâne disponibil pentru investigare tehnică. Administratorul nu poate modifica un payload sau forța un contract invalid din interfață.

Scăderea numărului din coada `.dead` indică arhivarea în registru, nu rezolvarea erorii. Înregistrările nu sunt șterse automat. Numărul de încercări este antetul raportat de transport, nu un contor exact al tuturor livrărilor fizice.

## Reluare și audit

Administratorul selectează un mesaj valid, introduce motivul și bifează confirmarea. Se salvează o singură cerere de reluare per înregistrare: RequestId, operator, motiv și dată. Repetarea aceleiași cereri returnează rezultatul existent, păstrând operatorul inițial. Alt motiv, alt RequestId pentru aceeași înregistrare sau reutilizarea RequestId pentru alt mesaj produc 409.

Worker-ul publică cererile salvate cu ID-ul, tipul și corpul original, attempt resetat la zero și un antet `replay-id` egal cu RequestId. Publicarea trebuie confirmată de broker înainte de salvarea PublishedAt. Dacă răspunsul sau commitul se pierde, aceeași cerere este republicabilă; Inbox-ul existent împiedică repetarea efectelor comerciale. O indisponibilitate a brokerului lasă cererea persistentă în așteptare, fără a necesita browserul deschis.

„Republicat” înseamnă numai confirmarea publicării, nu succesul procesării. Dacă mesajul eșuează din nou, retry-ul transportă același replay-id, iar arhivarea produce o nouă înregistrare legată de cererea anterioară. Aceasta poate fi investigată și reluată separat. Nu există bulk replay, editare payload sau purjare din administrare. Detaliile afișează PayloadRemovedAt când retenția a eliminat corpul, păstrând auditul.

## Limite și migrare

Migrarea Ordering: `FailedMessageRecovery`, aplicată local după backup privat. Inventory nu are migrare nouă. Nu se schimbă autorizarea comenzilor, regulile stocului sau checkout-ul demonstrativ.

Starea brokerului este o observație punctuală. Avertizările din panou se evaluează la fiecare încărcare manuală; separat, worker-ele verifică periodic starea și emit tranziții în jurnalele serviciilor. Verificările UI pot reprezenta momente diferite între servicii. Retenția payload-urilor Outbox publicate este disponibilă cu simulare implicită; [detalii](outbox-retention.md). Nu există încă notificări externe, retenție pentru Inbox/erori nerezolvate, confirmare automată în UI a succesului unei reluări sau un panou pentru toate fluxurile viitoare. Cauza tehnică detaliată se investighează în jurnale; registrul păstrează contractul, încercările și auditul, nu stack trace-uri. Datele tehnice și eventualele corpuri invalide necesită control de acces și retenție înainte de producție.

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

Panoul arată momentul ultimei evaluări și prioritizează avertizările critice. În timpul actualizării nu afișează concluziile vechi. Absența avertizărilor nu certifică procesarea fiecărui mesaj. Închiderea paginii oprește observarea din browser; worker-ele de monitorizare continuă separat. Nu există polling în UI, email, notificări externe sau istoric persistent de incidente. Arhivarea și reluarea continuă prin worker-ele existente, independent de browser.

## Monitorizare în fundal și jurnale

Inventory și Ordering înregistrează fiecare câte un `MessagingMonitorWorker` atunci când `Messaging:Enabled=true`. Inventory citește propriul Outbox. Ordering verifică AMQP pasiv și citește agregatele propriului registru prin `IMessageFailureMonitor`, fără corpuri de mesaje. Probele sunt independente: eșecul brokerului nu împiedică verificarea registrului. Nu sunt necesare sesiune Administrator, browser deschis sau acces la baza altui serviciu.

Configurația fiecărui host, sub `Messaging:Monitoring`:

| Opțiune | Implicit | Valori acceptate |
| --- | --- | --- |
| Enabled | true | true/false |
| IntervalSeconds | 60 | 5–3600 |
| ProbeTimeoutSeconds | 10 | 1–60 |
| ReminderSeconds | 1800 | 60–86400 |

De exemplu, `Messaging__Monitoring__IntervalSeconds=60` configurează intervalul prin mediu. Prima verificare are loc la pornire; intervalul este pauza după terminarea ciclului, fără suprapunere. Timeout-ul folosește anularea cooperativă a clienților. Configurațiile numerice invalide sunt refuzate la pornire. Oprirea gazdei nu este tratată drept incident.

Evenimente structurate: **7100 / Raised**, **7101 / Reminder**, **7102 / Recovered**. Apariția și memento-ul au nivel Warning; recuperarea are nivel Information. Proprietăți: `AlertCode`, `AlertState`, `ObservedAt` (UTC); categoria logger-ului identifică serviciul. Nivelul Information trebuie păstrat pentru categoria worker-ului dacă se dorește colectarea recuperărilor. Nu se includ payload-uri, motive de reluare, operatori, credențiale sau excepții brute.

| Cod | Condiție |
| --- | --- |
| inventory.outbox-unavailable | Citire eșuată sau timeout |
| inventory.outbox-age-unknown | Mesaje restante, timestamp lipsă sau în viitor |
| inventory.outbox-delayed | Mesaj restant de cel puțin 5 minute |
| ordering.broker-probe-unavailable | Proba AMQP nu se poate finaliza |
| ordering.broker-unavailable | Monitorul raportează broker indisponibil |
| ordering.ready-backlog | Cel puțin 100 mesaje ready |
| ordering.dead-letter | Mesaje prezente în coada de erori |
| ordering.registry-unavailable | Agregatele registrului nu pot fi citite |
| ordering.failures-awaiting-review | Mesaje fără cerere de reluare |
| ordering.replays-awaiting-publication | Cereri salvate, încă nepublicate |

Lipsa datelor nu rezolvă alerte anterioare: de exemplu, un broker indisponibil păstrează alerta de backlog până când o citire validă confirmă scăderea. Datele cu vechime necunoscută păstrează o alertă Outbox întârziat deja activă. O alertă de indisponibilitate a probei se recuperează când proba răspunde; aceasta nu implică rezolvarea alertelor comerciale sau procesarea mesajelor.

Starea deduplicării se păstrează în memorie, separat per proces. Repornirea poate emite din nou Raised pentru probleme încă prezente; replicile emit independent. Nu se implementează un registru persistent de incidente, escaladări, email sau webhooks. Supervisor-ul local poate colecta evenimentele într-un [jurnal comun](local-operational-logs.md). Un colector extern de producție nu este încă configurat. Dacă procesul însuși este oprit, aceste worker-e nu îl pot supraveghea; producția necesită monitorizare externă. Monitorizarea nu șterge date. Worker-ul separat de retenție Outbox are simulare implicită; vezi [politica tehnică](outbox-retention.md).
