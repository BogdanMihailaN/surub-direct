# Comenzi de aprovizionare

Pagina **Aprovizionare** (`/aprovizionare`) pregătește comenzile către furnizori în serviciul Purchasing. O comandă are un furnizor, o monedă și un număr `CA-…` generat în PostgreSQL. Numerele sunt unice, dar pot avea goluri; nu sunt numere de documente fiscale.

## Flux administrativ

1. Caută un furnizor activ, alege moneda și creează ciorna.
2. Adaugă asocieri SKU active în moneda comenzii și cantitatea de ambalaje. Poți edita cantitatea și costul net negociat ori elimina linia cu confirmare.
3. După ce ai trimis comanda printr-un canal extern, confirmă acest lucru și marcheaz-o manual ca **Trimisă**. Aplicația nu trimite email și nu contactează furnizorul.
4. O comandă Ciornă sau Trimisă fără recepții poate fi marcată **Anulată**, cu motiv obligatoriu. Articolele și costurile se păstrează. Furnizorul trebuie notificat separat dacă este cazul.

Doar ciornele sunt editabile. Nu există redeschidere, ștergere a comenzilor sau schimbare de furnizor/monedă după creare. Lista are căutare după număr, furnizor, cod fiscal sau SKU, filtrare de stare și paginare cu 25 de comenzi.

## Date păstrate și validări

Datele firmei sunt copiate la crearea comenzii. SKU-ul, denumirea, codul furnizorului, ambalajul, bucățile per ambalaj și costul sunt copiate la adăugarea liniei. Modificările ulterioare ale furnizorului sau asocierii nu rescriu aceste date. Costul negociat în ciornă nu modifică asocierea furnizor–SKU.

Maximum 50 de linii, fără dublarea aceleiași asocieri. Cantitățile sunt întregi pozitive; totalul în bucăți al unei linii nu poate depăși `int.MaxValue`. Costul este pozitiv, maximum 1.000.000, cu cel mult 6 zecimale. Valoarea netă a liniei este limitată la 1.000.000.000. Totalurile liniilor se rotunjesc la două zecimale (jumătățile în sus), apoi se însumează. Nu includ TVA, transport sau alte taxe și nu se face conversie valutară.

Adăugarea și marcarea trimiterii verifică eligibilitatea variantei în Catalog. SKU absent/arhivat produce 409, iar dependența indisponibilă produce 503; tranzacția locală nu persistă modificarea. Furnizorul și asocierile trebuie să fie active la trimitere. Verificarea HTTP nu este o tranzacție distribuită: arhivarea ulterioară în Catalog nu șterge documentul Purchasing.

## Concurență și API

Toate operațiile sunt sub `/api/v1/admin/purchasing/orders`, cu sesiune Administrator și CSRF pentru modificări, inclusiv accesul direct la serviciu.

| Metodă / sufix | Operație |
|---|---|
| GET `/` | Listă: `search`, `status` (Draft/Sent/Cancelled), `page` |
| POST `/` | Creare: `supplierId`, `currency`, `notes` |
| GET `/{id}` | Detaliu și versiune |
| PUT `/{id}` | Observații: `version`, `notes` |
| POST `/{id}/lines` | Adăugare: `version`, `supplierItemId`, `packs` |
| PUT `/{id}/lines/{lineId}` | Editare: `version`, `packs`, `netCostPerPack` |
| POST `/{id}/lines/{lineId}/remove` | Eliminare: `version` |
| POST `/{id}/sent` | Înregistrare trimitere: `version` |
| POST `/{id}/cancel` | Anulare: `version`, `reason` |

Modificările blochează rândul furnizorului, apoi comanda, în aceeași tranzacție. Versiunea așteptată previne suprascrierea unei editări concurente; o versiune veche primește 409. Interfața cere reîncărcare după o modificare eșuată, pentru a verifica rezultatul. Crearea nu are cheie de idempotency; dacă răspunsul se pierde, verifică lista înainte de a crea din nou.

## Limite și etapa următoare

Pregătirea și trimiterea comenzilor nu schimbă stocul sau prețurile de vânzare. [Recepțiile parțiale](purchase-receipts.md) actualizează idempotent Inventory și împiedică suprarecepția. Exportul PDF, emailurile, corecțiile și auditul complet urmează separat. Migrarea `PurchaseOrders` adaugă doar tabelele comenzilor și liniilor în baza Purchasing; datele furnizorilor sunt păstrate.
