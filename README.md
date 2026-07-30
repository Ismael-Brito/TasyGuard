# TasyGuard

## Sobre

O **TasyGuard** é um utilitário desenvolvido em **C# (.NET 8)** para impedir a abertura de múltiplas instâncias do **Tasy Native (Electron)**.

O aplicativo permanece em segundo plano monitorando a criação de novos processos do Windows e permite apenas a quantidade configurada de instâncias do Tasy.

Foi desenvolvido para ambientes corporativos e hospitalares, onde a abertura de múltiplas instâncias pode causar custos operacionais.

---

## Recursos

* Impede múltiplas instâncias do Tasy Native
* Compatível com aplicações Electron
* Monitora apenas processos principais
* Ignora processos internos (`renderer`, `gpu`, `utility`)
* Baixo consumo de CPU
* Ícone na bandeja do sistema
* Inicialização automática com o Windows
* Registro de eventos em arquivo de log
* Executável único (Single File)
* Publicação Self Contained (.NET 8)

---

## Tecnologias

* .NET 8
* Windows Forms
* WMI (Windows Management Instrumentation)
* Win32 API
* Registry
* C#

---

## Estrutura

```text
TasyGuard
│
├── Program.cs
├── ProcessWatcher.cs
├── TasyProcess.cs
├── WindowManager.cs
├── StartupManager.cs
├── Logger.cs
├── TrayIcon.cs
├── NativeMethods.cs
├── app.manifest
├── TasyGuard.csproj
└── Resources
    └── icon.ico
```

---

## Publicação

Executar:

```bash
dotnet publish -c Release
```

Ou pelo bat:

```text
publish.bat
```

O executável será criado em:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

---

## Configuração

O projeto pode ser configurado para permitir um número máximo de instâncias por aplicação através de um arquivo de configuração.

Exemplo:

```json
{
  "Applications": [
    {
      "Name": "TasyNative.exe",
      "MaxInstances": 1
    }
  ],
  "Update": {
      "Enabled": true,
      "VersionFilePath": "\\\\SERVIDOR\\SOFTWARE\\TasyGuard\\versao.txt"
  }
}
```

---

## Atualização

O projeto pode consultar um servidor de arquivos para verificar novas versões através de um arquivo de versão.

Exemplo:

```text
1.0.0
```

---

## Logs

Os logs são gravados em:

```text
%LOCALAPPDATA%\TasyGuard\TasyGuard.log
```

---

## Requisitos

* Windows 10 ou superior
* .NET 8 (apenas para desenvolvimento)
* Visual Studio Code ou Visual Studio 2022

---

## Licença

Este projeto é distribuído sob a licença MIT.

Sinta-se à vontade para utilizar, modificar e contribuir.
