# Confirmarea comenzilor demonstrative

Fluxul este disponibil exclusiv în Development. Nu activează checkout-ul comercial, plata, transportul sau factura. În coș, după rezervarea activă, clientul poate alege explicit „Confirmă comanda demonstrativă”. Produsele și datele clientului trebuie să fie fictive.

## API și proprietar

- `POST /api/v1/ordering/cart/preparation/{id}/confirm`: creează sau returnează aceeași comandă demonstrativă, folosind ID-ul pregătirii drept cheie idempotentă.
- `POST /api/v1/ordering/cart/preparation/{id}/complete`: după rezultatul terminal, încheie pregătirea și permite folosirea coșului.
- `GET /api/v1/ordering/demo-orders?page=1`: istoricul proprietarului cookie-ului anonim, paginat cu 25 de comenzi, cele mai noi primele. Fără cookie returnează un istoric gol; nu creează sesiune.

Toate răspunsurile sunt no-store și rutele sunt limitate prin cart-storage. Scrierile cer cookie-ul proprietarului și `X-CART-CSRF`, inclusiv pe API-ul direct. În afara Development răspund 404. ID-ul comenzii și numărul vizibil nu oferă acces la datele altui proprietar. `/api/v1/ordering/orders` rămâne inexistent, iar `checkoutEnabled` este false.

## Tranzacția locală

Ordering blochează coșul, apoi planul de rezervare. Confirmarea este permisă numai pentru o rezervare Active, fără anulare începută și cu termenul local și cel raportat de Inventory încă valabile. O rezervare Pending, expirată, eliberată sau deja consumată fără comandă nu poate crea o comandă nouă.

În aceeași tranzacție se salvează `ordering.DemoOrders` și `ReservationPlans.ConfirmationRequested`, cu starea Confirming. Comanda copiază datele PF/PJ, adresele, denumirile, ambalajele, cantitățile, moneda și prețurile din snapshot-ul pregătirii. Modificările ulterioare în Catalog nu schimbă comanda. Migrarea este `DemoOrderConfirmation`.

Numărul vizibil este DEMO- urmat de secvența PostgreSQL, cu minimum opt cifre. Este unic, dar poate avea goluri după rollback; nu este număr fiscal. ID-ul intern rămâne ID-ul pregătirii și rezervării. Reîncercările aceleiași confirmări returnează aceeași comandă, inclusiv după golirea coșului, fără consum suplimentar.

## Reconciliere cu Inventory

Worker-ul citește intenția persistentă și apelează portul Ordering `IReservationInventory.Consume`, implementat prin HTTP către Inventory. Nu există acces la baza sau proiectele Inventory.

| Rezultat observat | Stare comandă | Efect |
|---|---|---|
| Consumed | Confirmed | Stocul a fost consumat atomic; se salvează data confirmării locale. |
| Released / Expired | Failed | Nu se confirmă comanda; clientul poate relua selecția după încheierea pregătirii. |
| Timeout, răspuns invalid, 404/409 neașteptat, dependență indisponibilă | Confirming | Rezultatul rămâne incert; se reia aceeași cheie cu întârziere plafonată. |

După începerea confirmării, termenul local nu mai transformă intenția în anulare: răspunsul unui consum deja executat poate fi pierdut. La reîncercare, Inventory returnează Consumed fără o nouă scădere; dacă nu a consumat și rezervarea a expirat, returnează Expired. Starea planului și a comenzii se actualizează în aceeași tranzacție Ordering. Eșecul commitului local nu poate produce un al doilea consum remote.

Anularea și confirmarea concurente sunt serializate. Cea care începe prima stabilește intenția; nu se salvează o comandă cu intenție de anulare. Confirmarea incertă nu poate fi anulată sau desprinsă de coș. Comenzile confirmate pot fi anulate integral de administrator, cu [restituire demonstrativă](order-cancellation.md); nu există retur sau compensare comercială.

## Coș și istoric

După Confirmed, „Golește coșul confirmat” șterge selecția și datele clientului din coș și elimină snapshot-ul temporar al pregătirii. Comanda păstrează propria copie și rămâne vizibilă în istoricul browserului. După Failed, încheierea pregătirii păstrează articolele și ciorna clientului pentru o nouă verificare și rezervare; comanda eșuată rămâne în istoric.

Interfața afișează separat confirmarea în curs, rezultatul confirmat și cel nereușit. Un răspuns pierdut la confirmare blochează local acțiunile până la reîncărcarea coșului. Citirea pregătirii poate recupera numărul și starea. Istoricul oferă actualizare manuală, reîmprospătare periodică cât pagina este vizibilă și paginare. Datele comenzilor nu se scriu în localStorage.

## Limite

[Consultarea în administrare](admin-sales-orders.md) este implementată. Anularea administrativă integrală după confirmare este implementată. Nu există încă asociere cu contul, notificări, plăți, livrare, facturare sau RabbitMQ. Accesul anonim este legat de cookie, fără recuperare între dispozitive; pierderea/expirarea cookie-ului pierde accesul la acel istoric. Politicile de retenție, ștergere/anonimizare și backup rămân necesare înainte de producție. Încheierea coșului nu șterge copia personală din comandă.

Retry-ul pentru rezultate incerte este persistent și fără abandon automat. Înainte de operare comercială sunt necesare monitorizare și intervenție controlată. Noindex și checkout dezactivat rămân obligatorii.
