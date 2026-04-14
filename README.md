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

> **Scrobblea tu musica a Last.fm** desde Jellyfin. Actualiza el estado de "Ahora Reproduciendo", envia scrobbles automaticamente y gestiona tus canciones favoritas directamente desde cualquier cliente de Jellyfin.

**Requiere Jellyfin version `10.11.0` o superior.**

---

## ✨ Caracteristicas

| Caracteristica | Descripcion |
|---|---|
| 🎵 **Scrobbling Automatico** | Las canciones se scrobblean al alcanzar el porcentaje configurado o 4 minutos, lo que ocurra primero |
| 📡 **Ahora Reproduciendo** | Actualiza tu perfil de Last.fm con lo que estas escuchando en tiempo real |
| ❤️ **Love / Ban Canciones** | Marca canciones como favoritas o prohibidas en Last.fm a traves de la API del plugin |
| 💖 **Auto-Love** | Marca automaticamente como favoritas las canciones que tienes como favoritas en Jellyfin |
| ⚙️ **Configurable** | Ajusta el porcentaje de scrobble, duracion minima y origen del artista |
| 👥 **Multi-Usuario** | Soporta multiples sesiones de Jellyfin simultaneamente |

---

## 📋 Clientes Probados

El plugin ha sido probado y funciona correctamente en los siguientes clientes de Jellyfin:

| Cliente | Plataforma | Estado |
|---|---|---|
| 🌐 **Jellyfin Web** | Interfaz web nativa | ✅ Funcional |
| 📱 **Jellyfin para Android** | App oficial de Jellyfin | ✅ Funcional |
| 🖥️ **[Feishin](https://github.com/jeffvli/feishin)** | Escritorio (AppImage Linux) | ✅ Funcional |
| 🎵 **[Finamp](https://github.com/jammsen/finamp)** | Android (version Beta) | ✅ Funcional |

> **Proximamente** se probará en otros clientes de Jellyfin. Si has probado el plugin en un cliente que no aparece en la lista, por favor envía tu reporte de uso abriendo un [Issue](https://github.com/pepebarrascout/jellyfin-plugin-lastfm/issues) para que podamos actualizar esta tabla.

---

## 🚀 Instalacion

### Metodo 1: Desde el Catalogo de Plugins de Jellyfin (via Manifest)

Esta es la forma mas sencilla de instalar el plugin. Solo necesitas agregar el manifest de este repositorio como fuente de plugins en tu servidor Jellyfin.

1. En tu servidor Jellyfin, navega a **Panel de Control > Plugins > Repositorios**
2. Haz clic en el boton **+** (agregar repositorio)
3. Ingresa los siguientes datos:
   - **Nombre**: `Last.fm Plugin`
   - **URL del Manifest**: `https://raw.githubusercontent.com/pepebarrascout/jellyfin-plugin-lastfm/main/manifest.json`
4. Haz clic en **Guardar**
5. Navega a la pestana **Catalogo**
6. Busca **Last.fm** en la lista de plugins disponibles
7. Haz clic en **Instalar**
8. Reinicia Jellyfin cuando se te solicite

> **Nota**: Cada vez que se publique una nueva version en este repositorio, Jellyfin la detectara automaticamente y te ofrecera actualizar.

### Metodo 2: Instalacion Manual

1. Descarga la ultima version desde [Releases](../../releases)
2. Descomprime el archivo ZIP
3. Copia todos los archivos `.dll` a la carpeta de plugins de tu servidor Jellyfin:
   - **Linux**: `~/.config/jellyfin/plugins/`
   - **Windows**: `%LocalAppData%\Jellyfin\plugins\`
   - **macOS**: `~/.local/share/jellyfin/plugins/`
   - **Docker**: Monta un volumen en `/config/plugins` dentro del contenedor
4. Reinicia Jellyfin

---

## ⚙️ Configuracion

### Paso 1: Obtener credenciales de la API de Last.fm

Necesitas una [cuenta de API de Last.fm](https://www.last.fm/api/account/create) para usar este plugin:

1. Ve a [last.fm/api/account/create](https://www.last.fm/api/account/create) e inicia sesion
2. Completa los detalles de la aplicacion (cualquier nombre y descripcion funcionan)
3. Copia tu **API Key** y **Shared Secret** — las necesitaras mas adelante

### Paso 2: Configurar el plugin en Jellyfin

1. Navega a **Panel de Control > Plugins > Last.fm**
2. Ingresa tu **API Key** y **Shared Secret** en la seccion de credenciales
3. Haz clic en **Save Changes** para guardar las credenciales
4. En la seccion **Authentication**, haz clic en **Connect to Last.fm**
5. Se abrira una pagina de Last.fm solicitando autorizacion. Haz clic en **Yes, allow access**
6. Regresa a Jellyfin y haz clic en **I Already Approved**
7. Si todo sale bien, veras el mensaje "Successfully connected" y tu nombre de usuario

### Opciones de Configuracion

| Opcion | Predeterminado | Descripcion |
|---|---|---|
| Enable Scrobbling | Si | Activa/desactiva el scrobbling a Last.fm |
| Enable Now Playing Notifications | Si | Activa/desactiva las notificaciones de ahora reproduciendo |
| Scrobble after | 25% | Porcentaje de la cancion a reproducir antes de scrobblear (max. 4 minutos) |
| Minimum track duration | 25s | Duracion minima en segundos para que una cancion sea elegible para scrobble |
| Auto-love liked tracks | Si | Marca automaticamente como favoritas las canciones que tienes como favoritas en Jellyfin |
| Use Album Artist for scrobbling | No | Usa el artista del album en lugar del artista de la pista para el scrobble |

---

## 🔧 Solucion de Problemas

### Los scrobbles no aparecen en Last.fm

- Verifica que tu **API Key** y **Shared Secret** sean correctos
- Asegurate de haber completado el proceso de autorizacion (debes ver "Connected as [usuario]")
- Verifica que las canciones duren al menos 25 segundos (configuracion predeterminada)
- Confirma que hayas alcanzado el umbral de scrobble (25% de reproduccion o 4 minutos)
- Revisa los registros (logs) de Jellyfin buscando mensajes del plugin Last.fm

### El plugin no aparece en el Dashboard

- Asegurate de estar usando Jellyfin 10.11.x o superior
- Reinicia Jellyfin despues de instalar el plugin
- Verifica que los archivos `.dll` esten en la carpeta correcta de plugins

### La configuracion no se guarda

- Haz clic en **Save Changes** despues de ingresar tus credenciales ANTES de intentar conectar
- Si usas Jellyfin en Docker, asegurate de que el directorio de configuracion este montado como volumen persistente

### Problemas de conexion con Last.fm

- Asegurate de que `https://ws.audioscrobbler.com` sea accesible desde tu servidor Jellyfin
- Si usas un proxy o firewall, verifica que permita conexiones salientes a Last.fm
- Intenta reconectarte haciendo clic en **Disconnect** y luego en **Connect to Last.fm** nuevamente

---

## 🛠️ Compilacion desde el Codigo Fuente

### Requisitos

- .NET SDK 9.0 o superior
- Fuentes de NuGet configuradas:
  - `https://api.nuget.org/v3/index.json`
  - `https://nuget.jellyfin.org/v3/index.json`

### Comandos de Compilacion

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

## 💡 Recomendacion

Si te interesa crear **listas inteligentes y dinamicas** en Jellyfin (playlists y colecciones basadas en reglas que se actualizan automaticamente), te recomiendo probar el plugin **[Jellyfin SmartLists](https://github.com/jyourstone/jellyfin-smartlists-plugin/)**:

- Crea playlists automaticas basadas en genero, artista, calificacion, fecha, estado de reproduccion y mucho mas
- Interfaz web moderna para gestionar tus listas
- Funciona con todos los tipos de medios (peliculas, series, musica, etc.)
- Se actualiza automaticamente cuando tu biblioteca cambia

<a href="https://github.com/jyourstone/jellyfin-smartlists-plugin/">
    <img alt="SmartLists Plugin" src="https://img.shields.io/badge/SmartLists-Plugin-6c5ce7?logo=github&logoColor=white&style=for-the-badge"/>
</a>

---

## 💬 Soporte y Contribuciones

- **Reportes de bugs y sugerencias**: Usa la seccion de [Issues](https://github.com/pepebarrascout/jellyfin-plugin-lastfm/issues) para reportar problemas o proponer nuevas funciones
- **Contribuciones**: Las contribuciones son bienvenidas. No dudes en enviar un Pull Request
- **Reportes de uso**: Si has probado el plugin en un cliente de Jellyfin que no aparece en la lista de [Clientes Probados](#-clientes-probados), por favor compartelo

---

## ⚠️ Disclaimer

Este proyecto se proporciona tal cual (as-is) sin garantias de ningun tipo. El autor no se hace responsable de cualquier dano, perdida de datos o problema derivado del uso de este plugin. Last.fm y sus respectivos logotipos son marcas registradas propiedad de Last.fm Ltd. Este plugin no esta afiliado con, respaldado por, ni patrocinado por Last.fm Ltd.

---

## 📄 Licencia

Este proyecto esta bajo la Licencia MIT — consulta el archivo [LICENSE](LICENSE) para mas detalles.
