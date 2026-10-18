# Șurub Direct — arhitectura magazinului, propunere v0.1

Data: 17 octombrie 2026. Stare: propunere pentru revizuire, înainte de implementare.

## 1. Decizii confirmate și ipoteze

Confirmate: un singur comerciant; magazin exclusiv online; clienți persoane fizice și firme; backend .NET; frontend Angular; furnizori încă nealeși. Mirad Tehnic este doar reper de catalog, fără import sau integrare planificată.

Numele ales: **Șurub Direct**. Domeniul ales: **surubdirect.ro**. La verificarea WHOIS din 17 octombrie 2026 nu a fost găsită o înregistrare; domeniul nu a fost cumpărat sau rezervat în cadrul proiectului. Disponibilitatea se reconfirmă la înregistrare.

Convenții tehnice propuse: repository `surub-direct`, soluție și namespace de bază `SurubDirect`. Descriere propusă pentru magazin: „Șuruburi și elemente de fixare”.

Ipoteze propuse pentru prima versiune:

- România, limba română și moneda RON; datele permit ulterior alte monede, fără a implementa acum vânzarea multivalută.
- O singură locație de stoc și expedierea integrală a comenzii. Livrările parțiale rămân în afara primei versiuni.
- Vânzare din stoc propriu; produsele fără stoc rămân vizibile, dar nu sunt comandabile. Aprovizionarea direct la client și precomenzile vor necesita reguli suplimentare.
- Prețuri publice comune pentru persoane fizice și firme. Discounturile cantitative sunt prevăzute; prețurile negociate și creditul comercial sunt amânate.
- Checkout cu sau fără cont. Contul este al unei persoane; datele firmei sunt un profil de facturare, fără organizații cu mai mulți utilizatori în MVP.
- Cantități întregi pentru bucăți, cutii și kituri. Vânzarea la kilogram/metru fracționar necesită extinderea explicită a modelului.

## 2. Structură tehnică

Un repository, monolit modular cu Clean Architecture în backend: ASP.NET Core pe .NET 10 LTS, PostgreSQL și EF Core. Angular 22 pentru magazin și o aplicație Angular separată pentru administrare. Versiunile patch se fixează la pornirea implementării.

Magazinul folosește SSR pentru produse și categorii, prerandare pentru pagini editoriale stabile și randare în browser pentru cont și checkout. SSR rulează într-un proces Node.js; regulile comerciale sunt exclusiv în .NET. Prețurile personalizate și datele clienților nu intră în cache-uri publice.

API-ul și worker-ul sunt procese de găzduire ale aceleiași soluții. Worker-ul execută sarcini durabile pentru importuri, mesaje și integrări. Inițial, PostgreSQL păstrează sarcinile și evenimentele de tip outbox; nu introducem broker sau cache distribuit fără o nevoie măsurată.

Organizare propusă, cu modulele grupate după responsabilitatea comercială și straturi în fiecare modul. Creăm proiectele pe măsură ce implementăm modulele; nu generăm toate modulele goale:

```text
src/
  backend/
    SurubDirect.Api/
      Endpoints/
        Catalog/
        Sales/
    SurubDirect.Worker/
    Modules/
      Catalog/
        SurubDirect.Catalog.Domain/
        SurubDirect.Catalog.Application/
          Products/
            CreateProduct/
            GetProduct/
        SurubDirect.Catalog.Infrastructure/
        SurubDirect.Catalog.Contracts/
      Sales/
        SurubDirect.Sales.Domain/
        SurubDirect.Sales.Application/
          Orders/
            PlaceOrder/
        SurubDirect.Sales.Infrastructure/
        SurubDirect.Sales.Contracts/
      ... celelalte module, când sunt implementate
    BuildingBlocks/
  frontend/
    storefront/
    admin/
    shared/
tests/
  Unit/
  Architecture/
  Integration/
  EndToEnd/
infra/
docs/
```

Modulele planificate sunt Catalog, Pricing, Inventory, Customers, Sales, Payments, Fulfillment, Procurement și Integrations. Monolitul modular definește granițele comerciale; Clean Architecture definește responsabilitățile și dependențele codului în interiorul acestor granițe. Un proiect de cod nu este un microserviciu și nu cere o publicare separată.

### Straturi și responsabilități

| Strat | Responsabilitate | Exemple |
|---|---|---|
| Domain | Entități, value objects și reguli comerciale independente de infrastructură | Order, OrderLine, Money, reguli de tranziție a comenzii |
| Application | Cazuri de utilizare, coordonare, autorizare operațională și interfețe pentru dependențe externe | PlaceOrder, ReceiveStock, CreateProduct, IPaymentGateway |
| Infrastructure | Persistență și adaptoare tehnice care implementează interfețele interioare | EF Core, DbContext, migrații, procesator de plată, e-mail |
| Presentation / Hosts | Contractul HTTP, autentificarea la intrare, maparea cererilor/răspunsurilor și pornirea aplicației | Endpoints ASP.NET Core, middleware, API și Worker |

Contracts este interfața publică între module: operații, DTO-uri și evenimente de integrare fără dependențe de EF Core, HTTP sau entități interne. Se creează când un modul trebuie consumat de alt modul. BuildingBlocks rămâne mic și conține numai concepte tehnice comune; nu devine o colecție de entități partajate între toate modulele.

### Reguli de dependență

```text
Application    → Domain
Infrastructure → Application și Domain
API / Worker   → Application

API / Worker → Infrastructure numai la configurarea dependency injection
Modul consumator → Contracts ale modulului furnizor
```

Săgețile reprezintă dependențe de cod, nu ordinea execuției unei cereri. Domain nu referențiază celelalte straturi. Application nu referențiază Infrastructure, ASP.NET Core, EF Core sau SDK-urile furnizorilor. API-ul apelează cazurile de utilizare, fără acces direct la DbContext în endpoint-uri. Hosturile cunosc implementările concrete pentru a le înregistra în Program.cs; aceasta este limita explicită a excepției de compoziție.

Application poate consuma Contracts ale altui modul prin interfețe publice, fără referințe la Domain sau Infrastructure ale acelui modul. Implementările contractelor delegatează către cazurile de utilizare ale modulului proprietar. Evităm dependențele circulare; coordonarea checkout-ului aparține Sales, iar notificările independente folosesc evenimente outbox. Regulile de referențiere vor fi verificate prin teste de arhitectură.

### Aplicare practică

- Organizăm Application pe funcționalități (CreateProduct, PlaceOrder), cu separarea operațiunilor de scriere de interogări. Folosim aceeași bază de date; MediatR, event sourcing și baze separate pentru citire nu sunt cerințe ale Clean Architecture și nu sunt introduse implicit.
- Interfețele exprimă operațiuni utile: rezervare atomică de stoc, salvarea unei comenzi, citirea ofertelor. Evităm un repository generic pentru fiecare tabel. EF Core rămâne în Infrastructure, inclusiv interogările optimizate; interfețele de citire returnează DTO-uri fără IQueryable sau tipuri EF expuse către Application.
- Validarea formei cererii este la intrare; autorizarea și coordonarea sunt în Application; invariantele comerciale sunt în Domain. Constrângerile bazei și operațiunile atomice completează protecția la concurență.
- În aceeași bază, modulele pot participa la o tranzacție coordonată pentru plasarea comenzii, prin interfețe explicite. Coordonatorul tranzacțional este implementat în Infrastructure, iar Application depinde de abstracția lui. Dacă sunt folosite DbContext-uri separate, acestea trebuie înrolate explicit în aceeași conexiune și tranzacție; mai multe SaveChanges independente nu asigură atomicitatea comenzii și stocului.
- Nu efectuăm apeluri către plăți, curier sau e-mail în tranzacția bazei de date. Persistăm evenimentele outbox odată cu modificările și procesăm efectele externe după commit, cu idempotență.
- Testele unitare verifică regulile Domain și cazurile de utilizare. Testele cu PostgreSQL real verifică tranzacțiile, interogările și concurența; testele de arhitectură verifică granițele straturilor și modulelor.

Frontend-ul Angular se organizează pe funcționalități, componente și servicii de acces la API. Logica comercială autoritară rămâne în backend; nu duplicăm mecanic straturile .NET în frontend.

Referință: [Microsoft — Clean Architecture și inversarea dependențelor](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture). Structura modulară concretă de mai sus este propunerea pentru Șurub Direct.

## 3. Modelul catalogului

Relația principală este **produs → variantă fizică (SKU) → ofertă de vânzare/ambalare**.

Produsul grupează descrierea și variantele. SKU-ul identifică exact articolul urmărit în stoc. Oferta precizează câte unități de stoc primește cumpărătorul la o unitate comandată și la ce preț.

| Entitate | Câmpuri principale | Reguli |
|---|---|---|
| Category | Id, ParentId, Name, Slug, SortOrder, IsActive | Ierarhie fără cicluri; slug unic în ruta publică |
| Product | Id, Name, Slug, Description, PrimaryCategoryId, BrandId?, Status | Draft / Published / Archived; slug unic |
| ProductCategory | ProductId, CategoryId | Categorii secundare, fără duplicarea produsului |
| Brand | Id, Name | Opțional; nu presupunem brand pentru produse generice |
| ProductVariant | Id, ProductId, Sku, Name, StockUnit, Barcode?, IsActive | SKU intern unic, stabil; StockUnit = Piece sau Kit în MVP |
| SalesOffer | Id, VariantId, Code, PackLabel, UnitsPerPack, MinPacks, PackStep, IsActive | Numere întregi pozitive; prețul se referă la o unitate de vânzare |
| MediaAsset | Id, StorageKey, ContentType, Size, AltText | Fișiere în object storage, metadate în bază |
| ProductMedia | ProductId, VariantId?, MediaAssetId, Role, SortOrder | Imagini, fișe tehnice, certificate asociate explicit |
| ShippingProfile | SalesOfferId, WeightGrams, LengthMm, WidthMm, HeightMm, HandlingClass | Greutate și dimensiuni per ambalaj; reguli speciale pentru articole lungi/grele |

Exemplu: SKU-ul `SH-M8-40-ZN` poate avea oferta `SH-M8-40-BUC` (1 bucată) și `SH-M8-40-C100` (100 de bucăți). O comandă de două cutii rezervă 200 de bucăți.

Conversia se aplică doar dacă marfa este interschimbabilă și ambalajele pot fi desfăcute/reconstituite. O cutie sigilată, un set cu certificat propriu sau un kit cu identitate comercială distinctă primește SKU propriu. Nu implementăm asamblarea/dezasamblarea kiturilor în MVP.

Odată utilizate în operațiuni comerciale, StockUnit și UnitsPerPack nu se schimbă: se creează SKU/ofertă nouă și se dezactivează cea veche. Înregistrările folosite în comenzi se arhivează, nu se șterg.

Cotarea transportului folosește greutatea, dimensiunile și regulile de consolidare în colete ale integrării alese. Nu însumăm simplist dimensiunile ambalajelor. Lipsa datelor necesare sau depășirea limitelor curierului blochează promisiunea unui tarif automat și cere o metodă de livrare configurată pentru acel caz.

## 4. Atribute și filtre

| Entitate | Câmpuri principale | Rol |
|---|---|---|
| AttributeDefinition | Id, Code, Name, DataType, Unit?, Scope | Definește atribute numerice, text, enum sau boolean; Scope = Product / Variant |
| AttributeOption | Id, AttributeId, Code, Label | Valori controlate pentru enum, de exemplu finisaj |
| CategoryAttribute | CategoryId, AttributeId, Required, Filterable, SortOrder | Schema efectivă a categoriei, fără moștenire implicită în MVP |
| ProductAttributeValue | ProductId, AttributeId, valoare tipizată | Atribute comune unei familii |
| VariantAttributeValue | VariantId, AttributeId, valoare tipizată | Atribute care identifică varianta |

Fiecare valoare are exact un tip completat, compatibil cu definiția: NumericValue, TextValue, BooleanValue sau OptionId. Unicitate pe perechea entitate–atribut. Atributul are un singur scope, pentru a evita suprascrieri ambigue între produs și variantă.

Diametrul și lungimea se stochează numeric în mm; interfața afișează unitatea. Materialul, finisajul și tipul capului folosesc vocabular controlat. Standardele se păstrează separat de texte descriptive; se pot adăuga mai multe referințe de standard printr-o relație VariantStandard. Nu deducem conformitatea dintr-un nume sau din asemănarea cu un produs concurent.

Pentru fiecare SKU păstrăm o combinație unică a atributelor de variantă în interiorul produsului, verificată la salvare/import. Publicarea cere toate atributele obligatorii și cel puțin o ofertă activă cu preț.

Căutarea inițială folosește PostgreSQL: cod exact prioritar, apoi denumiri și atribute. Normalizăm diacriticele și notații precum `M8x40`, `M8 x 40`, `M8×40`. La filtrare, toate condițiile unei căutări trebuie să fie satisfăcute de aceeași variantă; nu combinăm diametrul unui SKU cu lungimea altuia pentru a produce un rezultat fals.

## 5. Prețuri

| Entitate | Câmpuri principale | Reguli |
|---|---|---|
| OfferPrice | Id, SalesOfferId, Currency, NetAmount, ValidFrom, ValidTo? | Un singur preț de bază activ/ofertă/monedă la un moment dat |
| QuantityPrice | Id, OfferPriceId, MinPacks, NetAmountPerPack | Prag exprimat în ambalaje ale aceleiași oferte; prag unic |
| TaxCategory | Id, Code, Name | Asociată SKU-ului |
| TaxRate | Id, TaxCategoryId, Jurisdiction, Rate, ValidFrom, ValidTo? | Rate configurabile, fără perioade suprapuse |

Prețul de achiziție și prețul de vânzare sunt date separate. Schimbarea costului de la furnizor nu schimbă automat prețul public în MVP.

Sumele și ratele sunt decimal în .NET și numeric în PostgreSQL; nu folosim float/double pentru bani. Prețurile unitare pot păstra până la șase zecimale pentru articole ieftine; totalurile RON sunt rotunjite la două zecimale după o politică unică, aliniată cu integrarea de facturare. TVA se calculează pe liniile comenzii conform aceleiași politici. Cota aplicată și valorile finale se salvează în comandă.

Pagina afișează explicit prețul ambalajului, numărul de bucăți și prețul echivalent pe bucată. Prețul echivalent este informativ dacă vânzarea la bucată nu este disponibilă. Backend-ul calculează toate totalurile; frontend-ul nu furnizează prețuri de încredere.

## 6. Stoc și aprovizionare

| Entitate | Câmpuri principale | Reguli |
|---|---|---|
| StockLocation | Id, Code, Name | O locație activă inițial |
| InventoryBalance | VariantId, LocationId, OnHand, Reserved, Version | Pereche unică; OnHand ≥ Reserved ≥ 0 |
| InventoryMovement | Id, VariantId, LocationId, QuantityDelta, Type, Reference, Reason?, CreatedAt, ActorId | Registru imutabil; corecții prin mișcări compensatoare |
| StockReservation | Id, OrderId, VariantId, LocationId, Quantity, Status, ExpiresAt? | Active / Consumed / Released; tranziții idempotente |
| Supplier | Id, Name, ContactData, IsActive | Completat când sunt aleși furnizorii |
| SupplierItem | Id, SupplierId, SupplierCode, VariantId, PurchasePackUnits, LastCost?, Currency | Cod unic în cadrul furnizorului; asociere verificată manual |
| GoodsReceipt | Id, SupplierId?, Reference, Status, ReceivedAt | Draft / Posted / Reversed |
| GoodsReceiptLine | Id, ReceiptId, VariantId, BaseQuantity, UnitCost?, SupplierItemId? | Cantitate în unitatea SKU-ului; documentul păstrează conversia de achiziție |

**Available = OnHand − Reserved.**

InventoryBalance este proiecția folosită pentru operațiuni rapide. Mișcările și rezervările păstrează istoricul și permit reconcilierea. Mișcările fizice, actualizarea soldului și schimbările rezervărilor se fac în tranzacții, cu operațiuni atomice și protecție la concurență. Nu citim soldul și apoi îl suprascriem fără verificare.

La expediere scad atât OnHand, cât și Reserved pentru cantitatea rezervată. La anulare se eliberează doar rezervarea. Un retur crește stocul vandabil doar după recepție și verificare; produsele deteriorate nu sunt repuse automat în vânzare.

Primirea mărfii nu depinde de existența unui API de furnizor. În MVP putem înregistra manual recepții și importa linii de recepție din fișier. Evidența contabilă și metodele de evaluare contabilă a stocului rămân în sistemul de contabilitate/facturare ales.

## 7. Clienți, coș și comenzi

| Entitate | Date principale |
|---|---|
| Customer | Id, IdentityUserId, Email, Phone?, Name |
| BillingProfile | Id, CustomerId, Type, PersonName? / CompanyName + TaxId, Address |
| Address | Destinatar, telefon, țară, județ/regiune, localitate, stradă, cod poștal |
| Cart / CartLine | Client sau sesiune anonimă, SalesOfferId, QuantityPacks |
| Order | Id, Number, CustomerId?, ContactSnapshot, BillingSnapshot, ShippingSnapshot, Currency, Totals, Status, CreatedAt |
| OrderLine | OrderId, VariantId, SalesOfferId, QuantityPacks, UnitsPerPackSnapshot, SkuSnapshot, NameSnapshot, PriceSnapshot, TaxSnapshot, LineTotals |
| Payment / PaymentEvent | OrderId, Provider, ProviderReference, Amount, Currency, Status; ID unic al evenimentului primit |
| Shipment | OrderId, Carrier, TrackingNumber?, Status, ShippingCostSnapshot |
| Return / ReturnLine | OrderId, OrderLineId, Quantity, Reason, InspectionStatus, RestockedQuantity |
| Refund | PaymentId, ReturnId?, Amount, Status, ProviderReference? |

Datele de facturare și livrare sunt copiate în comandă; modificarea profilului clientului nu schimbă documentele istorice. Cantitatea și ambalajul comandat sunt păstrate în aceeași manieră.

Stările comenzii, plății și expedierii sunt distincte. O comandă poate fi confirmată și încă neplătită în cazul rambursului. Returul fizic și rambursarea banilor sunt operațiuni separate.

Flux inițial pentru plata online:

1. Backend-ul validează ofertele, cantitățile, prețurile, adresele și metoda de livrare.
2. Într-o tranzacție, rezervă stocul și creează comanda PendingPayment și evenimentul outbox. O cheie de idempotență previne dublarea comenzii la retry.
3. După commit, creează sesiunea de plată la procesator, cu idempotență și retry. Apelurile externe nu țin tranzacția de stoc deschisă.
4. Confirmarea semnată/verificată a procesatorului și verificarea sumei/monedei determină statusul plății. Redirecționarea browserului nu este dovadă de plată.
5. La succes, comanda devine Confirmed și rezervarea este păstrată până la expediere.
6. Rezervările neplătite expiră după un termen configurabil. Jobul verifică/reconciliază plata înainte de eliberare. Dacă plata confirmată sosește după eliberarea stocului, sistemul încearcă o nouă rezervare; în lipsa stocului, marchează excepția pentru rezolvare și rambursare, fără a confirma automat expedierea.

Pentru ramburs, comanda se confirmă conform regulilor comerciale și păstrează rezervarea până la expediere/anulare. Durata rezervării pentru transfer bancar se stabilește când alegem metodele de plată.

## 8. Import și administrare

Două fluxuri distincte:

**Catalog:** încărcare fișier → validare → previzualizare diferențe → confirmare → aplicare. Produsele noi sunt ciorne. Importul nu modifică stocul.

**Stoc:** încărcare recepție → validare SKU-uri/conversii/cantități → previzualizare → postare document. Postarea generează mișcări; un document postat nu poate fi postat încă o dată.

Entități de suport: ImportBatch (tip, stare, checksum, autor, date), ImportRow (număr, cheie, date normalizate, erori, rezultat) și AuditEntry (actor, acțiune, referință, moment).

Nu folosim identitatea din fișier drept ID intern. Actualizările catalogului se identifică prin SKU și codul ofertei; codul furnizorului se folosește numai în contextul furnizorului. Reimportul are rezultat previzibil; câmpurile lipsă nu șterg valori existente implicit.

O structură de import minimă poate fi împărțită în patru fișiere/foi: produse, variante, oferte, atribute. Recepțiile au fișier separat. Este mai clar decât repetarea descrierilor și stocului pe fiecare ambalaj.

## 9. Contracte API propuse

REST/JSON și OpenAPI, cu client TypeScript generat pentru cele două aplicații Angular. Liste paginate și filtre validate în backend.

| Rută orientativă | Scop |
|---|---|
| GET /api/v1/categories | Navigație și categorii |
| GET /api/v1/products | Căutare, filtrare, sortare, paginare |
| GET /api/v1/products/{slug} | Produs, variante și oferte |
| POST /api/v1/carts; PUT /api/v1/carts/{id}/lines/{offerId} | Coș și cantități |
| POST /api/v1/checkout/quote | Totaluri calculate, transport și validări; fără rezervare |
| POST /api/v1/orders | Plasare cu Idempotency-Key și rezervare atomică |
| GET /api/v1/account/orders | Istoricul utilizatorului autentificat |
| POST /api/v1/payments/webhooks/{provider} | Confirmări autentificate și deduplicate |
| POST /api/v1/admin/imports | Încărcare și validare import |
| POST /api/v1/admin/imports/{id}/apply | Aplicarea importului previzualizat |
| POST /api/v1/admin/goods-receipts/{id}/post | Postarea unei recepții |

Un quote nu garantează stocul și nu fixează prețul pe termen nelimitat. Dacă prețul se schimbă înainte de plasare, backend-ul cere reconfirmarea valorilor actualizate.

## 10. Acces, operare și validare

Identitatea folosește componente standard ASP.NET Core Identity. Pentru aplicațiile web propun cookie-uri HttpOnly/Secure și protecție CSRF, cu rutare API sub aceeași origine pentru fiecare aplicație. Accesul se verifică la nivel de resursă: un client nu poate citi coșul/comanda altuia. Comenzile fără cont se consultă printr-un mecanism verificat, cu token aleator limitat, nu doar după număr sau e-mail.

Roluri inițiale: administrator, operator catalog și operator comenzi/stoc. MFA pentru administrator, jurnal de acțiuni și secrete în configurația securizată a mediului. Încărcările de fișiere au limite de dimensiune și tip și nu sunt executabile.

Găzduire inițială: servicii/container(e) pentru API, worker și Angular SSR; administrarea și resursele statice sunt servite prin CDN/proxy. Bază PostgreSQL administrată, object storage și mediu staging separat. Furnizorul de hosting se alege după buget și volumul estimat; nu este necesar Kubernetes.

CI/CD: build, verificări automate relevante, migrații controlate și publicare în staging înainte de producție. Backup automat cu test de restaurare, health checks, loguri corelate și alerte pentru plăți/importuri eșuate. Indexurile și căutarea se verifică pe catalog reprezentativ înainte de alegerea unui motor suplimentar.

Scenarii de acceptanță obligatorii:

- Doi clienți încearcă simultan ultima cutie: numai o rezervare reușește.
- Două ambalaje ale aceluiași SKU consumă același stoc în unități de bază.
- SKU-urile de kit nu consumă implicit stocul componentelor.
- Retrimiterea plasării comenzii sau a webhook-ului nu dublează efectele.
- Importul repetat nu dublează produse sau recepții.
- Modificarea unui produs/preț/profil nu schimbă comenzile existente.
- Anularea, expirarea și expedierea nu eliberează/consumă de două ori aceeași rezervare.
- Plata întârziată după expirare produce reconciliere, nu stoc negativ.
- Filtrele combinate corespund unui SKU real, inclusiv atunci când familia are mai multe variante.
- Totalurile pentru articole foarte ieftine, discounturi și TVA sunt coerente cu factura.

## 11. Ce urmează

Catalogul demonstrativ din documentul alăturat verifică modelul, fără date comerciale reale. După revizuire, următorul pas este schema relațională executabilă (migrații EF Core), apoi prima funcționalitate completă: creare produs în administrare → ofertă și preț → recepție stoc → afișare în magazin.

Înainte de lansare trebuie decise furnizorii, politica reală de ambalare, plățile, curierul, facturarea și operațiunile de depozit. Aceste alegeri nu blochează implementarea catalogului, dar afectează fluxul comercial final.
