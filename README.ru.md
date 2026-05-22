[English version](README.md)

# Desktop MCP Control

Кроссплатформенная MCP панель управления, которая открывает безопасную локальную автоматизацию рабочего стола через Streamable HTTP. Внутри есть компактный glass UI, интеграция с треем, token authorization, скриншоты, управление мышью и клавиатурой, а также emergency stop.

![Desktop MCP Control preview](docs/assets/desktop-mcp-control-preview.png)

## Главное

- Компактная премиальная glass-панель для запуска, остановки и мониторинга MCP сервиса.
- Streamable HTTP endpoint по умолчанию: `http://127.0.0.1:45454/mcp`.
- Опциональная защита `Authorization: Bearer <token>` с копированием и регенерацией токена.
- Динамические настройки host, port и protocol без ручного перезапуска приложения.
- Кастомный трэй-попап для открытия панели, списка tools, настроек и выхода.
- Глобальная emergency hotkey: `Ctrl+Alt+Pause`.
- Desktop automation tools для скриншотов, мыши, клавиатуры, drag-and-drop и preview действий.
- Динамические platform capabilities через `desktop.get_capabilities`.

## Доступные tools

| Tool | Access | Описание |
| --- | --- | --- |
| `desktop.mouse_move` | write | Переместить мышь в координаты. |
| `desktop.mouse_click` | write | Кликнуть кнопкой мыши. |
| `desktop.mouse_scroll` | write | Выполнить скролл в целевой точке. |
| `desktop.keyboard_type` | write | Напечатать текст в активном окне. |
| `desktop.keyboard_hotkey` | write | Нажать сочетание клавиш. |
| `desktop.drag_drop` | write | Выполнить drag-and-drop между координатами. |
| `desktop.get_capabilities` | read | Вернуть capability flags для текущей платформы. |
| `desktop.capture` | read | Сделать скриншот display, window или region. |
| `desktop.predict_click` | read | Показать preview будущего клика на скриншоте. |
| `desktop.predict_swipe` | read | Показать preview будущего свайпа на скриншоте. |
| `desktop.emergency_stop` | write | Остановить текущие и будущие действия до reset. |

## Требования

- Windows 10/11 или Linux (X11 и Wayland в режиме best-effort).
- .NET SDK 10.0 или новее для разработки.
- MCP client с поддержкой Streamable HTTP.
- Для Linux X11 установи `xdotool` и `wmctrl`; для скриншотов установи `imagemagick` (`import`) или `grim`.

## Быстрый старт

```powershell
git clone https://github.com/yakoodev/desktop-mcp-control.git
cd desktop-mcp-control
dotnet restore DesktopMcp.slnx
dotnet build DesktopMcp.slnx
dotnet run --project DesktopMcp.App
```

Приложение автоматически запускает MCP runtime и показывает текущий endpoint в главном окне.

## Релизы

Каждый push в `main` собирает, тестирует, публикует приложение и обновляет GitHub Release для текущей версии из `Directory.Build.props`. Текущий release tag: `v1.0.0`.

Release assets:

- `desktop-mcp-control-v1.0.0-win-x64-portable.exe` - portable self-contained executable.
- `desktop-mcp-control-v1.0.0-win-x64-setup.exe` - WiX setup executable со Start Menu shortcut.
- `desktop-mcp-control-v1.0.0-linux-x64-portable.tar.gz` - Linux x64 portable archive.
- `SHA256SUMS.txt` - checksums для всех артефактов.

## Подключение MCP

Endpoint по умолчанию:

```text
http://127.0.0.1:45454/mcp
```

Пример общей конфигурации Streamable HTTP клиента:

```json
{
  "mcpServers": {
    "desktop-mcp-control": {
      "type": "streamable-http",
      "url": "http://127.0.0.1:45454/mcp"
    }
  }
}
```

Если включена авторизация токеном, добавь bearer header:

```json
{
  "headers": {
    "Authorization": "Bearer <token>"
  }
}
```

Форматы конфигурации у MCP клиентов могут отличаться, поэтому используй эквивалентные поля Streamable HTTP URL и headers в своем клиенте.

## Настройки

Пользовательские настройки хранятся локально здесь:

```text
Windows: %LocalAppData%\desktop-mcp-control\settings.json
Linux: ~/.local/share/desktop-mcp-control/settings.json
```

В окне настроек можно менять:

- Host, port и protocol.
- Режим авторизации: no auth или bearer token.
- Копирование, показ и регенерацию токена.

Network и authorization настройки применяются к работающему MCP runtime после сохранения.

## Безопасность

Приложение может управлять реальным рабочим столом. Держи endpoint на `127.0.0.1`, если тебе не нужен внешний сетевой доступ. Если биндишься на `0.0.0.0` или внешний интерфейс, включай token authorization и ограничивай доступ на уровне firewall.

Для немедленной остановки используй `Ctrl+Alt+Pause` или tool `desktop.emergency_stop`.

На Linux, особенно в Wayland-сессиях, часть функций автоматизации может быть недоступна из-за ограничений compositor/portal. Используй `desktop.get_capabilities` для проверки доступных действий и корректно обрабатывай ошибки `capability_unavailable`.

## Разработка

```powershell
dotnet build DesktopMcp.slnx
dotnet test DesktopMcp.slnx
```

Основные проекты:

- `DesktopMcp.App` - Avalonia UI, трэй, настройки и view models.
- `DesktopMcp.Mcp` - MCP runtime, authorization и tool definitions.
- `DesktopMcp.Core` - platform backends для Windows, Linux X11 и Linux Wayland (best-effort).
- `DesktopMcp.Tests` - unit tests для runtime, settings и view model поведения.

## Статус репозитория

Первый публичный релиз - `v1.0.0`. Проект теперь поддерживает Windows и Linux (X11 + Wayland best-effort capability model).


