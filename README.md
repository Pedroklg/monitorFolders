# Como Utilizar
Baixe o Executável

Acesse o repositório MonitorFolders e navegue até o diretório MonitorarPasta. Baixe o arquivo executável self-contained disponível lá.

Execute o Programa

Abra um terminal ou prompt de comando e navegue até o diretório onde o executável foi baixado, que deve ser MonitorarPasta.

Comando de Execução

Execute o programa com o seguinte comando:

FileSystemMonitor.exe <diretório1> <diretório2> ... <caminhoDoArquivoDeLog>
Onde:

<diretório1>, <diretório2>, etc., são os diretórios que você deseja monitorar.
<caminhoDoArquivoDeLog> é o caminho completo para o arquivo onde as alterações serão registradas.
Exemplo:

Para monitorar as pastas C:\Pasta1 e C:\Pasta2 e gravar o log em C:\Logs\log.json, use:

FileSystemMonitor.exe "C:\Pasta1" "C:\Pasta2" "C:\Logs\log.json"
Parar a Monitoria

Para parar a monitoria, pressione Enter no terminal onde o programa está em execução. O programa salvará os logs restantes e encerrará.

Se quiser mover as diferenças para um outro diretório, acesse https://github.com/Pedroklg/MigrateChanges
