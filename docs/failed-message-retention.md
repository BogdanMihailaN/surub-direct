# Retenția mesajelor eșuate procesate ulterior

Ordering poate elimina corpul unei înregistrări eșuate numai după reluare, dovadă de procesare în Inbox și perioada de păstrare. Rândul, auditul, fingerprint-ul, hash-ul și cheile de deduplicare rămân în bază. Implicit rulează în simulare.

## Condiții cumulative

- Contract valid (CanReplay), cerere de reluare identificată prin ReplayRequestId și RequestedAt.
- Publicare confirmată de cel puțin RetentionDays; și CapturedAt mai vechi decât pragul.
- Inbox conține același EventId și același PayloadHash, cu ReceivedAt mai vechi decât pragul.
- Corpul actual are în continuare același SHA-256 ca hash-ul salvat; nu a fost deja eliminat.

Confirmarea brokerului singură nu este suficientă. Un mesaj cu alt hash, fără Inbox, procesat recent, încă nepublicat, invalid sau nereușit rămâne păstrat. Filtrarea se face înainte de limitarea lotului, astfel încât primele mesaje neeligibile nu blochează curățarea celorlalte.

Inbox dovedește acceptarea evenimentului de către handler-ul Ordering. Pentru un plan necunoscut, acesta poate accepta evenimentul fără a crea sau modifica o comandă. Dovada nu înseamnă plată, livrare sau succes comercial.

## Configurație

Secțiunea `Messaging:FailedMessageRetention` din Ordering:

| Opțiune | Implicit | Interval |
| --- | --- | --- |
| Enabled | true | true/false |
| DryRun | true | true/false |
| RetentionDays | 30 | 30–3650 |
| BatchSize | 100 | 1–1000 |
| IntervalSeconds | 3600 | 60–86400 |

Worker-ul se înregistrează numai când mesageria este activată. Prima verificare rulează la pornire; fiecare ciclu are timeout cooperativ de 30 secunde și este urmat de pauza configurată. Parametrii numerici invalizi sunt refuzați. Simularea numără maximum un lot de candidați și nu modifică date. Hash-urile sunt verificate de PostgreSQL, fără transferul payload-urilor către proces pentru simulare.

Instalația locală păstrează DryRun=true. Activarea eliminării efective cere schimbarea explicită a setării, după evaluarea perioadei de investigare și a backupurilor. Pragul inițial este o alegere tehnică, nu o politică legală.

## Audit, concurență și relivrări

Lotul real folosește tranzacție și `FOR UPDATE OF f SKIP LOCKED`. Se golește numai Payload și se salvează PayloadRemovedAt. Rămân EventId, PayloadHash, OriginalSize, ID/tip, fingerprint-ul original, generația anterioară, operatorul, motivul și momentele reluării. Un eșec înainte de commit nu elimină parțial lotul; dacă confirmarea commitului se pierde, repetarea ignoră marcajele deja salvate.

Capturarea repetată a aceleiași livrări găsește fingerprint-ul păstrat și nu reintroduce corpul. Repetarea aceleiași cereri administrative păstrează auditul inițial; o cerere diferită produce conflict. Publisher-ul ia numai înregistrări nepublicate, astfel încât nu republică un corp eliminat. Inbox păstrează ID/hash și deduplică în continuare evenimentele întârziate.

Interfața arată data eliminării și explică dovada Inbox. Nu expune hash-ul sau corpul mesajului. Evenimentul de jurnal 7300 include DryRun/Candidates/Removed, iar 7301 raportează numai tipul erorii. Nu sunt trimise notificări externe.

## Migrare și limite

`ProcessedFailedMessageRetention` adaugă EventId, PayloadHash și PayloadRemovedAt, plus index filtrat și constrângere SQL. Hash-urile înregistrărilor existente sunt calculate din bytea original; EventId se completează numai pentru contracte marcate valide și identificatori acceptați de parser-ul UUID PostgreSQL. Formatele vechi neacceptate rămân excluse de la retenție. Capturile noi folosesc GUID-ul normalizat de parser-ul aplicației.

Migrarea calculează hash-uri pentru rândurile existente; în producție trebuie planificată în funcție de volumul registrului. Reversarea migrării nu restaurează corpurile eliminate. Backupurile au propriul ciclu de păstrare.

Inbox-ul, mesajele eșuate fără dovadă de rezolvare și metadatele de audit sunt păstrate pe termen nedefinit în această etapă. Nu există purjare generală, ștergere manuală în UI, arhivă externă sau clasificare de date personale. Constrângerea SQL protejează forma rândurilor curățate; dovada Inbox este verificată tranzacțional de serviciu. Nu eliminați separat înregistrări Inbox: politica se bazează pe deduplicarea lor permanentă.
