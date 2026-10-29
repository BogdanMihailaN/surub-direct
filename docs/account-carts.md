# Coșul contului — API demonstrativ

Ordering păstrează un coș separat pentru fiecare cont Identity. Coloana nullable `Carts.AccountId` și indexul unic sunt introduse de migrarea `AccountCarts`; coșurile anonime existente rămân neschimbate. Contul este identificat exclusiv din sesiunea validată de Identity, nu din corpul cererii.

## Endpoint-uri

Toate endpoint-urile sunt sub `/api/v1/ordering/account/cart`, disponibile numai în Development. Cer parametrul query obligatoriu `expectedAccountId`, care trebuie să coincidă cu identitatea autentificată. Acesta protejează confirmarea în cazul schimbării sesiunii; nu acordă acces unui cont destinație. Răspunsurile endpoint-urilor au `Cache-Control: no-store`.

| Metodă și sufix | Comportament |
| --- | --- |
| GET | Coșul contului; 204 dacă lipsește, 410 dacă a expirat. |
| POST `/session` | Creează un coș gol sau returnează coșul activ existent. Reîncepe explicit un coș expirat fără pregătire, golind selecția și ciorna și incrementând versiunea. |
| PUT | Înlocuiește selecția folosind `cartId`, `version`, `lines`. |
| PUT `/customer` | Salvează/șterge ciorna PF/PJ folosind `cartId`, `version`, `customer`. |
| POST `/import` | Transferă explicit coșul anonim în coșul gol al contului. |

Toate operațiile necesită sesiune autentificată. Mutațiile folosesc CSRF **Identity** (`X-CSRF-TOKEN`), inclusiv la acces direct pe Ordering. `X-CART-CSRF` rămâne numai pentru endpoint-urile anonime. Rolul Administrator nu este necesar. Lipsa sesiunii produce 401; CSRF invalid produce 400; contul așteptat diferit produce 409.

Nu se emite un cookie de proprietar pentru coșul contului. Identificatorul intern aleator folosit în stocare nu apare în DTO-uri; repository-ul anonim exclude coșurile de cont și la citire, și la editare. O altă sesiune autentificată în același cont poate citi și modifica selecția fără cookie-ul coșului anonim. ID-ul public al coșului nu acordă acces.

## Transferul din browser

Corpul conține `cartId`, `version` pentru sursă, `targetCartId`, `targetVersion` pentru destinație și `includeCustomer`. Sursa este identificată prin cookie-ul anonim original; destinația prin sesiunea Identity. Ambele coșuri trebuie să existe și să fie active.

Într-o singură tranzacție, Ordering blochează sursa, apoi destinația, verifică identificatorii/versiunile și mută selecția. Datele PF/PJ sunt copiate numai dacă `includeCustomer` este true. La final, sursa este golită inclusiv de datele clientului, iar versiunile ambelor coșuri cresc. Istoricul comenzilor deja finalizate rămâne pe sursa originală și se asociază contului prin fluxul separat de comenzi.

Transferul refuză o sursă goală/pregătită, o destinație nevidă/pregătită sau versiuni învechite (409). Nu există îmbinare implicită sau suprascriere a selecției unui alt dispozitiv. Două conturi care încearcă simultan transferul aceleiași surse nu pot câștiga ambele. Dacă răspunsul s-a pierdut, clientul recitește ambele coșuri; repetarea aceleiași comenzi după transfer produce 409 și nu dublează produsele.

## Concurență și expirare

Crearea inițială este serializată printr-un advisory lock tranzacțional pe cont, suplimentar indexului unic. Editările sunt serializate pe rând și verifică versiunea, astfel încât două dispozitive nu se suprascriu. Expirarea este de 30 de zile, fără prelungire implicită la citire. Reînceperea unui coș expirat este explicită; o pregătire activă o blochează. Curățarea periodică existentă poate elimina coșurile expirate după perioada de grație numai dacă nu au pregătiri sau comenzi.

## Limite și continuare

Această etapă implementează **API-ul**, nu schimbă magazinul Angular. Pagina `/cos` folosește în continuare coșul anonim. Urmează interfața de alegere/transfer explicit, recitirea pe dispozitive și conectarea pregătirii/confirmării comenzilor la coșul contului, cu păstrarea izolării după logout. Nu sunt expuse încă operații de pregătire pentru coșul contului. Nu există sincronizare push/WebSocket; actualizările se recitesc prin API și conflictele se detectează prin versiune.

Checkout-ul comercial rămâne dezactivat. Datele sunt demonstrative, iar `noindex` rămâne activ.
