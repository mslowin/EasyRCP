# EasyRCP

TODO: sprawdzenie po IP czy jest ktoś w pracy na miejscu, czy zdalnie <br />
TODO2: trzeba będzie chyba jednak rozdzielić ten jeden form na (hidden) MainForm i CredentialsForm -> <br />
CredentialsForm będzie odpalane jako dialog i będzie służyć tylko do wpisywania emailu i hasła, a <br />
MainForm będzie jako Application Run i to będzie wiecznie ukryte okno, które będzie miało w tray'u wszystkie przyciski <br />
TODO3: Pozbierać wszystkie try catche, żeby rzucały wyjątki do Program.cs <br />

# Automatyczna aktualizacja aplikacji przez GitHub Releases

Aplikacja automatycznie sprawdza dostępność nowej wersji na podstawie najnowszego releasu w repozytorium GitHub. 
Kolejne wydania aplikacji są tworzone i oznaczane tagami przez narzędzie **release-please**,  
a wersjonowanie zarządzane jest przez narzędzie **MinVer**.

Mechanizm aktualizacji:

- Pobierany jest tag najnowszego release'u z GitHuba i porównywany z aktualną wersją aplikacji zarządzaną przez MinVer.
- Jeśli dostępna jest nowsza wersja, aplikacja pobiera nowy plik `EasyRCP.exe` pod nazwą `EasyRCP_new.exe`.
- Tworzy się i uruchamiany skrypt `update.bat`, który zatrzymuje działającą aplikację, usuwa stary plik, zamienia nowy na oryginalny i uruchamia aplikację ponownie.
- Skrypt jest usuwany po wykonaniu, aby nie pozostawiać niepotrzebnych plików.