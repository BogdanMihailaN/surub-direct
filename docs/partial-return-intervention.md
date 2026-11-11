# Intervenția la recepții parțiale neconfirmate

În administrarea comenzilor, cererile aflate în restituire cu cel puțin trei încercări nereușite afișează o alertă. Aceeași alertă apare lângă recepție în lista filtrată după „Restituire stoc în curs”. Alerta se stinge când Inventory confirmă recepția. Nu trimite e-mailuri sau notificări externe.

După o încercare nereușită, administratorul poate solicita „Reîncearcă recepția”, cu motiv de 3–500 de caractere și confirmare explicită. Acțiunea programează imediat aceeași intenție pentru worker. Păstrează identificatorul cererii folosit drept cheie Inventory, cantitățile, confirmarea inițială și numărul încercărilor. Nu marchează recepția ca finalizată și nu modifică direct stocul.

API-ul POST `/api/v1/admin/sales-orders/{id}/partial-returns/{requestId}/retry-receipt` primește `operationId`, `expectedVersion` și `reason`. Este disponibil în Development și cere sesiune Administrator plus CSRF, inclusiv prin acces direct la Ordering. Răspunsurile sunt no-store. Cererile pentru altă comandă răspund 404, iar versiunile depășite și stările neeligibile răspund 409.

Tranzacția blochează cererea prin același row lock ca workerul și salvează programarea și auditul împreună. Fiecare intervenție are identificator, versiune, motiv, operator și dată. Replay-ul exact recuperează operația deja salvată inclusiv după confirmarea recepției, fără reprogramare. Un alt conținut cu aceeași cheie este refuzat. Este permisă cel mult o intervenție pe minut și cel mult 100 pe cerere; reîncercările automate continuă independent.

Interfața recitește detaliul după răspuns pierdut, fără retransmitere automată. Auditul este exclusiv administrativ. Migrarea aditivă `PartialReturnReceiptRetries` creează tabela de istoric fără a modifica înregistrările existente.

Operatorul verifică disponibilitatea Inventory și configurația conexiunii înainte de intervenție. Erorile persistente cer investigarea cauzei. Nu se creează o recepție nouă pentru a ocoli o recepție neconfirmată, deoarece Inventory ar fi putut salva deja stocul înainte de pierderea răspunsului.
