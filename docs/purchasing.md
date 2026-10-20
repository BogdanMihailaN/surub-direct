# Furnizori și articole de aprovizionare

Pagina **Furnizori** din administrare gestionează firmele de la care cumperi și condițiile curente de achiziție pentru fiecare SKU. Serviciul Purchasing rulează independent pe portul 5105 și deține baza `surub_purchasing`, cu propriul utilizator. Catalog deține în continuare produsele și prețurile de vânzare, iar Inventory stocul.

## Furnizori

Se pot crea și edita denumirea firmei, țara, codul fiscal, persoana de contact, emailul, telefonul, adresa și observațiile. Țara este un cod din două litere (de exemplu RO). Codul fiscal este obligatoriu și unic în cadrul țării; spațiile sunt eliminate, literele normalizate la majuscule, iar prefixul RO este eliminat pentru România. Validarea este de format și unicitate locală; nu se verifică registrul fiscal sau statutul TVA.

Lista permite căutare după firmă, cod fiscal, SKU sau codul articolului la furnizor, filtrare după activ/inactiv și paginare cu 25 de furnizori. Dezactivarea păstrează datele și asocierile, dar blochează crearea și editarea articolelor până la reactivare. Nu există ștergere fizică.

## Asocierea unui SKU

După salvarea furnizorului, caută un SKU sau o denumire din Catalog și selectează varianta. Căutarea cere minimum două caractere și întoarce maximum 25 de rezultate; include ciorne și produse publicate, fără produse arhivate.

Completează:

- codul articolului la furnizor;
- denumirea ambalajului de achiziție;
- numărul întreg pozitiv de bucăți per ambalaj;
- costul net pentru **întregul ambalaj**, fără TVA, cu maximum 6 zecimale;
- moneda: RON, EUR, USD, GBP sau CHF.

Un SKU poate avea mai mulți furnizori, dar o singură asociere la fiecare furnizor. Codul articolului este unic în cadrul furnizorului, inclusiv pentru asocierile inactive. O asociere existentă se editează sau se reactivează; nu se dublează pentru un alt ambalaj. Această etapă păstrează o singură condiție curentă de achiziție per furnizor–SKU, fără istoric de costuri sau discounturi cantitative.

SKU-ul și ID-ul variantei rămân stabile la editarea asocierii. Denumirea variantei este un snapshot de la asociere și poate diferi ulterior de Catalog. Modificarea costului/ambalajului nu schimbă prețurile de vânzare sau stocul. Modificările asocierilor sunt serializate pe furnizor, iar unicitatea este protejată și în PostgreSQL.

## Integrare și API

Toate endpoint-urile `/api/v1/admin/purchasing/**` cer sesiune Administrator, inclusiv accesul direct pe portul 5105. POST/PUT cer validare CSRF prin Identity. Gateway-ul rutează aceste cereri spre Purchasing și nu expune rutele `/internal`.

| Metodă și rută, relativ la `/api/v1/admin/purchasing` | Operație |
|---|---|
| `GET /suppliers?search=&active=true&page=1` | Listă; `active` omis include toate stările |
| `GET /suppliers/{id}` | Datele furnizorului și articolele asociate |
| `POST /suppliers` | Creează furnizor activ |
| `PUT /suppliers/{id}` | Editează datele și starea activă |
| `GET /variants?search=...` | Caută variante prin API-ul Catalog |
| `POST /suppliers/{id}/items` | Asociază o variantă verificată în Catalog |
| `PUT /suppliers/{id}/items/{itemId}` | Editează condițiile și starea asocierii |

Purchasing definește propriul port `ICatalogVariants` și propriul DTO HTTP; nu referă proiectele Catalog și nu îi accesează baza. Clientul are timeout și cookie handling dezactivat. Lipsa unui SKU este distinctă de un Catalog indisponibil sau un răspuns invalid (503). Citirea furnizorilor și editarea condițiilor existente nu depind de Catalog. Asocierea nouă verifică starea Catalog la momentul cererii; arhivarea ulterioară a produsului nu șterge asocierea. Comenzile verifică din nou eligibilitatea la adăugarea liniei și la marcarea trimiterii. Vezi [comenzile de aprovizionare](purchase-orders.md).

## Pornire și limite

`python3 scripts/dev.py db` adaugă credențialul privat Purchasing dacă lipsește și creează baza separată. `python3 scripts/dev.py init` include migrarea Purchasing. `python3 scripts/dev.py up` îl pornește în grup, iar `python3 scripts/dev.py purchasing` separat. Pentru publicare, configurează `ConnectionStrings__Purchasing`, `ServiceKeys__Catalog`, `ServiceKeys__Identity`, adresele celor două servicii și destinația gateway-ului. OpenAPI este disponibil în Development la `/openapi/purchasing/v1.json` prin gateway.

Nu sunt implementate trimiterea automată către furnizori, recepțiile parțiale legate de comenzi, costul mediu al stocului, conversia valutară, importurile de furnizori sau auditul complet. Recepțiile de stoc existente rămân în Inventory. Nu sunt trimise emailuri și nu sunt activate cumpărături ori checkout prin această etapă.
