# Coordonarea rezervărilor în Ordering

Ordering păstrează intențiile în ordering.ReservationPlans, în baza proprie. Inventory continuă să dețină soldurile și rezervările efective. Comunicarea este HTTP prin portul IReservationInventory și DTO-uri de transport locale Ordering; nu există referințe la proiectele sau baza Inventory.

Mecanismul intern este folosit de [pregătirea demonstrativă a coșului](cart-preparation.md), care salvează snapshot-ul și planul atomic, asociază proprietarul și convertește ambalajele în bucăți. Checkout-ul rămâne dezactivat.

## Protocol

Rutele sunt doar interne, neexpuse prin gateway:

- POST /internal/reservation-plans: id, orderId și lines cu variantId/quantity în bucăți, 1–50 SKU-uri distincte.
- GET /internal/reservation-plans/{id}: starea coordonării, termenul local, termenul comunicat de Inventory, numărul erorilor consecutive și codul ultimei erori.
- POST /internal/reservation-plans/{id}/cancel: orderId; intenție persistentă de anulare.

Cheia COORDINATOR_SERVICE_KEY autorizează aceste rute și este distinctă de RESERVATIONS_SERVICE_KEY, folosită pentru apelurile Ordering → Inventory. Ambele sunt transmise numai proceselor care le folosesc. scripts/dev.py db completează secretele lipsă; init aplică migrarea ReservationCoordination. Sesiunea Administrator sau cheia de citire Inventory nu înlocuiesc cheia coordonatorului.

ID-ul planului este și cheia idempotentă a rezervării. Payload-ul este stabil; o reutilizare cu altă comandă sau alte cantități produce 409. Retry-ul creării nu modifică termenul original. Anularea unui ID încă necunoscut creează o intenție fără linii care împiedică o creare întârziată să reactiveze acel ID.

## Procesare și recuperare

Planurile sunt salvate înainte de apelurile remote. Worker-ul procesează până la 10 planuri per ciclu, la interval de 5 secunde. Selectarea folosește FOR UPDATE SKIP LOCKED, câte un rând per tranzacție. Blocarea locală se păstrează în timpul apelului HTTP, limitat la 5 secunde; alte planuri pot fi procesate de instanțe separate.

Stările sunt Pending, Active, Cancelling, Released și Expired. Ultimele două sunt terminale. Inventory confirmă rezultatul; un timeout sau un răspuns nevalid nu este interpretat drept succes. Worker-ul păstrează starea, înregistrează InventoryUnavailable și reîncearcă după 5–60 secunde. Codul erorii nu conține payload-uri sau date personale.

Dacă apelul remote reușește, dar răspunsul ori commitul local se pierde, reluarea folosește aceeași cheie. Inventory returnează rezervarea existentă fără a mări Reserved ori a prelungi expirarea. Anularea se repetă până la confirmare. Un refuz 409 la creare trece în Cancelling, urmat de eliberare/anulare idempotentă pentru a închide sigur încercarea.

Planul are un termen local de 15 minute. După acest termen nu mai trimite creări, ci anulări. Rezervarea Inventory are propria durată de 15 minute de la prima acceptare; dacă prima trimitere este întârziată și Ordering devine indisponibil, rezervarea poate rămâne activă după termenul local până la expirarea ei în Inventory. Termenul local este o limită pentru coordonare, nu o garanție de eliberare exactă la secundă.

Planurile Active sunt reconciliate la 15 secunde prin aceeași cerere idempotentă. Worker-ul Inventory poate expira rezervarea independent; Ordering preia starea terminală la următoarea confirmare. Rândurile persistente permit reluarea după restart.

## Limite

Există snapshot și o singură pregătire atașată per coș demonstrativ. Nu sunt implementate încă entitatea comercială de comandă, consumarea rezervării, plata sau confirmarea comenzii. Rutele interne sunt destinate serviciilor de încredere, nu browserului. Nu există RabbitMQ, Inbox ori dead-letter în această etapă; retry-ul HTTP este persistent și continuă cu întârziere plafonată până la rezolvare. Sunt necesare monitorizare operațională, politici de abandon și teste de încărcare înainte de producție.
