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

Politica demonstrativă este izolată în Domain/Delivery/DemoDeliveryRates. Pentru lansare sunt necesare tarife și condiții reale, configurare administrativă, restricții de greutate/destinație, integrarea curierului și validarea fiscală. Politica actuală nu trebuie activată ca ofertă comercială. Checkout-ul comercial și indexarea rămân dezactivate.
