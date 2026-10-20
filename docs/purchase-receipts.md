# Recepții parțiale ale comenzilor

În pagina **Aprovizionare**, o comandă Trimisă afișează situația pe fiecare SKU: bucăți comandate, confirmate în stoc, în așteptare și rămase disponibile pentru recepție. Cantitatea se introduce în **bucăți**, inclusiv când livrarea conține o parte dintr-un ambalaj. Conversia cantității comandate folosește ambalajul păstrat în comandă, fără a reciti condițiile curente ale furnizorului.

## Înregistrare și reluare

Alege linia, cantitatea fizic primită și documentul de recepție. Limita unei recepții este 10.000.000 bucăți și nu poate depăși cantitatea rămasă. Referința documentului este obligatorie, maximum 100 de caractere, normalizată la majuscule. Același document poate acoperi mai multe linii, dar se înregistrează o singură dată pe aceeași linie; pentru o livrare ulterioară se folosește documentul ei distinct.

Recepțiile sunt definitive: nu se editează, șterg sau anulează. O comandă cu orice recepție, inclusiv în așteptare, nu mai poate fi anulată. Corecțiile și retururile necesită un flux ulterior de ajustare; nu înregistra o cantitate fictivă pentru a le compensa. Furnizorul poate deveni inactiv după trimitere fără a împiedica recepția mărfii deja comandate.

Dacă apare o eroare, reîncarcă documentul. O recepție **În așteptare** poate fi deja aplicată în Inventory, dar fără confirmare în Purchasing. Folosește **Reia recepția**; nu înregistra din nou livrarea. Reluarea folosește datele persistate, fără a cere reintroducerea cantității. Cantitățile în așteptare ocupă deja restul de recepționat.

## Consistența dintre servicii

1. Purchasing blochează furnizorul, apoi comanda, verifică versiunea și cantitățile, apoi salvează recepția în propria bază. Cheia UUID vine de la client; aceeași cheie și aceleași date returnează recepția existentă chiar dacă versiunea inițială este veche. Refolosirea cheii cu alte date produce 409. Un index unic pe linie/document împiedică dublarea cu o cheie nouă.
2. După commit, Purchasing apelează endpoint-ul administrativ existent din Inventory cu aceeași cheie UUID, varianta, bucățile și referința. Inventory deduplică cheia și actualizează jurnalul și soldul într-o singură tranzacție. Nu există acces la baza altui serviciu.
3. După confirmarea răspunsului, Purchasing salvează momentul confirmării. Dacă cererea sau răspunsul se pierde ori acest ultim commit eșuează, înregistrarea rămâne în așteptare și se poate relua. Retry-ul confirmă și un răspuns Inventory `created: false` pentru o recepție deja aplicată.

Nu există tranzacție distribuită. Recepțiile paralele nu pot depăși comanda deoarece verificarea și rezervarea cantității sunt serializate în PostgreSQL; sunt incluse atât recepțiile confirmate, cât și cele în așteptare. O recepție rezervată nu este eliminată automat după timeout. Comanda rămâne Trimisă, iar situația cantităților indică recepția parțială sau completă; nu există încă închidere contabilă.

## Autorizare și API

Ambele operații cer Administrator și CSRF, inclusiv direct pe portul Purchasing:

- `POST /api/v1/admin/purchasing/orders/{id}/receipts`: `requestId`, `version`, `lineId`, `quantity`, `reference`.
- `POST /api/v1/admin/purchasing/orders/{id}/receipts/{receiptId}/retry`: fără date comerciale în corp; folosește recepția existentă.

Detaliul comenzii include `receipts` cu cantitatea, referința, operatorul inițial, data înregistrării și `appliedAt` (null până la confirmare). Confirmarea crește versiunea comenzii; reluarea unei recepții deja confirmate nu o schimbă.

Purchasing transmite cookie-ul sesiunii curente și tokenul CSRF către endpoint-ul administrativ Inventory, care le validează din nou prin Identity. Clientul HTTP nu stochează cookie-uri și nu urmărește redirectări; timeout de 5 secunde. Nu există endpoint intern nou sau rută gateway nouă. `Services:Inventory` configurează adresa de destinație; în producție aceasta trebuie să fie o adresă internă de încredere, cu TLS. Operatorul Purchasing rămâne cel inițial; Inventory înregistrează utilizatorul care a efectuat prima aplicare, inclusiv dacă aplicarea are loc la reluare.

## Limite

Reluarea este manuală și cere sesiune validă. Nu există încă worker, retry automat, RabbitMQ sau compensări pentru acest flux. Toate răspunsurile Inventory neconfirmate păstrează starea în așteptare; un conflict de cheie sau o limită permanentă a soldului necesită investigarea jurnalelor, nu o recepție nouă. Costul mediu al stocului, TVA-ul, facturile de achiziție, retururile și ajustările nu sunt implementate. Recepțiile manuale existente în Inventory rămân disponibile și nu sunt retroactiv asociate comenzilor.
