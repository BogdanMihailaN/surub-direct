# Baza Inventory pentru retururi pe cantități

Etapă tehnică pentru retururi parțiale. Ordering are acum [recepție coordonată](partial-return-coordination.md) pentru cererile administrative parțiale. Formularele clientului/administratorului folosesc încă returul integral existent. Acest endpoint nu trebuie apelat pentru retururile gestionate deja de acel flux: cele două moduri sunt exclusiv alternative pentru aceeași rezervare.

## Contract intern

`POST /internal/reservations/{id}/return-receipts/{receiptId}` primește `orderId` și `lines: [{variantId, quantity}]`. Cantitățile sunt bucăți, nu pachete. Sunt acceptate 1–100 SKU-uri distincte, cantități strict pozitive și identificatori nenuli. Autentificarea utilizează cheia internă pentru rezervări; cheia de interogare a stocului nu este suficientă. Gateway-ul nu expune ruta. Răspunsul este no-store și conține recepția persistentă, cantitățile și momentul înregistrării.

ReceiptId este cheia globală de idempotență. Replay-ul cu aceeași rezervare, comandă și cantități întoarce aceeași recepție, inclusiv data; ordinea liniilor nu contează. Alt conținut răspunde 409. După răspuns pierdut, apelantul trebuie să retransmită aceeași cheie și aceleași cantități, niciodată o cheie nouă pentru aceeași recepție fizică.

## Invariante și persistență

Sunt necesare rezervarea Consumed, comanda originală și jurnalul consumului original pentru fiecare SKU. Totalul tuturor recepțiilor pe SKU nu poate depăși cantitatea consumată. Se pot înregistra recepții distincte până la această limită. StockBalance.OnHand crește doar cu cantitățile primite; Reserved rămâne neschimbat.

Tranzacția blochează mai întâi cheia recepției, apoi rezervarea prin același advisory lock folosit de consum/restituire integrală. Soldurile se modifică în ordinea SKU-urilor. La eroare, inclusiv depășirea capacității unui sold ulterior, toate modificările de sold și recepția sunt anulate. Două recepții concurente nu pot depăși consumul; două replay-uri nu dublează stocul.

După prima recepție pe cantități, restituirea integrală este refuzată pentru a evita dublarea. După restituirea integrală, recepțiile pe cantități sunt refuzate. Rezervarea rămâne Consumed chiar dacă totalul recepțiilor ajunge la consumul original; nu se publică evenimentul integral Restored. Astfel, Ordering nu poate interpreta o recepție parțială ca anulare ori retur integral finalizat.

Migrarea aditivă `PartialReturnReceipts` creează ReturnReceipts și ReturnReceiptLines, cu relație la rezervarea originală, cheie unică pe recepție/SKU și cantități pozitive. Jurnalul administrativ include mișcări `ReturnReceipt`, etichetate „Recepție retur”, cu numărul de bucăți și identificatorul comenzii. Documentele nu se modifică prin acest API.

## Următoarele etape

- Modelul cererilor și deciziilor administrative în Ordering este implementat în [etapa cererilor parțiale](partial-return-requests.md); formularele și accesul clientului rămân restante.
- Aprobarea, recepția explicită și retry/reconcilierea sunt implementate prin API-ul Ordering; interfețele rămân restante.
- Selecția produselor/cantităților în cont și administrare, rezumate și audit.
- Produse deteriorate/carantină și rambursări parțiale separate. Simulatorul actual rambursează numai integral; nu se calculează implicit o rambursare din cantitățile recepționate.

Nu activează un flux comercial, un formular de retur parțial sau rambursări reale.
