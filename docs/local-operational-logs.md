# Jurnal operațional comun pentru dezvoltare

La pornirea cu `python3 scripts/dev.py up`, supervisor-ul citește ieșirea serviciilor și colectează evenimentele operaționale permise din Inventory și Ordering în `work/operations/events.jsonl`. Consola serviciilor rămâne disponibilă; cele două servicii folosesc formatter-ul JSON inclus în .NET, cu timestamp UTC și fără scopes.

Nu este un serviciu nou de business și nu adaugă acces între baze. Nu există endpoint public, cont extern, agent cloud sau notificări trimise.

## Consultare

```sh
python3 scripts/dev.py operations
python3 scripts/dev.py operations --service ordering --limit 20
python3 scripts/dev.py operations --service inventory --limit 50
```

Comanda citește și când aplicațiile sunt oprite. Afișează JSONL, implicit ultimele 100 de evenimente, cu limită 1–1000. Fișierele rotate sunt citite de la cel mai vechi la cel curent; liniile JSON incomplete sunt ignorate. Ordinea este cea a colectării, nu o ordine globală garantată a evenimentelor produse de servicii.

## Date păstrate

- version, timestamp (sursa, UTC), receivedAt (colector, UTC), service, eventId și level;
- pentru 7100/7101/7102: alertCode și alertState, numai codurile și tranzițiile cunoscute;
- pentru 7200/7300/7400: dryRun, candidates, removed, cu tipuri și intervale validate;
- pentru 7201/7301/7401: errorType dintr-o listă de tipuri cunoscute, altfel OtherException.

Sursa este dată de procesul pornit de supervisor. Sunt acceptate doar combinațiile cunoscute de serviciu/categorie/EventId/nivel. Mesajul formatat, exception, scopes, payload-uri, motivele reluărilor, operatorii, SQL și câmpurile suplimentare nu sunt persistate de colector. Liniile mai mari de 64 KiB și intrările nevalide sunt ignorate. Jurnalele generale ale aplicației continuă separat în consolă și nu fac parte din acest filtru.

## Rotație și operare

Fișierul curent este rotit la aproximativ 5 MiB, cu maximum trei copii: circa 20 MiB în total. Directorul are permisiuni 0700 și fișierele create au 0600. Scrierile concurente ale serviciilor sunt serializate de handler-ul de logare. Un lock de fișier permite un singur colector pentru acest director; a doua instanță nu scrie peste rotația primei. Implementarea locală folosește flock pe macOS/Linux, la fel ca gestionarea proceselor POSIX a scriptului existent. `work/` este exclus din Git.

Rotația limitează spațiul, nu vechimea: evenimente vechi pot rămâne dacă volumul este mic, iar cele mai vechi copii sunt înlocuite când limita este atinsă. Jurnalul nu este audit comercial, backup sau arhivă permanentă.

Dacă inițializarea fișierului eșuează, supervisor-ul anunță lipsa colectării și continuă pornirea; reporniți supervisor-ul după remediere. Dacă scrierea eșuează ulterior, se afișează o avertizare fără detalii sensibile; citirea ieșirii serviciilor continuă. Următorul eveniment salvat cu succes marchează recuperarea. Evenimentele pierdute nu sunt reîncercate sau păstrate într-o coadă durabilă.

Colectarea este activă exclusiv pe durata `dev.py up`. Pornirea individuală a unui serviciu păstrează JSON în consolă, fără acest fișier comun. Oprirea supervisor-ului închide procesele și drenează firele de citire înainte de închiderea colectorului.

## Verificări și limite

Testele Python sunt în `backend/tests/SurubDirect.Tooling.Tests/Unit` și se execută și prin `python3 scripts/dev.py test`, după testele .NET. Pot fi rulate separat:

```sh
python3 -m unittest discover -s backend/tests/SurubDirect.Tooling.Tests/Unit -v
```

Această etapă centralizează local alertele mesageriei, rezultatele retenției și curățarea coșurilor expirate. Nu colectează toate logurile și nu oferă dashboard web, tracing, metrici, alerte externe sau colectare de pe mai multe calculatoare. Producția necesită colector dedicat, acces controlat, politică de stocare și monitorizare independentă de procesul aplicației.
