# Plan de implementare și automatizare

Automatizarea din acest chat este **oprită (PAUSED)** la cererea utilizatorului. Lucrul continuă manual, prin mesaje în chat. Configurația de reluare la 30 de minute este păstrată, dar nu rulează. Utilizatorul a autorizat explicit implementarea întregului backlog, cu teste, documentație, commit și push după fiecare etapă. Dacă utilizatorul o reactivează, condiția de finalizare convenită este implementarea și verificarea întregului proiect; nu se reactivează implicit la continuarea manuală. Activarea comercială și publicarea în producție necesită acord separat.

## Funcționalități finalizate

- [x] Servicii independente .NET cu Clean Architecture și gateway YARP; baze/credențiale separate.
- [x] Catalog, variante, ambalaje, prețuri, taxonomie, imagini, import CSV și administrare.
- [x] Identity: sesiune Administrator și CSRF, inclusiv direct pe servicii.
- [x] API de înregistrare a clienților în Development, fără privilegii administrative, cu validare și protecție CSRF.
- [x] Interfață Angular pentru cont: înregistrare, autentificare, sesiune și deconectare, inclusiv mobil.
- [ ] Confirmarea e-mailului, recuperarea parolei și asocierea sigură a coșului/comenzilor.
- [x] Furnizori, condiții SKU, comenzi de aprovizionare cu snapshot-uri și versiuni.
- [x] Recepții parțiale cu jurnal persistent, retry manual și deduplicare Inventory.

## Etape restante

- [x] Ajustări de stoc și jurnal al mișcărilor — implementate și verificate.
- [x] Corecții legate de recepțiile originale, coerente între Purchasing și Inventory.
- [x] Închiderea comenzilor de aprovizionare, inclusiv cantitatea rămasă nelivrată.
- [x] Coș demonstrativ local, cu recalculare server a prețurilor, ambalajelor și stocului.
- [x] Persistența coșului anonim în Ordering, cookie protejat, CSRF și controlul versiunilor.
- [x] Date PF/PJ și ciornă persistentă atașată coșului, cu livrare/facturare și rezumat.
- [x] Curățarea coșurilor anonime expirate fără legături comerciale, cu simulare implicită și perioadă de grație.
- [ ] Plasarea comercială a comenzii, asociere cu contul și politici pentru coșuri expirate legate de pregătiri/comenzi.
- [x] Inventory: rezervări atomice, expirări și eliberare/anulare idempotentă.
- [x] Coordonator intern Ordering cu intenții persistente, retry, reconciliere și anulări.
- [x] Snapshot demonstrativ al coșului și generarea atomică a planului din ciorna clientului, cu prețuri revalidate.
- [x] Inventory: consumare atomică/idempotentă și jurnal de stoc; compatibilitate Ordering pentru starea Consumed.
- [x] Comandă demonstrativă, număr unic, intenție persistentă de confirmare/consum și istoric pentru proprietarul coșului.
- [x] Consultarea comenzilor de vânzare demonstrative în administrare: listă, filtre, paginare și detalii protejate.
- [x] Anulare administrativă integrală demonstrativă și restituire idempotentă după confirmare.
- [ ] Activarea comercială și fluxuri de operare/retur după plată și expediere.
- [x] RabbitMQ, Outbox/Inbox, deduplicare, retry și dead-letter pentru rezultatele Inventory → Ordering.
- [x] Monitorizare și reluare controlată a mesajelor eșuate, cu audit și publicare persistentă.
- [x] Avertizări operaționale în panoul Mesagerie, cu praguri, severitate și recomandări de intervenție.
- [x] Monitorizare în fundal și evenimente structurate pentru apariție, recuperare și persistența problemelor.
- [x] Retenție pentru payload-urile Outbox publicate, cu simulare implicită și păstrarea cheilor de deduplicare/audit.
- [x] Retenție pentru payload-urile eșuate reluate și confirmate în Inbox, cu simulare implicită și audit păstrat.
- [x] Colectare locală comună pentru alertele mesageriei și retenție, cu rotație și filtrare de metadate.
- [ ] Colector de producție/notificări externe, politici pentru Inbox/erori nerezolvate și extinderea brokerului la celelalte fluxuri.
- [ ] Integrări de plăți, curier și facturare, cu verificări în mediile de test.
- [ ] Retururi și cerințele comerciale convenite pentru lansare.
- [ ] Validarea completă a fluxurilor comerciale, securității și operării; documentarea recuperării și backupurilor.

## Informații care trebuie furnizate sau confirmate

Furnizorii și datele comerciale reale, prețurile/TVA-ul, procesatorul de plăți, curierul, soluția de facturare, conturile/credențialele și condițiile de lansare. Nu se inventează aceste date și nu se declară proiectul terminat cât există cerințe restante ori blocaje. Se continuă lucrările independente și se cer deciziile necesare când devin relevante.

Progresul detaliat este în `implementation-status.md`, iar dovezile verificărilor în `verification.md`. Datele demonstrative și `noindex` rămân active până la îndeplinirea condițiilor de lansare și autorizarea separată a utilizatorului.
