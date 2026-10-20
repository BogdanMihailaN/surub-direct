# Ajustări de stoc și jurnalul mișcărilor

În administrarea unui produs, secțiunea **Stoc și mișcări** permite alegerea SKU-ului și afișează soldul, cantitatea rezervată, disponibilul și istoricul paginat. Sunt incluse atât recepțiile manuale și cele aplicate de Purchasing, cât și ajustările și corecțiile de recepție. Datele provin din Inventory; nu se fabrică un sold zero dacă Inventory sau Catalog nu poate fi consultat. Un SKU existent fără sold are zero bucăți.

## Ajustare

Introdu diferența în bucăți (pozitivă sau negativă), documentul și motivul, apoi confirmă verificarea cantității. Diferența trebuie să fie între -10.000.000 și +10.000.000, diferită de zero. Documentul are maximum 100 caractere și motivul 500. Operatorul este preluat din sesiunea autentificată, nu din corpul cererii.

Serverul verifică soldul și rezervările pe care le-ai văzut. Dacă s-au modificat între timp, primești 409 și trebuie să reîncarci datele. Nu poate rezulta un stoc negativ sau mai mic decât rezervările existente; depășirea limitei numerice este respinsă. În etapa actuală nu există încă flux comercial de rezervare, dar regula protejează soldurile rezervate.

Ajustarea este definitivă și nu editează recepții ori alte operații. O corecție ulterioară se înregistrează ca o operație nouă cu motiv explicit. Ajustările pot fi făcute și pentru SKU-uri ale produselor arhivate. Ele nu schimbă prețuri, stări de publicare sau cantitățile recepționate din comenzile Purchasing. Pentru o greșeală într-o recepție Purchasing folosește [corecția legată de original](receipt-corrections.md). Retururile sunt o etapă ulterioară.

## Erori și idempotency

Clientul generează o cheie UUID și păstrează corpul ajustării cât timp rezultatul este incert. **Reia ajustarea** retrimite aceeași comandă. Cheia identică și payloadul identic nu modifică soldul de două ori, chiar dacă prima încercare a reușit și răspunsul s-a pierdut. Schimbarea payloadului pentru aceeași cheie produce 409.

După o eroare de validare sau conflict, reîncarcă soldul și verifică istoricul înainte de o comandă nouă. Cheia pending din interfață nu persistă după închiderea/reîncărcarea întregii pagini sau schimbarea produsului; în aceste cazuri verifică jurnalul înainte de a reintroduce ajustarea. Referința documentului nu este cheie de deduplicare: un document poate justifica mai multe ajustări distincte.

## Persistență și concurență

Inventory adaugă jurnalul ajustării, blochează rândul soldului, verifică valorile așteptate și actualizează soldul în aceeași tranzacție. Inserarea jurnalului cu `ON CONFLICT` serializează cererile cu aceeași cheie; rollback-ul elimină și înregistrarea dacă ajustarea nu poate fi aplicată. Actualizările recepțiilor existente folosesc aceeași blocare PostgreSQL a soldului, evitând pierderea actualizărilor.

Jurnalul unește tabelele Receipts, Adjustments și ReceiptCorrections fără a modifica datele istorice. Pagina și soldul sunt citite într-o tranzacție Repeatable Read. Sunt 25 de mișcări pe pagină, ordonate după data înregistrării, tip și ID. Data este cea a înregistrării, nu o garanție a ordinii de commit pentru operații concurente. Nu se inventează solduri istorice după fiecare mișcare sau operații pentru eventuale solduri migrate fără jurnal.

## API

Ambele rute cer rol Administrator, inclusiv la apel direct; POST validează și CSRF prin Identity. Gateway-ul le rutează explicit la Inventory.

- `GET /api/v1/admin/variants/{id}/movements?page=1` — sold, rezervări, mișcări și paginare.
- `POST /api/v1/admin/variants/{id}/adjustments` — `requestId`, `quantity`, `expectedOnHand`, `expectedReserved`, `reference`, `reason`; răspuns `created` adevărat pentru aplicare nouă, fals pentru retry identic.

Migrarea `StockAdjustments` adaugă doar tabela Adjustments și indexurile sale în baza Inventory. Soldurile și recepțiile existente sunt păstrate. Nu sunt introduse acces interservicii la baze sau rute `/internal` prin gateway.
