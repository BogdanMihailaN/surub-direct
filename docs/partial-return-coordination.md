# Recepția coordonată a returului parțial demonstrativ

Ordering primește confirmarea administrativă a tuturor cantităților dintr-o cerere parțială aprobată. Inventory deține soldurile și jurnalul recepțiilor. Produsele trebuie să fie primite și revandabile; lipsurile, deteriorările și recepția în tranșe a aceleiași cereri nu sunt încă suportate.

## Confirmare și stări

`POST /api/v1/admin/sales-orders/{orderId}/partial-returns/{requestId}/receive` primește operationId, expectedVersion, reason și resalableConfirmed. API-ul este disponibil numai în Development, cere Administrator și CSRF inclusiv direct pe Ordering și întoarce 204/no-store. Gateway-ul are o rută explicită, fără a expune endpointul intern Inventory.

Approved → Restoring salvează atomic confirmarea, motivul, operatorul și intenția de recepție. ExpectedVersion este versiunea aprobării (1); după confirmare devine 2. Restoring nu înseamnă succes: cererea devine Restored numai după confirmarea verificată a Inventory. Data InventoryReceivedAt provine din recepția persistentă a Inventory. Finalizarea tehnică nu schimbă versiunea administrativă.

Replay-ul exact al confirmării este acceptat înainte și după finalizare, fără o recepție nouă. Alt motiv/operator/versiune sau o operație nouă după confirmare răspunde 409. Replay-ul aprobării anterioare rămâne valid după recepție, fără revenirea la Approved. O confirmare fără resalableConfirmed este refuzată.

## Coordonare și recuperare

Worker-ul rulează numai în Development, la aproximativ 5 secunde, maximum 10 elemente pe iterație. Selectează intențiile scadente cu FOR UPDATE SKIP LOCKED. Fiecare apel HTTP are timeout de 5 secunde; nu există o tranzacție comună între servicii.

Ofertele din cerere sunt agregate pe VariantId, în bucăți. ID-ul comenzii identifică rezervarea originală, iar ID-ul cererii parțiale este cheia stabilă a recepției Inventory. Ordering folosește portul și DTO-urile HTTP proprii, fără referințe la proiectele sau baza Inventory.

Răspunsul trebuie să confirme exact receiptId, orderId, reservationId, cantitățile agregate și o dată de recepție nenulă. Orice eroare HTTP, răspuns nevalid sau indisponibilitate păstrează Restoring și programează reîncercarea cu aceeași cheie: 5 secunde per încercare, până la un interval maxim de 300 secunde. Numărul încercărilor, următoarea încercare și un mesaj generic sunt vizibile în API-ul administrativ; nu se persistă corpuri de răspuns sau detalii de excepție.

Dacă Inventory a salvat recepția, dar răspunsul s-a pierdut ori tranzacția locală Ordering nu s-a confirmat, replay-ul întoarce recepția existentă fără dublarea stocului. La oprirea worker-ului, anularea cererii de execuție lasă intenția persistentă pentru reluare.

Worker-ul blochează numai cererea pe care o procesează și nu ia ulterior lock pe comandă/plan. Confirmarea administrativă folosește ordinea existentă: cheia cererii, planul comenzii, cererea. Astfel, procesarea concurentă nu creează o ordine inversă de lock-uri.

## Limite și efecte

Rezervarea originală rămâne Consumed, comanda Confirmed și expedierea Delivered. Restored este starea acestei cereri parțiale, nu o restituire integrală a comenzii. Plata rămâne neschimbată; nu se generează rambursare. Cererea recepționată continuă să consume cantitățile disponibile pentru retur și blochează vechiul flux integral.

Nu se emite evenimentul RabbitMQ integral Restored pentru o recepție parțială; confirmarea se recuperează prin HTTP idempotent. Erorile permanente rămân în Restoring cu retry; un flux separat de intervenție/rezolvare și alertele dedicate sunt restante. Nu corecta intenția prin schimbarea cheii sau prin ajustări manuale de stoc.

Migrarea aditivă PartialReturnReceiptCoordination adaugă auditul recepției, rezultatul Inventory și câmpurile de retry. Interfața administrativă este disponibilă în [Angular](partial-return-admin-ui.md); clientul poate solicita cantități și urmări rezultatul în Contul meu.
