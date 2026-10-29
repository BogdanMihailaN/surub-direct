# Asocierea comenzilor demonstrative cu un cont

Ordering păstrează `AccountId` (identificatorul stabil al utilizatorului Identity) și `AccountLinkedAt` în DemoOrders. Nu citește baza Identity și nu revendică automat comenzi după adresa de e-mail. Migrarea `AccountOrderOwnership` adaugă două coloane nullable și un index AccountId/Sequence; comenzile existente rămân anonime.

## Protocol

- `POST /api/v1/ordering/account/orders/{id}/claim`: necesită sesiune autentificată validată la Identity, token `X-CSRF-TOKEN` al acelei sesiuni și cookie-ul original `SurubDirect.Cart`. Contul destinație provine exclusiv din principalul validat; nu poate fi ales în corpul cererii. Nu se folosește tokenul anonim X-CART-CSRF pentru această operație.
- `GET /api/v1/ordering/account/orders?page=1`: istoric paginat, 25 de comenzi/pagină, al contului autentificat. Nu necesită cookie de coș și funcționează din alt browser după autentificare.
- Endpoint-urile sunt disponibile doar în Development și sunt protejate inclusiv direct pe Ordering, independent de gateway. Răspunsurile handler-elor au `Cache-Control: no-store`; indisponibilitatea Identity produce eroare, nu acces anonim.

Asocierea este explicită și permisă doar pentru comenzi Confirmed sau Cancelled, după încheierea fluxului coșului: coșul nu mai referă pregătirea comenzii, iar snapshot-ul PreparedCart a fost eliminat. Astfel, datele comenzii nu rămân accesibile prin vechiul endpoint de pregătire. Comenzile Confirming, Cancelling sau Failed nu sunt eligibile pentru prima asociere.

Repository-ul blochează mai întâi coșul, apoi comanda, în aceeași tranzacție. Verifică potrivirea OwnerHash și CartId, eliminarea pregătirii și asocierea existentă. Repetarea pentru același cont este idempotentă și păstrează momentul inițial; un alt cont primește 404. Două conturi concurente nu pot câștiga ambele.

După asociere, citirea anonimă nu mai include comanda. Nici retry-ul anonim de confirmare, nici finalizarea anonimă nu returnează snapshot-ul asociat. Comanda rămâne disponibilă administratorului prin fluxul administrativ existent. Valorile comerciale și rezervările nu sunt modificate de asociere.

## Limite și continuare

Pagina Angular `/cont` afișează separat comenzile contului și comenzile neasociate din browser, cu paginare independentă și detalii. Asocierea este inițiată pe o comandă confirmată/anulată și cere confirmare explicită cu e-mailul contului destinație; Renunță nu trimite cererea. Un coș încă legat de pregătire rămâne blocat de validarea serverului. Coșul activ rămâne anonim; autentificarea nu îl leagă automat, nu îl unește cu alt coș și nu transferă istoricul implicit. Cererea de asociere trebuie făcută înainte de pierderea cookie-ului original; odată asociată, comanda nu depinde de durata cookie-ului coșului.

Urmează politica de proprietate a coșului activ. Confirmarea e-mailului, recuperarea parolei și activarea comercială rămân în backlog. Nu se face downgrade al migrației în prezența comenzilor asociate fără un plan de păstrare a proprietății: eliminarea coloanelor ar pierde această restricție de acces.

## Protecția interfeței la schimbarea sesiunii

Identity returnează identificatorul stabil `id` în `/api/auth/me`, cu `Cache-Control: no-store`. Interfața îl trimite drept `expectedAccountId` la citirea istoricului și asociere. Parametrul este o precondiție, nu un cont destinație: dacă diferă de identitatea validată, Ordering răspunde 409. Este opțional pentru compatibilitatea clienților API existenți; autoritatea rămâne exclusiv sesiunea validată de Identity.

Listele sunt recitite după asociere, inclusiv după un răspuns pierdut; o comandă regăsită în istoricul contului confirmă rezultatul. Erorile nu sunt afișate ca liste goale, iar datele vechi sunt eliminate înainte de reîncărcare. Asocierea este indisponibilă cât timp istoricul contului nu poate fi validat. Deconectarea sau reverificarea sesiunii distruge componentele și starea istoricului; butonul Reverifică sesiunea permite recuperarea explicită.
