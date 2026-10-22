# Consultarea comenzilor în administrare

Pagina „Comenzi” (`/comenzi`) afișează comenzile demonstrative create în Ordering. Este disponibilă în mediul Development. Include [anularea integrală demonstrativă și restituirea stocului](order-cancellation.md). Nu activează operații comerciale, plată, livrare sau facturare.

## Lista și detaliile

Lista are paginare de 25 de comenzi, ordonată descrescător după numărul generat în baza de date. Filtrele sunt starea (Confirming, Confirmed, Failed, Cancelling, Cancelled) și numărul exact, cu sau fără prefixul DEMO-. Numerele inexistente produc o listă goală; valorile invalide și paginile în afara intervalului 1–100000 sunt respinse cu 400.

Lista arată numărul, starea, numele clientului, totalul și data. Selectarea citește separat comanda după ID. Detaliile includ datele PF/PJ, contactul, adresele de livrare și facturare, produsele/SKU-urile, ambalajele, cantitățile, prețurile net/brut salvate, moneda și momentele creării/finalizării. Nu se recitesc prețurile curente din Catalog.

Starea rezervării, numărul de erori consecutive și referința din jurnalul stocului pot fi consultate în „Detalii procesare”. O confirmare în curs sau nereușită este indicată explicit. Citirea comenzii și a planului folosește aceeași tranzacție RepeatableRead, evitând amestecarea unor versiuni diferite.

Filtrarea listei nu închide detaliul selectat. Butoanele de actualizare recitesc separat lista și detaliile. La eroare, datele aferente citirii eșuate sunt ascunse și se poate reîncerca; indisponibilitatea nu apare ca o listă goală. Răspunsurile întârziate ale selecțiilor anterioare sunt ignorate. Tabelul de produse permite derulare în interior pe mobil.

## API și securitate

- `GET /api/v1/admin/sales-orders?status=Confirmed&number=DEMO-00000001&page=1`
- `GET /api/v1/admin/sales-orders/{id}`

Ambele rute cer sesiune validată de Identity și rolul Administrator, inclusiv prin apel direct către Ordering. Un ID necunoscut răspunde 404 numai după autorizare. Răspunsurile de date sunt no-store. Hash-ul cookie-ului proprietarului nu este expus. Gateway-ul are două rute explicite GET și ruta POST de anulare către Ordering și nu expune rutele interne.

Ordering folosește mecanismul comun RemoteSession și necesită acum `ServiceKeys:Identity`, transmis de scriptul local din `IDENTITY_SERVICE_KEY`. Nu stochează cookie-ul administrativ și nu accesează baza Identity. O indisponibilitate Identity refuză accesul; nu există validare permisivă de rezervă. Rutele anonime de coș/comenzi continuă să folosească propriul cookie și CSRF, fără validare administrativă.

Consultarea inițială nu a schimbat schema; anularea ulterioară adaugă auditul și jurnalul de restituire descrise separat. Checkout-ul comercial rămâne dezactivat.
