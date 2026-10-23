# Curățarea coșurilor anonime expirate

Coșurile anonime au ExpiresAt la 30 de zile de la creare. Modificarea produselor sau ciornei actualizează UpdatedAt și versiunea, fără a extinde această durată. Ordering verifică expirarea înaintea modificărilor și a pregătirii.

Worker-ul de curățare poate elimina coșurile expirate fără legături comerciale. Implicit rulează în simulare; instalarea locală nu șterge coșuri.

## Eligibilitate

Toate condițiile trebuie îndeplinite:

- ExpiresAt este mai vechi decât momentul verificării minus GraceDays.
- PreparedId este null.
- Nu există niciun PreparedCart cu acel CartId, inclusiv o referință istorică detașată.
- Nu există nicio DemoOrder cu acel CartId, indiferent de starea comenzii.

Coșurile active, cele aflate încă în perioada de grație și cele legate de pregătiri/comenzi sunt excluse. Nu se încearcă rezolvarea automată a pregătirilor rămase pe coșuri expirate. Aceste cazuri rămân pentru un flux separat de reconciliere/operare.

Curățarea nu apelează Inventory și nu șterge planuri de rezervare, snapshot-uri, comenzi, jurnal de stoc sau Inbox. Politica privește numai rândurile eligibile din Carts, inclusiv selecțiile de produse, ciorna clientului și hash-ul proprietarului din acel rând.

## Configurație și execuție

Secțiunea `CartCleanup` din Ordering, independentă de activarea RabbitMQ:

| Opțiune | Implicit | Interval |
| --- | --- | --- |
| Enabled | true | true/false |
| DryRun | true | true/false |
| GraceDays | 7 | 1–3650 |
| BatchSize | 100 | 1–1000 |
| IntervalSeconds | 3600 | 60–86400 |

De exemplu, `CartCleanup__DryRun=true` păstrează simularea. În configurația implicită, eligibilitatea începe după aproximativ 37 de zile de la crearea coșului, dacă nu există asocieri. Nu este o politică legală completă de păstrare a datelor.

Prima verificare rulează la pornire. După fiecare ciclu se așteaptă intervalul configurat; nu se suprapun cicluri în același proces. Timeout-ul cooperativ este de 30 secunde. Eșecurile se reîncearcă în ciclul următor, iar oprirea gazdei nu este raportată ca incident.

Simularea selectează maximum un lot de identificatori și raportează numărul lor, fără a încărca ciornele clientului sau a modifica rânduri. Numărul de candidați nu reprezintă totalul acumulat.

Execuția reală selectează și blochează identificatorii prin `FOR UPDATE OF c SKIP LOCKED`, apoi șterge numai acele rânduri în aceeași tranzacție. Actualizările și pregătirea coșului folosesc deja blocarea rândului; rândurile blocate sunt sărite și reevaluate într-un ciclu ulterior. Operațiile normale nu pot atașa o pregătire unui coș expirat. Loturile concurente nu șterg același coș de două ori.

## Comportament după curățare

Citirea cu un cookie care indică un coș eliminat întoarce lipsa coșului (204), conform contractului existent. O scriere cu vechiul identificator nu recreează datele și este refuzată. Endpoint-ul de creare poate emite o sesiune nouă cu token și coș noi. În mod normal cookie-ul de proprietar a expirat deja odată cu coșul.

Nu se păstrează o copie a ciornei șterse sau o listă de date personale în jurnal. Copiile din backup nu sunt eliminate de acest worker și au nevoie de o politică distinctă.

## Observabilitate și migrare

Evenimentul 7400 (ExpiredCartCleanupBatch) include DryRun, Candidates și Removed; 7401 raportează tipul erorii. Niciun ID de coș, owner hash sau conținut al ciornei nu este logat. Evenimentele sunt acceptate în jurnalul operațional local, consultabil cu:

```sh
python3 scripts/dev.py operations --service ordering --limit 20
```

Migrarea CartCleanupOrderIndex adaugă un index pe DemoOrders.CartId pentru verificarea asocierilor. Carts.ExpiresAt și PreparedCarts.CartId au deja indexuri. Nu schimbă rânduri existente sau reguli de stoc.

Activarea eliminării efective cere schimbarea explicită a DryRun în false după verificarea simulării și a backupului. Conturile de client, asocierea comenzilor cu conturi și retenția datelor comerciale sunt etape distincte.
