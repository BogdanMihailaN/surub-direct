# Organizarea proiectului

```text
surub-direct/
├── backend/
│   ├── SurubDirect.sln
│   ├── Directory.Build.props
│   ├── src/
│   │   ├── services/
│   │   │   ├── catalog/
│   │   │   │   ├── SurubDirect.Catalog.{Api,Application,Domain,Infrastructure,Contracts}/
│   │   │   │   ├── SurubDirect.Pricing.{Application,Domain,Infrastructure,Contracts}/
│   │   │   │   ├── SurubDirect.Store.{Application,Infrastructure}/
│   │   │   │   └── SurubDirect.Catalog.Worker/
│   │   │   ├── inventory/SurubDirect.Inventory.{Api,Application,Domain,Infrastructure,Contracts}/
│   │   │   ├── identity/SurubDirect.Identity.{Api,Infrastructure}/
│   │   │   ├── purchasing/SurubDirect.Purchasing.{Api,Application,Domain,Infrastructure,Contracts}/
│   │   │   └── ordering/SurubDirect.Ordering.{Api,Application,Domain,Infrastructure,Contracts}/
│   │   ├── gateway/SurubDirect.Gateway/
│   │   └── shared/
│   │       ├── SurubDirect.BuildingBlocks/
│   │       └── SurubDirect.ServiceDefaults/
│   └── tests/SurubDirect.Tests/
│       ├── Unit/
│       ├── Integration/
│       └── Architecture/
├── frontend/
│   ├── src/
│   │   ├── apps/
│   │   │   ├── storefront/
│   │   │   └── admin/
│   │   └── shared/
│   ├── tests/e2e/
│   ├── angular.json
│   ├── package.json
│   └── tsconfig.json
├── scripts/                          # pornire și operații locale pentru întregul proiect
├── docs/                             # arhitectură, convenții și verificări
├── compose.yaml                      # infrastructura locală comună
├── global.json                       # SDK .NET, descoperit și din rădăcină
└── README.md
```

Notația `{...}` abreviază directoarele reale, nu reprezintă un director literal.

Codul de producție se află exclusiv în `backend/src` și `frontend/src`; testele sunt în directoarele `tests` corespunzătoare. Serviciile nu au încă un nivel `src` interior. Configurațiile build-ului rămân la rădăcina backend-ului sau frontend-ului, iar cele comune întregului proiect rămân în rădăcina repository-ului.

## Reguli backend

O clasă, înregistrare, enumerare sau interfață principală per fișier. Domain conține regulile comerciale, fără framework-uri de persistență sau HTTP. Application organizează operațiile în `Features/<resursă>/<operație>`, cu handler și cerere proprii, plus porturi în `Abstractions`.

Infrastructure conține `Persistence/Configurations`, repository-uri și migrații. Păstrăm migrațiile existente și namespace-urile lor pentru continuitatea bazei. API conține endpoint-uri separate, clienți pentru servicii externe, middleware, configurare și bootstrap; Program.cs doar le compune.

Contracts sunt contracte interne serviciului proprietar, nu un proiect comun tuturor serviciilor. Integrarea externă utilizează un port și un DTO al consumatorului. Nu introducem acces direct la contextul EF al altui serviciu sau o bibliotecă comună cu toate entitățile.

Testele comune pot referi mai multe servicii pentru verificări și adaptoare de test; această excepție nu se aplică proiectelor de producție. Testele PostgreSQL folosesc baze distincte pentru Catalog și Inventory. Testele Playwright verifică și comunicarea reală HTTP prin gateway.

## Reguli Angular

Fiecare componentă are TypeScript, HTML și SCSS alăturate. `pages` compune ecranul, `components` conține formularele și elementele vizuale, `data-access` face apelurile API, `state` coordonează operațiile, iar `models` definește cererile.

`core/auth` rămâne specific aplicației; `frontend/src/shared` conține doar cod folosit de ambele aplicații. Rutele sunt încărcate la cerere. TypeScript și șabloanele Angular au verificarea strictă activă. Browserul și SSR trimit cererile comerciale prin gateway, nu direct spre API-urile serviciilor.

Nu creăm straturi goale pentru funcționalități neimplementate și nu împărțim artificial expresii simple în servicii. Nu comitem `.tools`, `work`, secrete, artefacte de compilare sau cache-uri.

## Administrarea ciclului de viață al produsului

Operațiile sunt separate în `Catalog.Application/Features/Products/{UpdateProduct,ArchiveProduct,RestoreProduct}`, fiecare cu handler propriu și endpoint în `Catalog.Api/Endpoints/Administration/Catalog`. Regulile de editare și tranzițiile de stare sunt în `Catalog.Domain/Products/Product.cs`. Citirea administrativă după ID este compusă în `Store.Application/Features/GetAdminProduct`.

În Angular, formularul de editare și confirmarea arhivării/restaurării sunt componente distincte (`product-edit-form`, `product-lifecycle`); pagina le compune, iar `CatalogAdminStore` păstrează starea operațiilor, filtrarea, paginarea și selecția. Testele de domeniu și PostgreSQL sunt în `Unit/ProductLifecycleTests.cs` și `Integration/ProductAdministrationTests.cs`.

## Editarea variantelor și ambalajelor

`Catalog.Application/Features/Variants/UpdateVariant` și `Features/Offers/UpdateOffer` au comenzi și handler-e proprii. `Product` verifică apartenența și starea arhivată și coordonează trecerea în ciornă la schimbarea bucăților/ambalaj; `Variant` și `SalesOffer` validează câmpurile înainte de mutare. Persistența rămâne o singură salvare în Catalog, fără operații asupra bazei Inventory.

Angular are `variant-edit-form` și `offer-edit-form` distincte, cu TypeScript/HTML/SCSS alăturate. Tabelul emite intențiile de editare, `CatalogAdminStore` păstrează editorul activ, iar `data-access` trimite cererile. Modelele editării nu includ SKU sau cod ofertă. Testele au fișiere dedicate în `Unit`, `Integration` și `frontend/tests/e2e/variant-offer-edit.spec.ts`; `tests/e2e/support` conține funcțiile comune de autentificare pentru teste.

## Categorii și atribute tehnice

Catalog deține taxonomia în `Catalog.Domain/Categories` și `Attributes`; operațiile sunt granulare în `Catalog.Application/Features/Taxonomy`, cu portul `ITaxonomyRepository`. Fiecare tabel are configurație EF separată în Infrastructure. `TaxonomyProjection` calculează moștenirea și valorile de afișare; `VariantAttributeFilters` aplică filtrele pe aceeași variantă. Endpoint-urile administrative sunt în `Endpoints/Administration/Taxonomy`; salvarea valorilor SKU rămâne în `Administration/Catalog`. Gateway-ul adaugă doar rutele explicite.

Angular admin are feature-ul `taxonomy` cu `pages`, `components`, `data-access`, `state`, `models` și stiluri locale comune formularelor. `variant-attributes-form` aparține feature-ului catalog. Storefront are `CatalogStore` pentru încărcare și sincronizarea filtrelor cu URL-ul; pagina compune componentele. Modelele și funcțiile pentru arbore/moștenire din `frontend/src/shared` sunt folosite de ambele aplicații. Testele taxonomiei sunt separate în Unit, Integration, migrare și `frontend/tests/e2e/taxonomy.spec.ts`.

## Imagini și galerie

`Catalog.Domain/Images/ProductImage` și agregatul Product dețin regulile galeriei. Application are porturi distincte (`IImageProcessor`, `IImageStorage`, `IProductImageRepository`) în `Abstractions/Media` și câte un handler pentru upload, metadate, ordine, eliminare și citire în `Features/Images`. Infrastructure conține procesorul SkiaSharp și stocarea locală în `Media`, plus configurația EF și repository-ul separat. API expune un fișier per endpoint în `Administration/Images` și citirea publică în `Endpoints/Catalog`.

Admin are feature-ul `product-images`: `image-upload-form`, `image-editor` și `product-images-panel`, fiecare cu TS/HTML/SCSS alăturate; API și stare în directoarele proprii. Pagina catalogului compune panoul. Storefront folosește `product-gallery`; helper-ul pentru URL și modelul comun sunt reutilizate de cele două aplicații. Testele de procesare și persistență sunt în `Integration/Images`, regulile comerciale în `Unit/ProductImageTests.cs`, iar fluxul complet în `frontend/tests/e2e/images.spec.ts`.

## Importul CSV

`Store.Application/Features/Imports` compune operațiile Catalog și Pricing din același serviciu: parser CSV, citire de rânduri, construire de lot, handler de previzualizare și handler de salvare. Regulile comerciale reutilizează domeniile Catalog/Pricing. Portul `IProductImportRepository` este în `Abstractions/Imports`. `Store.Infrastructure/Imports` implementează citirea conflictelor și tranzacția comună celor două contexte EF, cu înregistrare DI separată; worker-ul nu primește aceste dependențe. Nicio referință spre alt serviciu.

API are câte un endpoint în `Administration/Imports`. Angular are feature-ul `product-import`, cu pagină de compoziție, componente `import-upload` și `import-preview`, API, stare și modele în directoare separate. Testele sunt în `Unit/Imports`, `Integration/Imports` și `frontend/tests/e2e/product-import.spec.ts`.

## Furnizori și condiții de achiziție

`backend/src/services/purchasing` conține serviciul complet. Domain are agregatul `Supplier` și entitatea `SupplierItem` în fișiere separate. Application are câte un handler pentru creare/editare/listă/detaliu/asociere/căutare, porturi de persistență și Catalog. Infrastructure conține configurații EF separate, proiecția DTO, repository-ul tranzacțional și migrarea inițială. API are un endpoint per fișier și clientul HTTP Catalog, fără referințe la proiectele Catalog.

Frontend-ul admin are feature-ul `purchasing`: pagină de compoziție, listă de furnizori, formular de furnizor, selector SKU, listă de articole și formular de asociere; fiecare componentă are TS/HTML/SCSS alăturate. Accesul API, starea, modelele și stilurile sunt separate. Testele sunt în Unit/Purchasing, Integration/Purchasing, Integration/Http și `frontend/tests/e2e/purchasing.spec.ts`.

Comenzile au tipuri separate în Domain/Orders și Contracts/Orders, câte un use case în Application/Features/Orders și endpoint-uri în Api/Endpoints/Administration/Orders. Frontend-ul `purchase-orders` separă pagina, lista, creare, observații, linii, adăugare/editare linie și acțiuni; API/starea/modelele sunt dedicate. Testele browser sunt în `purchase-orders.spec.ts`.

Recepțiile comenzilor au Domain/Receipts, Contracts/Receipts, handler-e distincte pentru înregistrare și reluare, porturi `IPurchaseReceiptRepository` și `IInventoryReceipts`, repository EF și client HTTP în API. Componenta `order-receipts` compune formularul, situația cantităților și istoricul; testele sunt separate în `PurchaseReceiptPersistenceTests`, `InventoryReceiptsClientTests` și `purchase-receipts.spec.ts`.

Inventory păstrează ajustările în Domain/Adjustments, handler-e separate AdjustStock/GetStockLedger, contracte în Contracts/Movements și portul IStockLedger. Configurația EF și repository-ul sunt separate. Frontend-ul feature `inventory` conține panelul de compoziție, formularul de ajustare, lista mișcărilor, API, state și modele; pagina Catalog compune panelul fără acces API direct.

Corecțiile de recepție sunt grupate în `Domain/Corrections`, `Application/Features/Corrections` în Purchasing, respectiv `Features/CorrectReceipt` în Inventory. Fiecare operație are handler și endpoint proprii; repository-urile și configurațiile EF sunt locale serviciului. Admin compune `order-corrections` din pagina de aprovizionare, cu modele, API și store în directoarele feature-ului.

Coșul storefront este în `features/cart`, cu `models`, `data-access`, `state`, componente `add-to-cart`/`cart-line` și pagina `pages/cart`. Ordering organizează use case-ul în `Application/Features/QuoteCart`, portul consumatorului în `Abstractions/Catalog`, iar transportul HTTP în `Infrastructure/Catalog/Transport`, cu un DTO per fișier.

Persistența coșului: entitatea în Ordering.Domain/Carts, portul în Application/Abstractions/Persistence și câte un handler în Features/StoredCart. Infrastructure/Persistence separă contextul, configurația și repository-ul; migrațiile sunt locale serviciului. API/Endpoints/Cart are un fișier per rută; Security/CartOwnerCookie gestionează accesul anonim. Angular separă modelul StoredCart, CartPersistenceApi și CartStore.

Datele PF/PJ sunt în Ordering.Domain/Customers; contractele de răspuns în Contracts/Customers, use case-ul în Application/Features/StoredCart/SaveCustomer. În Angular, features/cart separă componentele customer-form și customer-address, factory-ul formularelor, modelele și accesul API/starea.

Rezervările: Inventory.Domain/Reservations, câte un handler în Application/Features/Reservations, portul IStockReservations și repository/configurație EF separate. Rutele sunt în Api/Endpoints/Internal/Reservations; worker-ul de expirare în Api/Workers. Nu există entități de rezervare partajate între servicii.

Coordonarea rezervărilor este în Ordering.Domain/Reservations și Application/Features/Reservations. Portul IReservationInventory aparține Ordering; Infrastructure/Reservations conține adaptorul și DTO-urile locale. Persistența, configurația EF, fiecare endpoint intern și worker-ul au fișiere separate.

Pregătirea coșului: Ordering.Domain/Preparation, Application/Features/Preparation cu handler separat pentru pregătire/citire/anulare, portul IPreparedCarts și repository/configurație EF proprii. Endpoint-urile sunt în Api/Endpoints/Preparation. Angular păstrează componenta preparation cu TS/HTML/SCSS colocate, PreparationApi în data-access, PreparationStore în state și modelele în models.

Consumarea rezervărilor: Inventory.Domain/Reservations/StockConsumption, handler și comandă în Application/Features/Reservations/ConsumeStock, endpoint intern separat și configurație EF proprie. Jurnalul existent este extins cu tipul Consumption. Testele PostgreSQL sunt în Integration/StockConsumptionTests; scenariul browser în stock-consumption.spec.ts.
