# Metin2 serverfiles API
A working but unfinished prototype of ASP.NET Minimal API that runs alongside metin2 server files on FreeBSD, connects to mariaDb session and exposes API endpoints for website and admin panel interactions.

* Account registration, login credentials validation.
* Sending GM commands to the server, such as notice, day/night change.
* Getting server statistics e.g. rankings, online status, players status.
* Item shop interactions, adding / spending dragon coins, awarding itemshop items.

You have to set up environment variables for it to work.

Server commands are sent through Tcp connection. <br>
MariaDb/Mysql edits are sent through MySqlConnector.

Most of this was done by reverse-engineering old publicly available metin2 php website files and server files from https://metin2.dev/
