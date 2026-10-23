# RabbitMQ: rezultate Inventory → Ordering

Prima integrare prin broker transportă rezultatele Consumed și Restored. Comenzile de rezervare, consum și restituire păstrează porturile HTTP și reconcilierea persistentă existente. Evenimentele pot finaliza o intenție locală când răspunsul HTTP s-a pierdut. Nu se activează checkout-ul comercial și nu se mută toate comunicările pe broker.

## Contract și granițe

Tip: `inventory.reservation-outcome.v1`. Payload JSON: EventId, ReservationId, OrderId, Status, ExpiresAt, OccurredAt și Lines (VariantId, Quantity). Nu conține client, adresă, prețuri sau credențiale. MessageId este EventId. Producătorul și consumatorul au DTO-uri locale, fără referințe între proiectele serviciilor.

Inventory scrie `inventory.ReservationOutbox` în aceeași tranzacție cu consumul/restituirea și jurnalul de stoc. Unicitatea ReservationId + Status împiedică evenimente repetate pentru aceeași tranziție. Evenimentele istorice de dinaintea migrării nu sunt fabricate retroactiv; reconcilierea HTTP rămâne valabilă.

Publisher-ul blochează câte un rând cu FOR UPDATE SKIP LOCKED și marchează PublishedAt numai după confirmarea brokerului. Folosește mesaje persistente, publicare mandatory și publisher confirms cu timeout. Lipsa unei rute, întreruperea conexiunii sau eșecul commitului local lasă mesajul republicabil. Publicarea și commitul PostgreSQL nu formează o tranzacție distribuită: livrarea este cel puțin o dată.

Ordering salvează `ordering.ReservationInbox` și efectul asupra planului/comenzii într-o singură tranzacție, înainte de ACK. ID-ul evenimentului este deduplicat cu lock și cheie primară; reutilizarea ID-ului cu alt corp este respinsă prin hash. Se validează versiunea, identitatea rezervării/comenzii și liniile originale.

Numai intențiile locale autorizate pot avansa: Confirming → Consumed/Confirmed, Restoring → Restored/Cancelled. Un Consumed întârziat nu inversează o anulare. Evenimentele nu creează comenzi și nu schimbă stocul; planurile necunoscute sunt înregistrate fără efect, pentru ceilalți apelanți legitimi Inventory. Evenimentele incompatibile cu starea locală nu o forțează; reconcilierea existentă rămâne sursa de recuperare.

## Topologie și recuperare

- Exchange direct, durabil: `inventory.reservations.v1`.
- Routing key: `reservation.outcome`.
- Coada quorum: `ordering.reservation-outcomes.v1`.
- Coada quorum de erori: `ordering.reservation-outcomes.v1.dead`, legată prin `reservation.outcome.dead`.

Consumer-ul procesează serial, cu ACK manual. La eroare temporară republică persistent cu antetul attempt, așteaptă confirmarea, apoi confirmă originalul. Maximum cinci încercări per lanț de livrare, cu întârzieri 2/4/8/16 secunde; apoi mesajul este publicat confirmat în coada de erori. Contractele invalide ajung direct acolo. Dacă publicarea de retry/dead-letter eșuează, originalul rămâne neconfirmat și va fi relivrat. Ferestrele de crash pot produce duplicate, absorbite de Inbox.

Nu se folosește dead-letter automat bazat pe TTL. Limita implicită de relivrări quorum este dezactivată (`x-delivery-limit=-1`) pentru a nu pierde mesaje la deconectări repetate; bugetul retry este controlat de aplicație. Reconectarea la nivel de worker se reia la cinci secunde. Un broker oprit nu pierde Outbox-ul și nu împiedică tranzacția de stoc. Un ACK pierdut după commit produce doar o relivrare deduplicată.

## Pornire locală

1. `python3 scripts/dev.py db` pregătește baza și secretele locale.
2. `python3 scripts/dev.py broker` generează secretele brokerului, pornește containerul, creează vhost-ul `surub-direct` și conturile distincte Inventory/Ordering. Comanda poate fi repetată.
3. Aplică migrările prin procedura locală cu backup înainte de actualizare; `python3 scripts/dev.py init` inițializează mediul de dezvoltare.
4. `python3 scripts/dev.py up` pornește aplicațiile.

Brokerul ascultă doar pe loopback (5672, administrare 15672), cu volum separat persistent. Configurația runtime folosește Messaging:Enabled, Host, Port, VirtualHost, Username și Password. Scripturile transmit fiecărui serviciu doar contul său; administratorul brokerului nu este transmis aplicațiilor. Inventory nu are drept de consum asupra cozilor. Ambele conturi au drepturile necesare declarării/bindării doar pentru topologia acestui flux.

Migrațiile sunt `ReservationOutcomeOutbox` și `ReservationOutcomeInbox`. Shared/Messaging conține numai transport, configurare și confirmări/retry, fără entități comerciale sau persistență. Catalog își păstrează Outbox-ul și jurnalul local; acesta nu publică încă în RabbitMQ.

## Operare și limite

Urmărește numărul/vârsta rândurilor ReservationOutbox cu PublishedAt null, coada principală, coada `.dead`, avertismentele worker-ului și conectivitatea brokerului. Nu goli cozile pentru a trata o eroare. Pentru un mesaj în `.dead`, remediază cauza și păstrează ID-ul, tipul și corpul când îl republici controlat, cu attempt resetat. Nu edita corpul unui ID deja procesat. Interfața dedicată de inspecție/reluare, alertele și politicile de retenție urmează.

Instanța locală este un singur nod, nu un cluster cu toleranță la pierderea gazdei. Sunt necesare TLS, rotație de credențiale, monitorizare, backup/restore și dimensionare înainte de producție. Polling-ul serial și tranzacția Outbox ținută pe durata confirmării sunt potrivite pentru volumul demonstrativ, nu validate prin teste de încărcare. RabbitMQ nu înlocuiește autorizarea intențiilor în Ordering.

Documentație tehnică: [clientul .NET RabbitMQ](https://www.rabbitmq.com/client-libraries/dotnet-api-guide), [confirmările de publicare](https://www.rabbitmq.com/tutorials/tutorial-seven-dotnet). Client folosit: RabbitMQ.Client 7.2.2.
