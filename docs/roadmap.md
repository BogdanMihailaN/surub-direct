# Plan de implementare și automatizare

Automatizarea din acest chat este **oprită (PAUSED)** la cererea utilizatorului. Lucrul continuă manual, prin mesaje în chat. Configurația de reluare la 30 de minute este păstrată, dar nu rulează. Utilizatorul a autorizat explicit implementarea întregului backlog, cu teste, documentație, commit și push după fiecare etapă. Dacă utilizatorul o reactivează, condiția de finalizare convenită este implementarea și verificarea întregului proiect; nu se reactivează implicit la continuarea manuală. Activarea comercială și publicarea în producție necesită acord separat.

## Funcționalități finalizate

- [x] Servicii independente .NET cu Clean Architecture și gateway YARP; baze/credențiale separate.
- [x] Catalog, variante, ambalaje, prețuri, taxonomie, imagini, import CSV și administrare.
- [x] Identity: sesiune Administrator și CSRF, inclusiv direct pe servicii.
- [x] Furnizori, condiții SKU, comenzi de aprovizionare cu snapshot-uri și versiuni.
- [x] Recepții parțiale cu jurnal persistent, retry manual și deduplicare Inventory.

## Etape restante

- [x] Ajustări de stoc și jurnal al mișcărilor — implementate și verificate.
- [x] Corecții legate de recepțiile originale, coerente între Purchasing și Inventory.
- [x] Închiderea comenzilor de aprovizionare, inclusiv cantitatea rămasă nelivrată.
- [x] Coș demonstrativ local, cu recalculare server a prețurilor, ambalajelor și stocului.
- [x] Persistența coșului anonim în Ordering, cookie protejat, CSRF și controlul versiunilor.
- [ ] Date PF/PJ și flux de comandă; asociere cu contul, politici de retenție și curățarea coșurilor expirate.
- [ ] Rezervări atomice, expirări, anulări și compensări.
- [ ] RabbitMQ, Outbox/Inbox, deduplicare, retry și dead-letter pentru fluxurile distribuite.
- [ ] Integrări de plăți, curier și facturare, cu verificări în mediile de test.
- [ ] Retururi și cerințele comerciale convenite pentru lansare.
- [ ] Validarea completă a fluxurilor comerciale, securității și operării; documentarea recuperării și backupurilor.

## Informații care trebuie furnizate sau confirmate

Furnizorii și datele comerciale reale, prețurile/TVA-ul, procesatorul de plăți, curierul, soluția de facturare, conturile/credențialele și condițiile de lansare. Nu se inventează aceste date și nu se declară proiectul terminat cât există cerințe restante ori blocaje. Se continuă lucrările independente și se cer deciziile necesare când devin relevante.

Progresul detaliat este în `implementation-status.md`, iar dovezile verificărilor în `verification.md`. Datele demonstrative și `noindex` rămân active până la îndeplinirea condițiilor de lansare și autorizarea separată a utilizatorului.
