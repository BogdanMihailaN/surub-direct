# Pregătirea internă a comenzilor demonstrative

Disponibilă exclusiv în Development, în detaliul comenzii din administrare. Este o etapă internă separată de confirmarea/consumul stocului: nu reprezintă plată, expediere, facturare sau activarea checkout-ului comercial.

## Reguli

Numai o comandă `Confirmed` permite schimbări: `Pending` → `Processing` → `Ready`, cu revenire `Ready` → `Processing` pentru reverificare. Fiecare schimbare cere motiv de 3–500 caractere și confirmare în interfață. Confirming, Failed, Cancelling și Cancelled blochează orice schimbare nouă. Anularea existentă rămâne permisă în toate etapele de pregătire, deoarece nu există expediere; istoricul pregătirii se păstrează. Starea și auditul anulării sunt afișate separat.

Starea inițială este Pending, versiunea 0, inclusiv pentru comenzile anterioare migrării; nu se inventează istoric retrospectiv. Există maximum 1000 de schimbări per comandă demonstrativă. Istoricul intern include etapa inițială/finală, motivul, operatorul, data, versiunea și identificatorul operației; nu este expus prin istoricul public al clientului.

## API și concurență

`POST /api/v1/admin/sales-orders/{id}/processing` primește `operationId` (UUID), `expectedVersion`, `target` și `reason`. Returnează 204; detaliul administrativ GET include `processing` cu status/version/history. Toate operațiile cer sesiune Administrator și CSRF Identity, inclusiv direct pe Ordering. Identificatorul operatorului provine din sesiune. Gateway expune o singură rută POST explicită; răspunsurile sunt no-store.

Scrierea blochează planul rezervării în aceeași ordine ca anularea și reconcilierea. Starea, versiunea și rândul de audit se salvează în aceeași tranzacție. Două operații diferite pe aceeași versiune au un singur câștigător; cealaltă primește 409. Replay-ul aceleiași operații și aceluiași conținut returnează 204 fără audit nou, inclusiv dacă ulterior comanda a fost anulată; nu reia pregătirea și nu schimbă operatorul inițial. Reutilizarea cheii cu alt conținut răspunde 409.

Interfața recitește detaliile după succes, conflict sau răspuns pierdut, fără retransmitere automată. Dacă recitirea eșuează, formularul nu rămâne disponibil pe date vechi. Schimbarea selecției ignoră rezultatul întârziat al operației anterioare. Anularea și schimbarea etapei nu pot fi trimise simultan din același panou.

Migrarea `OrderProcessingHistory` adaugă două coloane în DemoOrders și tabela OrderProcessingChanges, cu chei unice per operație și versiune. Nu modifică stocul, prețurile sau datele clientului. Backup privat Ordering înainte de aplicarea locală; baza legacy rămâne păstrată.
