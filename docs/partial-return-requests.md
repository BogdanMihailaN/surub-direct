# Cereri demonstrative de retur pe cantități în Ordering

API administrativ disponibil numai în Development. Această etapă înregistrează și decide cereri; nu recepționează produse, nu modifică Inventory și nu rambursează plata. Recepția aprobată este disponibilă prin [fluxul coordonat](partial-return-coordination.md); interfețele urmează separat.

## Selecția și snapshot-ul

Clientul API trimite `requestId`, `reason` (3–500 caractere) și `lines: [{offerId, packs}]`. Oferta identifică ambalajul din comanda originală. Ordering păstrează oferta, SKU-ul/varianta, numele, eticheta ambalajului, numărul de ambalaje și bucățile per ambalaj din snapshot-ul comenzii. Quantity este calculat pe server ca Packs × UnitsPerPack; nu se acceptă SKU-uri sau cantități în bucăți arbitrare furnizate de apelant.

Sunt acceptate 1–100 oferte distincte, ambalaje întregi strict pozitive și cel mult cantitatea cumpărată. Două oferte ale aceluiași SKU rămân linii separate, deoarece pot avea ambalaje diferite. Agregarea pe SKU pentru Inventory aparține viitoarei recepții.

Cererea necesită o comandă Confirmed, Delivered, cu returul integral NotRequested. Cantitățile din toate cererile nerespinse se cumulează per ofertă și nu pot depăși comanda. O comandă poate avea cel mult 100 de cereri în această versiune, inclusiv cele respinse; listarea este astfel limitată și ordonată după dată și ID.

## API administrativ

- `POST /api/v1/admin/sales-orders/{orderId}/partial-returns`: înregistrează cererea și întoarce snapshot-ul persistent.
- `GET /api/v1/admin/sales-orders/{orderId}/partial-returns`: listează cererile cu audit, inclusiv cele respinse; pentru o comandă necunoscută răspunde 404.
- `POST /api/v1/admin/sales-orders/{orderId}/partial-returns/{requestId}/decision`: primește operationId, expectedVersion, target (Approved/Rejected), reason și întoarce 204.

Gateway-ul YARP are rute explicite pentru listare/creare și decizie, fără expunerea rutelor interne. Toate rutele cer sesiune Administrator, inclusiv direct pe Ordering. Mutațiile cer CSRF Identity. Operatorul provine exclusiv din sesiune; răspunsurile sunt no-store. Nu există încă endpoint de client pentru acest flux și auditul nu este expus istoricului public.

## Idempotență, decizii și concurență

RequestId identifică global cererea. Replay-ul exact întoarce cererea existentă și starea ei curentă, inclusiv după decizie; nu redeschide cererea. Alt conținut, altă comandă ori alt operator răspunde 409. Ordinea liniilor nu contează. Datele sunt normalizate la precizia PostgreSQL.

Tranzițiile sunt Requested → Approved sau Rejected. Decizia păstrează identificatorul operației, motivul, operatorul și data. Replay-ul identic este acceptat; altă operație după decizie sau o versiune depășită răspunde 409. Respingerea eliberează cantitățile pentru o cerere nouă. Aprobarea le păstrează rezervate în evidența retururilor, fără să creeze rezervări de stoc.

Crearea și decizia blochează cheia cererii și apoi ReservationPlans, folosind același lock de comandă ca returul integral. Două cereri simultane nu pot depăși cantitățile cumpărate. O cerere parțială activă blochează returul integral, inclusiv cel cerut din cont. După respingerea tuturor cererilor parțiale se poate începe returul integral. Odată început, returul integral blochează orice cerere parțială nouă. Replay-urile păstrează rezultatele anterioare.

## Persistență și limite

Migrarea aditivă PartialReturnRequests creează PartialReturns și PartialReturnLines în baza Ordering, cu relație la comanda originală și cheie unică per cerere/ofertă. Nu există acces la baza Inventory sau referințe la proiectele sale.

Recepția explicită și retry/reconcilierea cu Inventory sunt implementate în [fluxul coordonat](partial-return-coordination.md). Selecția în interfețe, cererile clienților și rambursările parțiale rămân de implementat. Un retur aprobat aici nu reprezintă încă marfă primită sau bani rambursați. Nu este politica comercială/legală a magazinului.
