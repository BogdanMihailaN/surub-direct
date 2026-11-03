# Transport în comenzile demonstrative

Etapă Development, 3 noiembrie 2026. Nu există curier conectat, AWB, termen de livrare garantat, plată sau factură. Tarifele sunt ilustrative, nu oferte comerciale și nu reguli fiscale pentru lansare.

## Selecție și totaluri

Înainte de pregătirea coșului, vizitatorul sau clientul autentificat poate alege:

- Livrare standard demonstrativă: 20 RON brut / 16,53 RON net.
- Livrare expres demonstrativă: 30 RON brut / 24,79 RON net.
- Fără transport inclus — simulare: compatibilitatea fluxului demonstrativ anterior; absența unui tarif nu înseamnă transport gratuit.

Lista se citește din `GET /api/v1/ordering/delivery-options`, disponibil numai în Development, cu no-store. O eroare este afișată explicit și poate fi reîncercată. Nu se derivă un tarif zero din indisponibilitate. Codul metodei este transmis în `deliveryCode` către pregătirea anonimă sau de cont, cu protecțiile existente CSRF/proprietar/versiune.

Recalcularea obișnuită a coșului arată în continuare subtotalul produselor și precizează că transportul nu este inclus. La pregătire, backendul validează metoda, moneda RON și adresa RO, calculează costul din politica demonstrativă și salvează snapshot-ul. Clientul nu transmite sume de transport. Totalul pregătirii/comenzii este suma liniilor plus transportul; rezumatul separă și identifică transportul înainte de confirmare. Listele administrative, detaliile și istoricul contului/browserului folosesc totalurile salvate.

Selecția nepregătită este temporară în interfață, nu o preferință persistentă între dispozitive. După rezervare, transportul este blocat împreună cu restul snapshot-ului. Schimbarea lui necesită eliberarea pregătirii și o pregătire nouă. Replay-ul cu aceeași cheie și alt transport răspunde 409; nu modifică snapshot-ul existent.

## Persistență și compatibilitate

Migrarea `DemoDeliverySnapshots` adaugă câte o coloană JSONB nullable în PreparedCarts și DemoOrders. Comenzile/pregătirile vechi rămân cu transport neinclus, fără recalculare retrospectivă. Costul și denumirea sunt copiate din pregătire în comandă și rămân păstrate după golirea coșului și anulare. Anularea restituie doar produsele în Inventory; nu creează o rambursare a transportului.

Regulile de selecție sunt în Domain/Delivery/DeliverySelection; tarifele vin prin portul Application/IDeliveryRates și adaptorul Infrastructure/Delivery/ConfiguredDeliveryRates. Pentru lansare sunt necesare tarife și condiții reale, configurare administrativă, restricții de greutate/destinație, integrarea curierului și validarea fiscală. Politica actuală nu trebuie activată ca ofertă comercială. Checkout-ul comercial și indexarea rămân dezactivate.

## Configurarea tarifelor — 3 noiembrie 2026

Metodele implicite ilustrative sunt în `backend/src/services/ordering/SurubDirect.Ordering.Api/appsettings.Development.json`, secțiunea `Delivery:Methods`. Pentru fiecare metodă se configurează `Code`, `Label`, `Currency`, `NetAmount`, `GrossAmount` și `Enabled`. Sumele net/brut sunt independente și trebuie validate comercial înaintea lansării; aplicația nu deduce o cotă de TVA.

Configurația acceptă maximum 20 de metode cu cod unic de maximum 64 de caractere (litere mici, cifre și cratime), etichetă de maximum 120 de caractere fără caractere de control, moneda RON și sume pozitive cu maximum două zecimale, net ≤ brut ≤ 10000. Duplicatele și intrările invalide, inclusiv cele dezactivate, împiedică pornirea. O configurație absentă sau fără metode active produce o listă goală, fără tarife implicite ascunse.

Pentru dezactivare se setează `Enabled: false`. Pentru modificarea tarifului se actualizează cele două sume și se repornește Ordering; modificările fișierului nu sunt aplicate dinamic cererilor în curs. Configurația este copiată într-o listă read-only la pornire. Variabilele de mediu .NET pot suprascrie câmpuri, de exemplu `Delivery__Methods__0__Enabled=false` (variabilă exportată în mediul procesului, nu o intrare arbitrară în `.env` citită de script).

Metodele dezactivate nu sunt expuse în listă și nu pot fi alese la o pregătire nouă. Un browser cu opțiuni vechi primește o eroare explicită la pregătire și trebuie să reîncarce opțiunile/pagina; nu este selectat automat alt tarif. Pregătirile existente și replay-urile lor păstrează costul inițial, chiar dacă metoda a fost dezactivată sau redenumită. Istoricul comenzilor nu este recalculat.

În producție, adaptorul este înregistrat fără metode demonstrative și endpoint-urile demonstrative rămân inaccesibile. Această etapă nu adaugă editor administrativ de tarife, condiții geografice/greutate sau integrarea unui curier. Nu sunt necesare migrații de bază de date.
