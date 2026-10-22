# Consumarea rezervărilor

Inventory poate transforma o rezervare activă în scădere fizică a stocului prin `POST /internal/reservations/{id}/consume`, cu `{ orderId }`. Operația este destinată serviciilor de încredere și cere cheia dedicată `ServiceKeys:Reservations`. Cheia de citire a stocului și sesiunea Administrator nu o autorizează. Gateway-ul nu expune ruta; răspunsul este no-store.

## Atomicitate și reîncercare

Consumarea folosește aceeași blocare consultativă per rezervare ca rezervarea, eliberarea și expirarea. Soldurile SKU sunt blocate în ordine deterministă, apoi se verifică termenul rezervării. Astfel, așteptarea unui sold blocat nu permite consumarea unei rezervări care a expirat între timp.

Pentru fiecare SKU se scad simultan cantitatea din OnHand și Reserved. Starea devine Consumed și se salvează câte o înregistrare imuabilă în `inventory.Consumptions`, cu cheia compusă ReservationId/VariantId, OrderId, cantitate și momentul operației. Toate soldurile, starea și jurnalul se salvează în aceeași tranzacție. O eroare la oricare SKU anulează întreaga operație.

Repetarea aceleiași cereri returnează starea Consumed existentă, cu același EndedAt, fără o nouă scădere și fără duplicarea jurnalului. O rezervare consumată nu poate fi eliberată sau reactivată; apelurile release și reserve returnează Consumed. Expirarea nu modifică rezervările consumate. Consumarea nu crește disponibilitatea: ambele componente OnHand și Reserved scad cu aceeași cantitate.

Un OrderId diferit produce 409, o rezervare inexistentă 404, iar identificatorii goi 400. O rezervare Released sau Expired este returnată în starea terminală, fără consum. Dacă termenul a trecut, o rezervare încă Active este expirată și Reserved este eliberat în aceeași tranzacție; OnHand rămâne neschimbat. Apelantul trebuie să verifice starea răspunsului, nu doar HTTP 200.

La concurență consume/release câștigă prima operație care dobândește blocarea; rezultatul terminal nu poate fi schimbat de cea de-a doua. Nu există consum parțial și nici compensare automată prin restocare.

## Jurnal și compatibilitate

Jurnalul administrativ existent include tipul Consumption, cu cantitate negativă, OrderId ca document de corelare și sursa Ordering. În interfață apare „Consum rezervare”. Migrarea `StockConsumptionLedger` adaugă tabela, indexurile și referința locală către rezervare; nu modifică baza legacy sau structura bazelor altor servicii.

Ordering recunoaște Consumed ca stare terminală și nu mai reprogramează planul. Pregătirea demonstrativă consumată nu poate fi detașată prin acțiunea de anulare. Aceasta împiedică afișarea falsă a unui stoc eliberat. UI afișează explicit starea și nu oferă anulare ori reluarea editării pentru acel caz.

## Etapa următoare

Această etapă livrează operația Inventory și compatibilitatea consumatorilor existenți. Ordering solicită consumarea prin [confirmarea demonstrativă](demo-orders.md), cu număr de comandă, intenție persistentă și recuperare după răspuns pierdut. Urmează administrarea comenzilor și compensările comerciale. Un apel manual cu cheia de serviciu nu este un substitut pentru acel flux. Plata, transportul, factura, retururile și RabbitMQ nu sunt implementate. Checkout-ul comercial și indexarea rămân dezactivate.
