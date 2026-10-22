# Rezervări atomice în Inventory

Inventory deține tabela inventory.Reservations și cantitățile Reserved din inventory.Balances. O rezervare conține un ID idempotent, un OrderId de corelare, durata în secunde și 1–50 SKU-uri distincte, fiecare cu un număr pozitiv de bucăți. Ordering transformă ambalajele în bucăți și cumulează SKU-urile în pregătirea demonstrativă a coșului; browserul nu poate apela aceste operații prin gateway.

## Protocol intern

Toate rutele cer X-Service-Key cu cheia dedicată ServiceKeys:Reservations. Cheia existentă pentru citirea stocului nu acordă acces. Nu sunt operații administrative: sesiunea Administrator nu le autorizează, iar gateway-ul nu expune /internal.

- POST /internal/reservations: { id, orderId, durationSeconds, lines: [{ variantId, quantity }] }.
- GET /internal/reservations/{id}: starea curentă, liniile și momentele de creare/expirare/încheiere; 404 pentru ID necunoscut.
- POST /internal/reservations/{id}/release: { orderId }; eliberare idempotentă, inclusiv înaintea creării.

Răspunsurile au Cache-Control: no-store. ID-urile goale, liniile duplicate, cantitățile nepozitive sau duratele în afara intervalului 60–1800 secunde sunt respinse. Nu există validare Catalog la acest nivel: Inventory verifică disponibilitatea soldurilor proprii. O eroare de bază de date nu este raportată drept rezervare reușită sau sold zero.

Cheia locală RESERVATIONS_SERVICE_KEY este generată de scripts/dev.py db, păstrată în .env și transmisă Inventory și coordonatorului Ordering. Pentru o instalare locală existentă, rulează comanda db pentru completarea secretelor, apoi init pentru migrare. Producția trebuie să configureze cheia separat, rețeaua internă și rotația ei.

## Atomicitate și idempotență

Toate liniile se rezervă într-o singură tranzacție. Soldurile sunt actualizate condiționat numai dacă OnHand − Reserved acoperă cererea, într-o ordine deterministă a SKU-urilor. Dacă oricare linie eșuează, întreaga tranzacție revine, inclusiv liniile deja actualizate. OnHand nu se modifică.

Operațiile cu același ID sunt serializate cu o blocare consultativă PostgreSQL în tranzacție, inclusiv când rândul nu există încă. Retry-ul cu aceleași date returnează rezervarea persistentă; nu mărește cantitatea și nu prelungește expirarea. Ordinea liniilor nu contează. Schimbarea comenzii, duratei ori cantităților pentru un ID deja rezervat produce 409.

Anularea înaintea creării salvează un rând terminal Released fără linii. O rezervare întârziată cu același ID și aceeași comandă rămâne Released și nu blochează stoc. ID-ul nu poate fi reciclat. După o cerere respinsă pentru stoc insuficient nu există rând persistent; încercarea poate fi reluată după modificarea disponibilității.

Stările sunt Active, Released și Expired. Eliberarea scade Reserved exact o dată, în aceeași tranzacție cu starea terminală. O anulare sosită după termen produce Expired. Ajustările și corecțiile existente continuă să refuze coborârea stocului fizic sub cantitatea rezervată.

## Expirare

Worker-ul găzduit de API rulează la pornire și apoi la fiecare 15 secunde, în loturi de maximum 100. Recitește fiecare rezervare sub aceeași blocare tranzacțională; mai multe instanțe sau anulări concurente nu dublează eliberarea. Eșecurile sunt jurnalizate și reluate la următorul ciclu. Citirea unei rezervări ori retry-ul creării finalizează și ele expirarea dacă termenul a trecut.

Nu există timere în memorie pentru fiecare rezervare; rândurile persistente permit recuperarea după restart. Citirea obișnuită a stocului nu declanșează expirări: un sold poate rămâne conservator până la procesarea lotului. Sub încărcare sau în timpul unei întreruperi, întârzierea poate depăși 15 secunde.

## Limite și continuare

Pregătirea demonstrativă a coșului PF/PJ pornește rezervări prin coordonatorul Ordering. Urmează consumarea rezervării la confirmarea comenzii și compensările comerciale, apoi integrarea RabbitMQ/Outbox/Inbox. Un OrderId poate avea mai multe încercări cu ID-uri distincte; coordonatorul trebuie să gestioneze încercările și să evite rezervările redundante.

În această etapă nu există prelungire, rezervare parțială sau ecran administrativ dedicat. Consumarea internă este descrisă în [protocolul de consum](stock-consumption.md). Jurnalul existent al mișcărilor fizice include recepții, ajustări, corecții și consumuri; tabela Reservations păstrează istoricul rezervărilor. Nu s-a făcut un test de încărcare sau deployment distribuit. Checkout-ul rămâne dezactivat.
