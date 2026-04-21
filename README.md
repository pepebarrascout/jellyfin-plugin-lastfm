# Jellyfin Last.fm Plugin
<div align="center">
    <p>
        <img alt="Logo" src="https://raw.githubusercontent.com/pepebarrascout/jellyfin-plugin-lastfm/main/logo.png" height="180"/><br />
        <a href="https://github.com/pepebarrascout/jellyfin-plugin-lastfm/releases"><img alt="Total GitHub Downloads" src="https://img.shields.io/github/downloads/pepebarrascout/jellyfin-plugin-lastfm/total?color=c3002f&label=descargas"/></a>
        <a href="https://github.com/pepebarrascout/jellyfin-plugin-lastfm/issues"><img alt="GitHub Issues" src="https://img.shields.io/github/issues/pepebarrascout/jellyfin-plugin-lastfm?color=c3002f"/></a>
        <a href="https://jellyfin.org/"><img alt="Jellyfin Version" src="https://img.shields.io/badge/Jellyfin-10.11.x-blue.svg"/></a>
        <a href="https://www.last.fm/"><img alt="Last.fm" src="https://img.shields.io/badge/Last.fm-c3002f?logo=lastdotfm&logoColor=white"/></a>
    </p>
</div>

> **Scrobblea tu música a Last.fm** desde Jellyfin. Actualiza el estado de "Ahora Reproduciendo", envía scrobbles automáticamente y gestiona tus canciones favoritas directamente desde cualquier cliente de Jellyfin.

**Requiere Jellyfin versión `10.11.0` o superior.**

---

## ✨ Características

| Característica | Descripción |
|---|---|
| 🎵 **Scrobbling Automático** | Las canciones se scrobblean al alcanzar el porcentaje configurado o 4 minutos, lo que ocurra primero |
| 📡 **Ahora Reproduciendo** | Actualiza tu perfil de Last.fm con lo que estás escuchando en tiempo real |
| ❤️ **Love / Ban Canciones** | Marca canciones como favoritas o prohibidas en Last.fm a través de la API del plugin |
| 💖 **Auto-Love** | Marca automáticamente como favoritas las canciones que tienes como favoritas en Jellyfin |
| ⚙️ **Configurable** | Ajusta el porcentaje de scrobble, duración mínima y origen del artista |
| 👥 **Multi-Usuario** | Soporta múltiples sesiones de Jellyfin simultáneamente |

---

## 📋 Clientes Probados

El plugin ha sido probado y funciona correctamente en los siguientes clientes de Jellyfin:

| Cliente | Plataforma | Estado |
|---|---|---|
| 🌐 **Jellyfin Web** | Interfaz web nativa | ✅ Funcional |
| 📱 **Jellyfin para Android** | App oficial de Jellyfin | ✅ Funcional |
| 🖥️ **[Feishin](https://github.com/jeffvli/feishin)** | Escritorio (AppImage Linux) | ✅ Funcional |
| 🎵 **[Finamp](https://github.com/UnicornsOnLSD/finamp)** | Android (versión Beta) | ✅ Funcional |

> **Próximamente** se probará en otros clientes de Jellyfin. Si has probado el plugin en un cliente que no aparece en la lista, por favor envía tu reporte de uso abriendo un [Issue](https://github.com/pepebarrascout/jellyfin-plugin-lastfm/issues) para que podamos actualizar esta tabla.

---

## 🚀 Instalación

### Método 1: Desde el Catálogo de Plugins de Jellyfin (vía Manifest)

Esta es la forma más sencilla de instalar el plugin. Solo necesitas agregar el manifest de este repositorio como fuente de plugins en tu servidor Jellyfin.

1. En tu servidor Jellyfin, navega a **Panel de Control > Plugins > Repositorios**
2. Haz clic en el botón **+** (agregar repositorio)
3. Ingresa los siguientes datos:
   - **Nombre**: `Last.fm Plugin`
   - **URL del Manifest**:
   - ```https://raw.githubusercontent.com/pepebarrascout/jellyfin-plugin-lastfm/main/manifest.json```
4. Haz clic en **Guardar**
5. Navega a la pestaña **Catálogo**
6. Busca **Last.fm** en la lista de plugins disponibles
7. Haz clic en **Instalar**
8. Reinicia Jellyfin cuando se te solicite

> **Nota**: Cada vez que se publique una nueva versión en este repositorio, Jellyfin la detectará automáticamente y te ofrecerá actualizar.

### Método 2: Instalación Manual

1. Descarga la última versión desde [Releases](../../releases)
2. Descomprime el archivo ZIP
3. Copia todos los archivos `.dll` a la carpeta de plugins de tu servidor Jellyfin:
   - **Linux**: `~/.config/jellyfin/plugins/`
   - **Windows**: `%LocalAppData%\Jellyfin\plugins\`
   - **macOS**: `~/.local/share/jellyfin/plugins/`
   - **Docker**: Monta un volumen en `/config/plugins` dentro del contenedor
4. Reinicia Jellyfin

---

## ⚙️ Configuración

### Paso 1: Obtener credenciales de la API de Last.fm

Necesitas una [cuenta de API de Last.fm](https://www.last.fm/api/account/create) para usar este plugin:

1. Ve a [last.fm/api/account/create](https://www.last.fm/api/account/create) e inicia sesión
2. Completa los detalles de la aplicación (cualquier nombre y descripción funcionan)
3. Copia tu **API Key** y **Shared Secret** — las necesitarás más adelante

### Paso 2: Configurar el plugin en Jellyfin

1. Navega a **Panel de Control > Plugins > Last.fm**
2. Ingresa tu **API Key** y **Shared Secret** en la sección de credenciales
3. Haz clic en **Save Changes** para guardar las credenciales
4. En la sección **Authentication**, haz clic en **Connect to Last.fm**
5. Se abrirá una página de Last.fm solicitando autorización. Haz clic en **Yes, allow access**
6. Regresa a Jellyfin y haz clic en **I Already Approved**
7. Si todo sale bien, verás el mensaje "Successfully connected" y tu nombre de usuario

### Opciones de Configuración

| Opción | Predeterminado | Descripción |
|---|---|---|
| Enable Scrobbling | Sí | Activa/desactiva el scrobbling a Last.fm |
| Enable Now Playing Notifications | Sí | Activa/desactiva las notificaciones de ahora reproduciendo |
| Scrobble after | 25% | Porcentaje de la canción a reproducir antes de scrobblear (máx. 4 minutos) |
| Minimum track duration | 25s | Duración mínima en segundos para que una canción sea elegible para scrobble |
| Auto-love liked tracks | Sí | Marca automáticamente como favoritas las canciones que tienes como favoritas en Jellyfin |
| Use Album Artist for scrobbling | No | Usa el artista del álbum en lugar del artista de la pista para el scrobble |

---

## 🔧 Solución de Problemas

### Los scrobbles no aparecen en Last.fm

- Verifica que tu **API Key** y **Shared Secret** sean correctos
- Asegúrate de haber completado el proceso de autorización (debes ver "Connected as [usuario]")
- Verifica que las canciones duren al menos 25 segundos (configuración predeterminada)
- Confirma que hayas alcanzado el umbral de scrobble (25% de reproducción o 4 minutos)
- Revisa los registros (logs) de Jellyfin buscando mensajes del plugin Last.fm

### El plugin no aparece en el Dashboard

- Asegúrate de estar usando Jellyfin 10.11.x o superior
- Reinicia Jellyfin después de instalar el plugin
- Verifica que los archivos `.dll` estén en la carpeta correcta de plugins

### La configuración no se guarda

- Haz clic en **Save Changes** después de ingresar tus credenciales ANTES de intentar conectar
- Si usas Jellyfin en Docker, asegúrate de que el directorio de configuración esté montado como volumen persistente

### Problemas de conexión con Last.fm

- Asegúrate de que `https://ws.audioscrobbler.com` sea accesible desde tu servidor Jellyfin
- Si usas un proxy o firewall, verifica que permita conexiones salientes a Last.fm
- Intenta reconectarte haciendo clic en **Disconnect** y luego en **Connect to Last.fm** nuevamente

---

## 🛠️ Compilación desde el Código Fuente

### Requisitos

- .NET SDK 9.0 o superior
- Fuentes de NuGet configuradas:
  - `https://api.nuget.org/v3/index.json`
  - `https://nuget.jellyfin.org/v3/index.json`

### Comandos de Compilación

```bash
# Restaurar dependencias
dotnet restore

# Compilar en modo Debug
dotnet build

# Compilar en modo Release
dotnet build -c Release

# Publicar artefactos
dotnet publish -c Release -o artifacts
```

---

## 💡 Recomendación

Si te interesa crear **listas inteligentes y dinámicas** en Jellyfin (playlists y colecciones basadas en reglas que se actualizan automáticamente), te recomiendo probar el plugin **[Jellyfin SmartLists](https://github.com/jyourstone/jellyfin-smartlists-plugin/)**:

- Crea playlists automáticas basadas en género, artista, calificación, fecha, estado de reproducción y mucho más
- Interfaz web moderna para gestionar tus listas
- Funciona con todos los tipos de medios (películas, series, música, etc.)
- Se actualiza automáticamente cuando tu biblioteca cambia

<a href="https://github.com/jyourstone/jellyfin-smartlists-plugin/">
    <img alt="SmartLists Plugin" src="https://img.shields.io/badge/SmartLists-Plugin-6c5ce7?logo=github&logoColor=white&style=for-the-badge"/>
</a>

---

## 💬 Soporte y Contribuciones

- **Reportes de bugs y sugerencias**: Usa la sección de [Issues](https://github.com/pepebarrascout/jellyfin-plugin-lastfm/issues) para reportar problemas o proponer nuevas funciones
- **Contribuciones**: Las contribuciones son bienvenidas. No dudes en enviar un Pull Request
- **Reportes de uso**: Si has probado el plugin en un cliente de Jellyfin que no aparece en la lista de [Clientes Probados](#-clientes-probados), por favor compártelo

---

## ⚠️ Disclaimer

Este proyecto se proporciona tal cual (as-is) sin garantías de ningún tipo. El autor no se hace responsable de cualquier daño, pérdida de datos o problema derivado del uso de este plugin. Last.fm y sus respectivos logotipos son marcas registradas propiedad de Last.fm Ltd. Este plugin no está afiliado con, respaldado por, ni patrocinado por Last.fm Ltd.

---

## 📄 Licencia

Este proyecto está bajo la Licencia MIT — consulta el archivo [LICENSE](LICENSE) para más detalles.
