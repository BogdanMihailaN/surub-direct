# Retenția payload-urilor Outbox Inventory

Retenția implementată acoperă exclusiv corpurile evenimentelor publicate din `inventory.ReservationOutbox`. Înregistrările și cheile lor unice rămân în bază. Inbox-ul Ordering, registrul mesajelor eșuate, auditul reluărilor și jurnalele comerciale nu sunt șterse.

## Configurație și simulare

Worker-ul `OutboxRetentionWorker` este înregistrat în Inventory când mesageria este activată. Configurația se află în `Messaging:OutboxRetention`:

| Opțiune | Implicit | Valori acceptate |
| --- | --- | --- |
| Enabled | true | true/false |
| DryRun | true | true/false |
| RetentionDays | 30 | 30–3650 |
| BatchSize | 100 | 1–1000 |
| IntervalSeconds | 3600 | 60–86400 |

De exemplu, `Messaging__OutboxRetention__DryRun=true` menține simularea. Aceasta numără maximum un lot de candidați, fără actualizări sau încărcarea corpurilor. Numărul raportat nu reprezintă totalul restanței.

Primul ciclu rulează la pornire, apoi se așteaptă intervalul configurat după fiecare ciclu. Fiecare ciclu are timeout cooperativ de 30 secunde. Un lot necomis nu modifică parțial datele; după o eroare, worker-ul reîncearcă în ciclul următor. Dacă confirmarea commitului se pierde, marcajele deja salvate fac reluarea idempotentă. Oprirea gazdei nu este raportată ca eșec. Configurațiile numerice invalide sunt refuzate la pornire.

**Instalarea locală rămâne în simulare.** Activarea eliminării efective presupune schimbarea explicită a DryRun în false, după verificarea backupului și perioadei de investigare/recuperare dorite. Cele 30 de zile sunt un prag tehnic inițial pentru aceste mesaje, nu o politică legală de retenție.

## Eligibilitate și atomicitate

Un payload este eligibil numai dacă PublishedAt există, au trecut cel puțin RetentionDays de la confirmarea publicării și PayloadRemovedAt este null. Vârsta de la producerea evenimentului nu este suficientă. Un mesaj vechi nepublicat sau publicat recent nu este eliminat.

Execuția reală selectează maximum BatchSize înregistrări, ordonate după PublishedAt și Id, într-o tranzacție cu `FOR UPDATE SKIP LOCKED`. Replicile nu curăță același rând de două ori și nu așteaptă rânduri deja blocate. Pentru fiecare rând:

- SHA-256 al reprezentării UTF-8 citite din coloana JSONB este păstrat în PayloadHash. Este hash-ul reprezentării stocate, nu o promisiune de identitate cu orice serializare anterioară a JSON-ului.
- Payload este înlocuit cu obiectul JSON gol `{}`.
- PayloadRemovedAt păstrează momentul UTC al eliminării.

Se păstrează Id, ReservationId, Status, OccurredAt și PublishedAt. Indexul unic ReservationId/Status rămâne activ; retry-ul operației de stoc nu poate crea din nou același eveniment. Publisher-ul selectează exclusiv PublishedAt null, deci nu republică payload-uri eliminate. Constrângerea SQL refuză marcarea unui payload eliminat fără PublishedAt, fără hash sau fără obiectul gol.

Dacă tranzacția eșuează, hash-ul, marcajul și corpul se revin împreună. O nouă execuție ignoră înregistrările deja curățate. O confirmare de publicare pierdută lasă PublishedAt null, astfel încât retenția nu atinge mesajul.

## Observabilitate și limite

Evenimentul 7200 (OutboxRetentionBatch, Information) include DryRun, Candidates și Removed, fără identificatori sau payload-uri. Evenimentul 7201 (OutboxRetentionFailed, Warning) include numai tipul erorii. Configurația este raportată la pornire. Păstrați nivelul Information pentru categoria worker-ului dacă doriți rezultatele simulării.

PublishedAt confirmă acceptarea de către broker, nu procesarea de către consumator. După curățare, Outbox nu mai poate furniza corpul pentru o recuperare manuală după pierderea brokerului; perioada trebuie să acopere fereastra operațională de recuperare și backup. Această limită este motivul simulării implicite.

Inbox-ul păstrează ID/hash pentru deduplicarea evenimentelor întârziate. Registrul erorilor păstrează corpurile necesare investigării și reluării. Retenția acestora, arhivarea externă, centralizarea jurnalelor și notificările externe rămân etape distincte. Curățarea payload-ului nu elimină automat copii din backup și nu micșorează garantat fișierele bazei; PostgreSQL își recuperează spațiul conform mentenanței sale obișnuite.

## Migrare

`PublishedOutboxPayloadRetention` adaugă două coloane nullable, un index filtrat și o constrângere SQL în Inventory. Rândurile existente rămân intacte. Aplicați migrarea înainte de pornirea noii versiuni. Reversarea migrării nu poate restaura corpurile eliminate; pentru acestea este necesar un backup corespunzător.
