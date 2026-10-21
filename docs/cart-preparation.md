# Pregătirea demonstrativă a coșului

Disponibilă exclusiv în mediul ASP.NET Core Development, anunțată prin `preparationEnabled` în capabilities. `checkoutEnabled` rămâne false. Această etapă nu creează o comandă comercială și nu inițiază plăți. În alte medii, cele trei endpoint-uri de pregătire răspund 404.

## Protocol și proprietar

- `POST /api/v1/ordering/cart/preparation`: `{ requestId, cartId, version }`.
- `GET /api/v1/ordering/cart/preparation`: snapshot și starea planului; 204 când coșul nu are pregătire.
- `POST /api/v1/ordering/cart/preparation/{id}/cancel`: solicită anularea sau reia editarea după confirmarea stării terminale.

Toate folosesc proprietarul cookie-ului anonim HttpOnly, răspunsuri no-store și limita cart-storage. Scrierile validează `X-CART-CSRF` inclusiv direct pe API. Nu folosesc sesiunea administrativă. Endpoint-urile interne Inventory și Ordering rămân inaccesibile prin gateway.

Pregătirea cere produse și date PF/PJ salvate, un coș neexpirat și versiunea curentă. Ordering recitește produsele publicate și prețurile prin Catalog, validează ambalajele, minimul/pasul, moneda și disponibilitatea cumulată. Indisponibilitatea Catalog/Inventory nu este tratată ca stoc zero. Cantitățile se convertesc în bucăți și se cumulează per SKU, fără depășirea limitei int32.

## Persistență și concurență

Migrarea `PreparedCartSnapshots` adaugă `ordering.PreparedCarts` și referința opțională `Carts.PreparedId`. Snapshot-ul păstrează copia datelor clientului, denumirile, SKU-urile, ambalajele, cantitățile, prețurile net/brut, moneda și momentul verificării. Nu include taxa de transport. Nu există acces la baza altui serviciu.

Salvarea blochează rândul coșului și reverifică versiunea. Snapshot-ul, planul de rezervare și incrementarea versiunii coșului se salvează în aceeași tranzacție. Două pregătiri concurente nu creează două planuri pentru același coș. ID-ul cererii identifică snapshot-ul, planul și rezervarea. Cât pregătirea este atașată, repetarea aceleiași chei returnează datele salvate fără recalculare sau rezervare suplimentară. Reutilizarea cheii după detașare este respinsă; istoricul planului rămâne.

Apelurile HTTP către Inventory sunt făcute ulterior de [coordonatorul persistent](reservation-coordination.md). Verificarea Catalog nu este o tranzacție distribuită: stocul se poate schimba între verificare și rezervare. Inventory decide atomic dacă poate rezerva toate SKU-urile. Prețurile snapshot-ului nu se modifică atunci când Catalog este editat.

## Interfață și anulare

Pe durata pregătirii sunt blocate editarea articolelor și datelor clientului. Interfața afișează snapshot-ul și distinge Pending, Active, Cancelling, Released și Expired; citește starea la cinci secunde cât pagina este vizibilă și permite actualizarea manuală. Dacă citirea eșuează, starea anterioară este ascunsă și apare o eroare explicită.

„Anulează rezervarea” persistă intenția de anulare, fără deblocare prematură. După confirmarea Released/Expired, „Reia editarea coșului” detașează pregătirea, șterge copia snapshot-ului inclusiv datele personale, incrementează versiunea și permite recalcularea cu prețurile actuale. Ciorna clientului originală rămâne în coș și poate fi ștearsă din formular. Un răspuns pierdut blochează local editarea până la reîncărcare; reîncărcarea citește adevărul de pe server.

## Limite

Nu există comandă comercială, consumare fizică a stocului, plată, transport, factură sau RabbitMQ. Termenul coordonatorului este de 15 minute; expirarea Inventory și reconcilierea sunt descrise separat. Snapshot-urile abandonate și coșurile expirate necesită încă politici de retenție și curățare, inclusiv pentru backupuri. Datele sunt fictive, iar noindex rămâne activ.
