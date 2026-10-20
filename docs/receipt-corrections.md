# Corecții ale recepțiilor de aprovizionare

În **Aprovizionare → Corecții de recepție**, selectează o recepție confirmată, cantitatea introdusă în plus, documentul corecției și motivul. Confirmarea explicită scade stocul. Recepția inițială rămâne intactă; corecția apare separat în comandă și în istoricul Inventory. Pentru cantități lipsă se înregistrează o recepție nouă. Acesta nu este un flux de retur către furnizor sau de rectificare contabilă a facturilor.

Cantitatea este în bucăți, între 1 și 10.000.000, cel mult restul corectabil al recepției originale. Documentul are maximum 100 de caractere, normalizat la majuscule, iar motivul maximum 500. Un document poate fi înregistrat o singură dată pentru aceeași recepție. Corecțiile, inclusiv cele în așteptare, ocupă deja cantitatea corectabilă. Stocul nu poate coborî sub rezervări.

## Persistență și recuperare

1. Purchasing blochează furnizorul și comanda, verifică versiunea, recepția confirmată și suma corecțiilor, apoi salvează intenția imuabilă în propria bază.
2. După commit, transmite ID-ul corecției și ID-ul recepției originale către Inventory. Inventory blochează recepția originală, verifică SKU-ul, documentul și cantitatea cumulată, apoi blochează soldul. Jurnalul corecției și scăderea soldului sunt atomice; orice eroare anulează ambele modificări locale.
3. Purchasing marchează corecția confirmată după un răspuns Inventory valid. Doar atunci scade cantitatea netă recepționată și permite recepționarea din nou a diferenței.

Nu există tranzacție distribuită. În caz de eroare, reîncarcă comanda și verifică istoricul. **Reîncearcă corecția** folosește aceeași intenție persistentă, fără dublarea scăderii. Nu crea alt document pentru aceeași eroare. Dacă stocul este insuficient sau rezervat, investighează disponibilitatea înainte de reluare; nu adăuga stoc fictiv. Nu există anulare sau expirare automată a unei intenții în așteptare. Un conflict permanent necesită investigarea jurnalelor.

Aceeași cheie UUID și aceleași date sunt idempotente chiar cu versiunea inițială veche. Aceeași cheie cu alte date produce 409. O corecție confirmată nu poate fi editată sau ștearsă. Comenzile cu recepții nu devin anulabile după corectarea integrală.

## API și securitate

- `POST /api/v1/admin/purchasing/orders/{id}/corrections`: `requestId`, `version`, `receiptId`, `quantity`, `reference`, `reason`.
- `POST /api/v1/admin/purchasing/orders/{id}/corrections/{correctionId}/retry`: reluare din datele persistate.
- Detaliul comenzii include `corrections`, cu legătura la recepție și linie, operator, date și `appliedAt` nullable.
- Inventory: `POST /internal/receipt-corrections`, accesibil numai direct între servicii. Cere simultan cheia Inventory, sesiune Administrator și validare CSRF prin Identity. Gateway-ul nu expune ruta.

Purchasing primește `ServiceKeys:Inventory` prin configurația sa separată. Clientul HTTP transmite cookie-ul și CSRF ale cererii curente, fără cookie jar sau redirectări, cu timeout de 5 secunde. Endpoint-ul intern optează explicit pentru validarea sesiunii prin metadată tehnică; celelalte endpoint-uri interne nu își schimbă politica. Operatorul Purchasing este cel inițial; Inventory păstrează operatorul primei aplicări reușite, care poate fi cel ce reia operația.

Jurnalul Inventory include tipul `Correction`, cantitate negativă și `originalReceiptId`. Soldul și istoricul sunt citite consistent în aceeași tranzacție. Corecțiile se inițiază din Purchasing; ajustările generale nu modifică situația comenzilor.

Migrațiile additive `ReceiptCorrections` creează câte un jurnal local în Inventory și Purchasing, cu chei externe doar în baza proprietară. Nu modifică recepțiile existente. Backupurile locale sunt păstrate privat în `work/backups`; baza legacy rămâne intactă. Reluarea este manuală, cu sesiune validă; RabbitMQ și worker-ul de recuperare rămân în backlog.
