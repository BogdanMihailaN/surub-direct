# Anularea comenzilor demonstrative

Disponibilă exclusiv în Development, din pagina administrativă Comenzi. Administratorul introduce un motiv de 3–500 caractere și confirmă explicit anularea integrală. Nu reprezintă retur comercial, rambursare, anulare de transport sau factură. Checkout-ul și indexarea rămân dezactivate.

## Intenție și audit în Ordering

`POST /api/v1/admin/sales-orders/{id}/cancel`, cu `{ "reason": "Cerere client" }`, cere sesiune Administrator și `X-CSRF-TOKEN`, inclusiv direct pe serviciu. Identity validează fiecare scriere. ID-ul operatorului provine din sesiune, nu din corpul cererii. Gateway expune numai ruta administrativă, nu operațiile interne Inventory.

O comandă Confirmed poate deveni Cancelling. Comanda, motivul, operatorul, data cererii și intenția persistentă de restituire sunt salvate în aceeași tranzacție, cu blocarea planului folosit și de worker. Confirming și Failed nu pot fi anulate prin această operație. Repetarea aceluiași motiv returnează operația existentă; un motiv diferit răspunde 409. Auditul inițial nu este înlocuit de alt operator la replay.

Worker-ul existent procesează planul Restoring și apelează portul HTTP consumer-owned. Numai răspunsul Restored, cu ID-urile și liniile originale, finalizează comanda în Cancelled. Timeout, 404/409, rezultat invalid sau indisponibilitate păstrează intenția și reîncearcă persistent, cu întârziere de 5–60 secunde. Nu se abandonează operația la expirarea termenului rezervării originale. La eșecul commitului Ordering după succes Inventory, replay-ul remote nu crește din nou stocul.

Data confirmării inițiale, clientul și prețurile rămân păstrate. Data anulării se salvează separat. Coșul nu poate fi încheiat în Cancelling; în Cancelled poate fi golit, fără pierderea comenzii din istoric. Detaliile administrative afișează auditul și starea; istoricul anonim afișează noile stări fără identificatorul operatorului sau motivul intern.

## Restituirea în Inventory

`POST /internal/reservations/{id}/restore`, cu OrderId, folosește cheia dedicată Reservations. Nu folosește sesiunea administrativă.

Operația acceptă doar Consumed sau replay Restored. Blochează cheia rezervării, verifică consumul original per SKU și mărește OnHand în ordine deterministă a SKU-urilor. Reserved nu se modifică. Soldurile, starea Restored și rândurile imuabile `inventory.Restorations` sunt salvate atomic. Cheia jurnalului este ReservationId + VariantId. Un sold care ar depăși întregul maxim refuză întreaga tranzacție; starea rămâne incertă în Ordering până la rezolvarea problemei.

Jurnalul consumului și data sa originală rămân neschimbate; jurnalul restituirii are data proprie. Reîncercările sau apelurile întârziate de consume/release/reserve nu repetă consumul ori restituirea. Jurnalul administrativ arată mișcarea pozitivă Restoration și consumul negativ original, ambele legate de comandă.

## Persistență și limite

Migrări: Inventory `StockRestorationLedger`; Ordering `DemoOrderCancellation`. Bazele au backup privat înaintea aplicării locale; baza legacy este păstrată. Nu există acces între baze sau referințe între proiectele serviciilor.

Sunt implementate numai anulări integrale ale comenzilor demonstrative confirmate, fără expediere sau plată. Retururile parțiale, rambursările și regulile comerciale după expediere rămân în backlog. Retry-ul persistent necesită monitorizare și intervenție controlată înainte de producție.

Rezultatele consumului și restituirii sunt propagate și prin [RabbitMQ cu Outbox/Inbox](reservation-messaging.md), fără eliminarea reconcilierii HTTP.
