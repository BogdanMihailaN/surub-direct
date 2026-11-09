# Simulator local de plăți

Disponibil exclusiv în Development, din detaliul administrativ al comenzilor confirmate. Decizie utilizator: simulare locală, fără procesator extern. Nu încasează bani, nu operează carduri și nu emite rambursări reale. Nu activează checkout-ul comercial.

## Flux

Stările sunt separate de confirmarea stocului și de pregătirea internă a comenzii:

- Neinițiată → În curs, prin inițiere explicită.
- În curs → Reușită sau Eșuată, prin alegerea explicită a rezultatului.
- Eșuată → În curs, cu o nouă încercare identificată separat.
- Reușită → Rambursată parțial la rambursarea produselor recepționate, dacă mai există sold.
- Reușită sau Rambursată parțial → Rambursată integral când totalul rambursat ajunge la valoarea plății. Aceasta încheie simularea pentru comandă.

Fiecare acțiune cere un motiv de 3–500 caractere și confirmarea comenzii/sumei în interfață. Suma și moneda sunt copiate exclusiv din comanda salvată, incluzând transportul; request-ul nu acceptă sume sau operatori furnizați de client. Simulatorul nu modifică produsele, prețurile, stocul sau starea pregătirii.

Anularea comenzii este blocată în stările În curs, Reușită și Rambursată parțial. Înainte de expediere, se poate anula după un rezultat eșuat sau după rambursarea integrală simulată; restituirea stocului rămâne operația distinctă existentă. Inițierea simultană cu anularea are un singur câștigător. După anulare nu mai sunt permise operații noi de plată.

## API, persistență și audit

`POST /api/v1/admin/sales-orders/{id}/payment-simulation` primește `operationId`, `expectedVersion`, `action` (Start/Succeed/Fail/Refund) și `reason`. Returnează 204. Ruta cere sesiune Administrator și CSRF Identity, inclusiv direct pe Ordering, și răspunde 404 în afara Development. Gateway expune explicit numai ruta administrativă. Detaliul comenzii include `paymentSimulation`; răspunsurile sunt no-store. Istoricul clientului nu include auditul intern al simulatorului.

Migrarea `PaymentSimulationLedger` adaugă PaymentSimulations și PaymentSimulationEvents în baza Ordering. Comenzile existente nu primesc retrospectiv plăți: sunt afișate ca Neinițiată, cu istoric gol. Prima operație validă creează înregistrarea și evenimentul în aceeași tranzacție. Tabela evenimentelor păstrează operația, încercarea, versiunea, acțiunea, stările inițială/finală, operatorul și data. Maximum 1000 de operații per simulare demonstrativă.

Scrierile folosesc aceeași blocare a planului ca anularea/reconcilierea. Replay-ul aceleiași chei, versiuni și acțiuni cu același motiv nu adaugă evenimente și nu înlocuiește operatorul inițial. Este permis și după anularea comenzii, exclusiv pentru a confirma rezultatul deja salvat. Cheile reutilizate cu alt conținut și versiunile depășite răspund 409. Un rezultat întârziat al unei încercări vechi nu poate confirma o încercare nouă.

Interfața recitește detaliul după succes, conflict sau răspuns pierdut, fără repetarea automată a mutației. La eșecul citirii, acțiunile vechi nu rămân disponibile. Schimbarea selecției ignoră erorile întârziate ale comenzii anterioare.

## Limite

Acesta este un instrument administrativ demonstrativ, nu un adaptor de procesator. Integrarea reală necesită separat alegerea furnizorului, autorizare/captură, webhook-uri autentificate, reconciliere persistentă, idempotency la furnizor, politica rezervărilor și validare sandbox. Rezultatele reale nu vor fi acceptate din butoanele simulatorului sau din redirectul browserului. Nu există chei externe, apeluri către procesatori, notificări sau activare în producție.

## Rezumatul clientului

Istoricul browserului și istoricul contului includ `paymentSummary` cu `status`, `amount`, `currency` și `refundedAmount`. Nu expun istoricul operațiilor, motive, operatori sau identificatorii încercărilor. Suma include transportul din comanda salvată. Lipsa unui registru de simulare este reprezentată explicit ca `NotStarted`; lipsa câmpului în interfață este afișată ca indisponibilitate, nu ca o stare presupusă.

Citirea paginii încarcă numai simulările comenzilor deja filtrate după proprietar, într-o singură interogare suplimentară și în aceeași tranzacție RepeatableRead. Asocierea la cont păstrează rezumatul și elimină accesul din istoricul anonim. Confirmarea repetată și răspunsul anulării returnează starea persistentă, fără a o reseta la Neinițiată. Rezumatul nu oferă operații de plată clientului; numai administratorul operează simulatorul local.

După expediere sau livrare, anularea cu restituire de stoc este blocată chiar după rambursare. Vezi [expedierea demonstrativă](demo-shipping.md).

Returul integral demonstrativ după livrare este acum disponibil prin fluxul separat descris în [demo-returns.md](demo-returns.md). Acesta nu deblochează anularea după expediere și nu rambursează automat plata.

## Rambursarea produselor recepționate

În detaliul comenzii, administratorul poate confirma o rambursare simulată pentru o cerere parțială ajunsă la „Stoc restituit”. Suma reprezintă totalul ambalajelor recepționate înmulțite cu prețurile cu TVA salvate la comandă. Transportul se poate deconta ulterior prin operația pentru soldul rămas.

Ordering primește operația la POST /api/v1/admin/sales-orders/{orderId}/partial-returns/{requestId}/refund. Solicitarea include identificatorul operației, versiunea plății afișate și motivul. Serviciul calculează suma, verifică plata și recepția și salvează totalul și evenimentul de audit în aceeași tranzacție. Același blocaj pe comandă serializează rambursările parțiale și integrale. O cheie unică limitează fiecare cerere de retur la o rambursare. Repetarea exactă a unei operații confirmate recuperează rezultatul.

Starea plății devine PartiallyRefunded cât timp există sold și Refunded la acoperirea întregii sume. Pentru plățile rambursate integral înainte de această migrare, istoricul continuă să afișeze suma integrală drept total rambursat. Clientul vede starea și totalul în istoricul comenzii. Administratorul vede și suma fiecărei operații noi în jurnalul simulării.

În lista Comenzi, filtrul „Stare retur parțial” → „Restituire stoc în curs” afișează pentru fiecare cerere în așteptare identificatorul, momentul primirii, numărul încercărilor și următoarea verificare programată. Detaliile provin din aceeași citire consistentă ca pagina de comenzi.
