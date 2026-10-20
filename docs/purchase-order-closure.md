# Închiderea comenzilor de aprovizionare

În **Aprovizionare → Închiderea comenzii**, administratorul poate închide o comandă Trimisă după livrarea integrală, parțială sau fără livrare. Sunt afișate bucățile nelivrate în total și pe fiecare SKU. Restul este calculat din cantitatea comandată minus recepțiile confirmate plus corecțiile confirmate.

Un motiv de maximum 500 de caractere este obligatoriu dacă există rest nelivrat; pentru o comandă integral recepționată este opțional. Confirmarea explicită precizează că închiderea este definitivă. Nu se schimbă stocul, costurile, articolele sau documentele originale și nu se notifică furnizorul. Sunt salvate data închiderii, operatorul din sesiunea autentificată și motivul.

## Reguli și recuperare

- Ciornele și comenzile anulate nu pot fi închise. O comandă deja închisă nu poate fi închisă din nou, anulată, editată, recepționată sau corectată prin documente noi.
- Orice recepție sau corecție în așteptare blochează închiderea. Verifică rezultatul în Inventory și reia operația originală până la confirmare. Cantitățile afișate se pot modifica după confirmare.
- Corecțiile necesare se efectuează **înainte** de închidere. Nu există deocamdată redeschidere sau corecție post-închidere; pentru livrări ulterioare se creează o comandă nouă. Ajustările generale Inventory nu rescriu situația Purchasing.
- Reluarea idempotentă a unei recepții/corecții deja confirmate rămâne fără efect comercial, inclusiv după închidere.
- Dacă răspunsul browserului se pierde, reîncarcă comanda pentru a verifica starea și auditul. Repetarea închiderii produce 409, fără alte modificări.

## Persistență și API

`POST /api/v1/admin/purchasing/orders/{id}/close` primește `version` și `reason`. Cere sesiune Administrator și CSRF, inclusiv la apel direct pe Purchasing. Actorul nu este acceptat din corpul cererii. Răspunsul reușit este 204.

Operația folosește blocarea existentă furnizor → comandă și verifică versiunea în aceeași tranzacție cu validarea recepțiilor/corecțiilor și salvarea închiderii. Închiderea și o recepție concurentă nu pot reuși pe aceeași versiune. Nu apelează Inventory și nu introduce tranzacții distribuite.

Detaliul comenzii include `closedAt`, `closedBy`, `closureReason`. Starea `Closed` a fost adăugată după valorile existente ale enumului, păstrând semnificația datelor persistate. Filtrul de listă acceptă `status=Closed`, iar interfața afișează „Închise”. Cantitățile originale nu se reduc; restul nelivrat rămâne derivabil din documentele înghețate.

Migrația `PurchaseOrderClosure` adaugă exclusiv trei coloane în Purchasing. Comenzile existente nu sunt închise automat. Backupul local anterior migrării este în directorul privat `work/backups`; baza legacy nu este modificată.
