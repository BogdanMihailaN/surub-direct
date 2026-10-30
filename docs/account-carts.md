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

## Interfața Angular — 30 octombrie 2026

Pagina `/cont` afișează coșul contului după verificarea sesiunii. Permite creare, reîncărcare, schimbarea cantităților, eliminarea articolelor și consultarea prețurilor estimate prin recalcularea publică Ordering. Datele clientului copiate pot fi consultate și șterse separat. Reînceperea după expirare cere confirmare explicită.

Transferul afișează e-mailul contului destinație și selecția browserului înainte de confirmare. Copierea datelor clientului/adreselor este debifată implicit. Renunțarea nu trimite nicio mutație. Lipsa unei destinații create, un coș de cont nevid, o pregătire sau o eroare de citire blochează transferul.

După orice mutație sunt recitite ambele coșuri. Un răspuns pierdut sau un conflict nu repetă automat salvarea: interfața afișează explicația și starea recitită. Transferul invalidează și coșul anonim din antet și notifică celelalte taburi. Notificările localStorage conțin doar un semnal aleator, fără produse, identificatori de cont sau date personale. Dacă stocarea browserului este restricționată, verificarea versiunilor pe server rămâne autoritativă.

Sesiunea se reverifică la focus, iar componentele cu date de cont sunt eliminate la reverificare/deconectare. Răspunsurile dintr-o instanță distrusă sau invalidate de alt tab nu pot restaura datele afișate. Fiecare cerere de cont trimite `expectedAccountId`. Erorile celor două coșuri sunt independente și nu sunt afișate ca selecții goale.

## Limite și continuare

Produsele adăugate din catalog și pagina `/cos` folosesc în continuare coșul anonim. Transferul în cont este explicit și disponibil numai către un coș gol; îmbinarea selecțiilor și adăugarea directă din catalog în coșul contului nu sunt implementate. Formularul de editare a datelor PF/PJ rămâne în coșul anonim; în cont există consultarea și ștergerea ciornei copiate.

Pregătirea/confirmarea comenzilor din coșul contului rămân de implementat; interfața anunță această limită și nu expune acțiuni inexistente. Nu există sincronizare push/WebSocket; actualizările se recitesc prin API la revenirea în pagină sau prin „Reîncarcă coșurile”, iar conflictele dintre dispozitive se detectează prin versiune.

Checkout-ul comercial rămâne dezactivat. Datele sunt demonstrative, iar `noindex` rămâne activ.
