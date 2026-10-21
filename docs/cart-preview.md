# Coș demonstrativ și recalculare în Ordering

Pagina produsului permite alegerea numărului de ambalaje și adăugarea în coș. Dacă ambalajul există deja, acțiunea **Actualizează coșul** înlocuiește cantitatea explicită. Pagina `/cos` permite modificarea și eliminarea articolelor, recalcularea și consultarea totalurilor. Coșul este demonstrativ; checkout-ul, comenzile și plățile nu sunt activate, iar `noindex` rămâne prezent.

## Păstrarea selecției

Selecția este salvată în baza proprie Ordering, fără prețuri persistate. Datele PF/PJ pot fi salvate separat în ciorna atașată coșului; vezi [formularul și limitele](customer-draft.md). Cookie-ul HttpOnly identifică un coș anonim, valabil 30 de zile. Copia locală din versiunea anterioară este importată la prima încărcare reușită. Modificările concurente sunt respinse și necesită reîncărcare. [Protocol, securitate și recuperare](cart-persistence.md).

## Recalcularea autoritativă

`POST /api/v1/ordering/cart/quote` primește `{ lines: [{ productSlug, offerId, packs }] }`. Este o interogare publică fără efecte persistente; nu folosește sesiunea administrativă și nu modifică stocul. Nu creează comenzi, rezervări sau plăți. Gateway-ul folosește ruta Ordering deja existentă, fără expunerea rutelor interne.

Ordering recitește produsele publicate prin HTTP din Catalog, prin portul propriu `ICartCatalog` și DTO-uri de transport locale. Nu referă proiectele sau baza Catalog. Prețurile primite de la browser nu sunt folosite. Fiecare produs distinct este citit o singură dată per cerere. Timeout HTTP 5 secunde, fără cookie jar sau redirectări.

Validările includ:

- ID-uri și slug-uri valide, cantități întregi pozitive, maximum 50 de ambalaje, fără duplicate;
- minimum și pas după formula `(packs - minPacks) % packStep == 0`, cu maximum 2.147.483.647 bucăți per linie;
- produs publicat, ambalaj existent și preț curent disponibil;
- cumularea bucăților cerute de **toate ambalajele aceluiași SKU**, pentru a nu verifica separat același sold;
- aceeași monedă pentru totalul coșului și limită de 1.000.000.000 pentru valoarea brută a unei linii.

Prețul brut per ambalaj este cel rotunjit furnizat de Catalog, înmulțit cu numărul de ambalaje. Netul liniei este rotunjit la două zecimale, AwayFromZero. Totalurile însumează liniile; transportul nu este inclus. Aceste valori sunt estimări demonstrative, nu facturi sau oferte cu preț blocat.

Un produs arhivat, un preț lipsă, stocul insuficient sau cantitatea neconformă produce explicații pe linii și elimină totalul valid. O dependență indisponibilă sau un răspuns nevalid produce 503, fără a inventa stoc zero. Interfața elimină totalul anterior când începe recalcularea și ignoră răspunsurile vechi dacă selecția s-a schimbat între timp.

Răspunsul include `checkedAt` și `checkoutEnabled: false`, cu `Cache-Control: no-store`. Verificarea nu rezervă stoc și nu constituie o imagine atomică a tuturor produselor; disponibilitatea se poate schimba imediat după citire. Viitorul flux de comandă trebuie să revalideze și să rezerve independent.

## Organizare și limite

Ordering are acum Domain, Application, Infrastructure, Contracts și API cu fișiere separate. Domain conține selecția și regulile de cantitate; Application coordonează recalcularea, Infrastructure adaptează HTTP, iar API compune serviciile și expune endpoint-ul. Persistența selecției este separată în use case-urile StoredCart și în OrderingDbContext; migrarea PersistedCarts creează ordering.Carts.

Endpoint-ul limitează cererile la 60/minut pentru adresa peer observată de serviciu, fără coadă; în configurația locală, traficul gateway are același peer. Body limit 64 KB. Înainte de producție trebuie definită politica pentru proxy-uri de încredere și capacitatea de trafic. Limita globală a gateway-ului rămâne neschimbată.

Următoarele etape sunt asocierea opțională cu un cont, plasarea comenzii, rezervările și expirările, apoi coordonarea prin Outbox/Inbox și RabbitMQ. Activarea comercială necesită date reale și autorizare separată.
