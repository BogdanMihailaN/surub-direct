# Administrarea retururilor parțiale în Angular

În pagina Comenzi, detaliul comenzii include secțiunea „Retururi parțiale demonstrative”. Crearea este disponibilă pentru comenzile livrate fără retur integral început, în limita de 100 de cereri. Nu reprezintă un flux comercial activ.

## Creare și decizie

Formularul afișează ofertele și ambalajele originale, cantitatea cumpărată și disponibilul pentru retur. Administratorul introduce numărul de ambalaje întregi; 0 exclude linia. Disponibilul scade cu cererile nerespinse, inclusiv cele recepționate. Sunt necesare cel puțin o cantitate validă, motiv de 3–500 caractere și confirmarea selecției. API-ul revalidează cantitățile, fără să se bazeze pe calculele interfeței.

Fiecare cerere afișează ambalajele, bucățile calculate de server, starea, motivul și auditul. Solicitările pot fi aprobate sau respinse, cu motiv și confirmare. Respingerea eliberează cantitățile pentru o cerere nouă. Returul integral nu este oferit cât există cereri parțiale active/recepționate.

## Recepție și recuperare

Cererea aprobată permite confirmarea recepției tuturor cantităților acelei cereri, cu confirmare separată că sunt primite și revandabile. Operația nu rambursează plata. Starea „Restituire stoc în curs” afișează încercările/eroarea generică și îndrumă operatorul să folosească Actualizează detaliile. După confirmarea Inventory apare data restituirii. Nu există polling automat în această versiune.

Mutațiile nu sunt retrimise automat după răspuns pierdut/conflict. Detaliul și lista cererilor sunt recitite; formularele se recreează din datele actuale. Dacă una dintre citiri eșuează, detaliul și formularele vechi sunt retrase, cu reîncercare manuală. Răspunsurile întârziate pentru o comandă selectată anterior nu înlocuiesc selecția curentă. Operațiile concurente din același detaliu sunt blocate pe durata mutației.

Filtrele listei de comenzi după starea returului se referă în continuare la returul integral; o coadă/filtrare dedicată cererilor parțiale rămâne restantă. Clientul nu are încă formular pentru retur parțial. Rambursările parțiale, produsele deteriorate și recepția în tranșe rămân în backlog.
