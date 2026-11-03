# Expediere demonstrativă

Ordering deține fluxul local al expedierii, distinct de pregătirea internă și de plata simulată. Nu apelează curieri, nu generează AWB-uri reale și nu modifică stocul. Disponibil exclusiv în Development, administrativ, cu sesiune Administrator și CSRF validate inclusiv la acces direct pe serviciu.

## Reguli

Tranzițiile sunt Necreată → Pregătită pentru expediere → Expediată → Livrată. Crearea cere o comandă Confirmed, pregătirea internă Ready și un transport selectat în snapshot. Referința `DEMO-<ID comandă>` este generată pe server și rămâne stabilă; nu este un identificator de curier.

Pentru marcarea Expediată, plata simulată trebuie să fie Paid. Aceasta este politica locală pentru simularea plății în avans, nu o politică comercială de ramburs. Livrarea poate fi înregistrată și după o rambursare simulată a unei comenzi deja expediate, deoarece cele două stări descriu operații distincte.

După crearea expedierii nu se mai poate relua pregătirea internă. Înainte de expediere, anularea comenzii rămâne posibilă conform regulilor plății; nu există încă anulare separată a expedierii sau reambalare. După Expediată/Livrată, anularea cu restituire automată de stoc este interzisă chiar după rambursare. Returul integral demonstrativ are un flux separat de recepție; retururile comerciale rămân restante. Rambursarea nu restituie stoc.

## Persistență și concurență

`POST /api/v1/admin/sales-orders/{id}/shipment` primește operationId, expectedVersion, target și reason. Operatorul provine exclusiv din sesiune. Fiecare operație are motiv de 3–500 caractere și confirmare explicită în interfață. Răspunsul este 204; versiunile depășite/tranzițiile invalide și reutilizarea unei operații cu alt conținut răspund 409. În afara Development ruta refuză modificarea cu 404 pentru administratorul autentificat.

Migrarea DemoShipmentHistory adaugă trei coloane pe DemoOrders și tabela ShipmentChanges cu cheie unică pe comandă/operație și comandă/versiune. Comenzile existente pornesc Necreată. Starea și evenimentul se salvează atomic. Operațiile folosesc aceeași blocare ReservationPlans ca plata, anularea și pregătirea, astfel încât o expediere nu poate fi autorizată folosind o plată deja rambursată. Replay-ul identic nu adaugă evenimente și nu înlocuiește operatorul original.

Interfața recitește starea după succes, conflict sau răspuns pierdut, fără repetarea automată a mutației. Auditul complet rămâne administrativ. Istoricul clientului include numai starea și referința demonstrativă, cu aceleași verificări ale proprietarului ca pentru comandă, inclusiv după asocierea la cont.

## Restant

Integrare curier/sandbox, AWB/etichete reale, tracking extern, colete multiple, corectarea/anularea expedierii, livrare nereușită, ramburs și retururi. Nu există activare comercială sau expediere fizică.

Returul integral demonstrativ după livrare este acum disponibil prin fluxul separat descris în [demo-returns.md](demo-returns.md). Acesta nu deblochează anularea după expediere și nu rambursează automat plata.
