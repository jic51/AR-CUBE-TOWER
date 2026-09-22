# Bitácora — AR Tower Game

Registro de trabajo del proyecto. Se actualiza al cierre de cada sesión.

**Repositorio:** https://github.com/jic51/AR-CUBE-TOWER
**Stack:** Unity 6.0.3 · ARFoundation 6.3.2 · URP 17.0 · Android
**Guía de pruebas en dispositivo:** [`COMO-PROBAR-EN-EL-TELEFONO.md`](COMO-PROBAR-EN-EL-TELEFONO.md) — pasos concretos, sin consola
**Guía de mudanza fuera de OneDrive:** [`COMO-MOVER-EL-PROYECTO.md`](COMO-MOVER-EL-PROYECTO.md)

---

## Rutina de trabajo

| Momento | Acción |
|---|---|
| **Al iniciar** | Traer el repo (`git fetch`), comparar lo que hay en GitHub con lo local y **releer esta bitácora antes de tocar nada**. Si lo que llega toca `ProjectSettings/` o `Packages/`, **cerrar Unity antes del `git pull`**: Unity los tiene en memoria y los reescribe al guardar |
| **Durante** | Contrastar lo que dice la bitácora contra el código real: lo que sigue pendiente, lo que ya está hecho y todo hallazgo nuevo se anota aquí |
| **Al cerrar** | Actualizar esta bitácora + commit + **push a `main`** |

El objetivo de esta rutina es que la bitácora sea suficiente para retomar el proyecto desde cualquier máquina sin depender de la memoria de nadie.

**Dónde vive el proyecto:** en `C:\Dev\AR-CUBE-TOWER`, **nunca** dentro de OneDrive, Dropbox, Google Drive ni Documentos sincronizados — ver el incidente del 2026-09-21. El respaldo es GitHub, no la nube del sistema operativo: la carpeta del disco es desechable y se recupera con `git clone`.

---

## Estado global del proyecto

### Completado

- Sistema de anuncios AR propio (ARAdSystem) con auto-dimensionado y guía dinámica
- Economía: monedas, gemas, vidas con regeneración temporal, comodines
- HUD persistente (PlayerHeaderUI) con botón de acción contextual
- Tienda con tabs sincronizada por eventos
- Máquina de estados del juego, efecto pozo, flujo de rescate
- 40 niveles definidos en 8 bloques de dificultad
- Repositorio Git configurado y publicado

### Pendiente — Código

Reverificado contra el código real el 2026-09-21. Los cuatro siguen abiertos.

| # | Tarea | Archivo | Estado |
|---|---|---|---|
| 1 | **Sistema de sonido completo** — cero `AudioSource`, `AudioClip` o `PlayOneShot` en `Assets/Scripts`. El único `AudioSource` del repo pertenece al sistema de anuncios (`ARAdBreak.cs:45`) y el único `.wav` es de un sample de XR Interaction Toolkit | nuevo | No empezado |
| 2 | **Modo supervivencia** — no existe. Lo único que apunta a él es `LevelManager.cs:10`, donde `tiempoLimite = 0` ya significa "sin límite": es la base sobre la que construirlo | nuevo | No empezado |
| 3 | **Toggle de idioma** en configuración (sonido y vibración ya funcionan) — cero ocurrencias de idioma/language/localization en todo `Assets/Scripts` | `PanelConfiguracion.cs` | No empezado |
| 4 | Verificar en dispositivo que el bug de destrucción en cascada quedó resuelto | `CuboInteligente.cs:251-253` | Umbral presente (`caída > 3× la altura del cubo` **y** `velocidad > 2 m/s`), falta probar |

### Ya resuelto (estaba mal listado como pendiente)

Reverificado el 2026-09-21: las siete siguen en pie, ninguna se ha regresado.

| Tarea | Verificación |
|---|---|
| `GruaController.Instance` | Existe en `GruaController.cs:6` |
| `ReticuloVisible` | Existe en `GruaController.cs:30` |
| `ReticuloAlturaHit` | Existe en `GruaController.cs:32` |
| Sensibilidad de pinch | Ahora `0.000025f` con slider en Inspector (`SetupFase.cs:22`) |
| Mensajes de setup AR en inglés | Implementado (`SetupFase.cs:122`) |
| Interacciones entre tipos de cubo | Las 5 implementadas en `CuboInteligente.cs:582` (`AplicarEfectoDeImpacto`) |
| CuboPreview3D con RawImage | Implementado (RenderTexture presente) |
| ARM64 obligatorio de Play | Ya cumplido: `AndroidTargetArchitectures: 2` (solo ARM64) |

### Pendiente — Seguridad

25 hallazgos totales (19 de la auditoría + 6 nuevos del 2026-09-21). De ellos, 7 están resueltos en código (1, 2, 3, 5, 6, 16, 17), 3 cerrados como riesgo asumido (7, 14, N1) y 1 es informativo (N5): **quedan 14 abiertos**, y el hallazgo 4 es el único que todavía bloquea el lanzamiento. Todas las referencias `archivo:línea` fueron reverificadas el 2026-09-21 contra el código de `main`.

#### Bloqueantes

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 1 | Softlock durante el anuncio AR | `ARAdBreak.cs:98,318-352` | ✅ Confirmado en código (`SegundosMaxEspera = 8f`). Sin probar en dispositivo |
| 2 | Farming infinito de monedas | `GameManager.cs` | ✅ Confirmado en código. Sin probar |
| 3 | Las vidas no bloquean nada | `GameManager.cs:919,927-936` | ✅ Confirmado: `HayVidasParaJugar()` existe y se invoca; `GastarVida()` en `IniciarPartida()` (`:321`). Sin probar |
| 4 | **Sin política de privacidad ni Data Safety** — la app pide cámara; Play lo exige | ficha de Play | Pendiente |

#### Altos y medios

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 5 | Recompensa completa del anuncio sin verlo | `ARAdBreak.cs:277` | ✅ Confirmado: el reloj arranca en el spawn del objeto |
| 6 | El pack de 3 vidas cobra 120 monedas y puede entregar 1 | `TiendaManager.cs` | ✅ Resuelto el 2026-09-21: se valida el hueco antes de cobrar y el botón solo se habilita si caben las 3. Precios movidos a constantes de `EconomiaManager`. Sin probar |
| 7 | Regeneración de vidas manipulable adelantando el reloj del teléfono | `EconomiaManager.cs:60` | ⚖️ **Riesgo asumido** el 2026-09-21 — ver *Decisión: el reloj del dispositivo* |
| 8 | ARCore Depth marcado *Required* pero desactivado en runtime — recorta dispositivos | `AR Core Settings.asset` (`m_Depth: 0`) | Pendiente — confirmado y agravado, ver hallazgo **N3** |
| 9 | Build sin endurecer | `ProjectSettings.asset` | Parcial — ID y `companyName` ya resueltos (ver **N4**); faltan R8, target SDK y keystore |
| 10 | API key `DEMO_KEY` serializada en la escena e inicializa el SDK en producción | `ARTowerGame.unity:6790` | Pendiente — confirmado |
| 11 | Guardado JSON sin HMAC — economía editable | `SaveSystem.cs:14` | Pendiente — confirmado (`JsonUtility.ToJson` + `File.WriteAllText`, texto plano) |
| 12 | Métodos `DEBUG_` sin `#if UNITY_EDITOR` (uno da 999 monedas) | `GameManager.cs:997-1054` | Pendiente — confirmado. Son 4 y ninguno está entre directivas. Riesgo real atenuado: son `[ContextMenu]` privados, no alcanzables desde la UI del build, pero viajan en el binario |
| 13 | Eliminar campo `fechaNacimiento` sin uso | `PlayerData.cs:9` | Pendiente — confirmado: es su **única** aparición en todo el repo |
| 14 | Bonus diario manipulable por fecha del dispositivo | `GameManager.cs:166` | ⚖️ **Riesgo asumido** el 2026-09-21 — ver *Decisión: el reloj del dispositivo* |
| 15 | Nombre de usuario sin límite ni sanitización | `PanelConfiguracion.cs:125-127` | Pendiente — confirmado: la única validación es `Length > 0` |

#### Bajos

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 16 | `Application.OpenURL` sin validar el esquema de la URL | `ARAdBreak.cs` (`TryNormalizarUrl`) | ✅ Resuelto el 2026-09-21: solo http/https; además completa `https://` cuando el anunciante escribe `www.marca.com`. Sin probar |
| 17 | `MostrarBreak()` no llama al callback si está ocupado → deja `timeScale` en 0 | `LayeredAds.cs:103-111` | ✅ Resuelto en la Fase 1 — estaba mal listado aquí |
| 18 | `PlayerPrefs.DeleteAll()` borra claves de terceros | `PanelConfiguracion.cs:160` | Pendiente — confirmado |
| 19 | Log de API key visible en Logcat de producción | `LayeredAds.cs:52` | Pendiente — confirmado. Imprime los primeros 8 caracteres en cualquier build |

#### Hallazgos nuevos — 2026-09-21

| # | Hallazgo | Archivo:Línea | Severidad |
|---|---|---|---|
| **N1** | **El decrecimiento de recompensa por anuncio es evadible.** `CalcularRecompensaAd()` compara contra `DateTime.Now` (hora local del teléfono) para decidir si resetear `adsVistosHoy`. Adelantar la fecha del dispositivo devuelve la recompensa al 100 % tantas veces como se quiera, así que el fix de la Fase 1 no cierra el farming: lo encarece un toque de ajustes. Es el mismo agujero que el hallazgo 14, y también el 7: **los tres dependen del reloj del dispositivo** | `EconomiaManager.cs:123` | ⚖️ **Riesgo asumido** el 2026-09-21 — ver *Decisión: el reloj del dispositivo* |
| **N2** | `LayeredAds.Inicializar(null)` lanza `NullReferenceException`: el log evalúa `apiKey.Length` antes de cualquier guarda. Con cadena vacía funciona; solo `null` rompe. Un `if (string.IsNullOrEmpty(apiKey))` al entrar lo resuelve y de paso cubre el hallazgo 19 | `LayeredAds.cs:47-52` | Bajo |
| **N3** | El hallazgo 8 es peor de lo listado: además de que `GameManager.cs:144-148` desactiva la oclusión en runtime, **no hay ni un solo `AROcclusionManager` en la escena** (0 ocurrencias en `ARTowerGame.unity`). Es decir, `m_Depth: 0` (*Required*) está recortando el catálogo de dispositivos a cambio de una función que el juego no usa en ninguna parte. Cambiarlo a *Optional* no rompe nada | `AR Core Settings.asset` | Medio |
| **N4** | El hallazgo 9 estaba parcialmente mal: **`stripEngineCode: 1` ya está activo**. Lo que de verdad falta, leído del asset: `AndroidTargetSdkVersion: 0` (Automatic), `AndroidMinifyRelease: 0` (sin R8) y `androidUseCustomKeystore: 0` (firma con la keystore de debug). El ID de aplicación y el `companyName` **ya quedaron resueltos** el 2026-09-21 — ver *Decisión: identificador de la aplicación* | `ProjectSettings.asset:184-189,292,298` | Alto — parcialmente resuelto |
| **N5** | No existe ningún `AndroidManifest.xml` propio en el repo: Unity genera el suyo en cada build. No es un fallo, pero conviene saberlo antes de la ficha de Play, porque declarar permisos a mano exigirá crear uno en `Assets/Plugins/Android/` | — | Informativo |
| **N6** | **El proyecto vive dentro de OneDrive** (`C:\Users\remod\OneDrive\Documents\JoseCastro\APPS\AR-CUBE-TOWER`), según la ruta del APK en el log del 2026-09-21. Unity y la sincronización en la nube se llevan mal: OneDrive intenta subir `Library/` y `Temp/` —decenas de miles de archivos que Unity reescribe constantemente—, lo que provoca bloqueos de archivo a media compilación, importaciones corruptas y builds más lentos. Que un build haya tardado 20 minutos encaja | Entorno de desarrollo | **En curso** — mudanza a `C:\Dev\AR-CUBE-TOWER` documentada en [`COMO-MOVER-EL-PROYECTO.md`](COMO-MOVER-EL-PROYECTO.md) |

### Pendiente — Trabajo en el Editor de Unity

| Tarea | Dónde |
|---|---|
| Vertical Layout Group + Content Size Fitter | `contenedorBotones` |
| Altura del prefab de botón de nivel a 85px | `prefabBotonNivel` |
| Ejecutar "CubePile → Crear Materiales de Cubos Faltantes" | Menú del Editor |
| Conectar campos de botones y textos | Inspector de `TiendaManager` |
| Cambiar onClick del botón pausa a `PlayerHeaderUI.OnBotonAccionClick()` | Inspector |
| Crear GameObjects hijos `IconoPausa` e `IconoCerrar` | Jerarquía del Canvas |
| Asignar textura a `TestAdConfig.texturaImagen` | Inspector |
| Asignar las **3 imágenes de estrella por separado** (hoy hay 1 sola, el gráfico entero: por eso siempre salen 3 estrellas aunque el resultado sea "Plata") | `GameManager` → `imagenesEstrellas` |
| Asignar `textoPrecioVida1` y `textoPrecioVidaPack` (el pack muestra "50" pero cuesta 120) | Inspector de `TiendaManager` |
| Botón CTA del anuncio: el texto es más grande que el fondo y se monta sobre el Skip | Prefab `ARAdBreakPrefab` |
| Texto del botón de anclaje dice "Anchore" | Escena, setup AR |
| "1 Live" → "1 Life" | Tienda, pestaña vidas |
| `adCadaNDerrotas` está en **1** en la escena; el código trae 2 | Inspector de `GameManager` — ver decisión pendiente |

### Pendiente — Publicación (2 de 7)

- ~~Decidir el `applicationIdentifier` definitivo~~ ✅ `io.github.jic51.arcubetower` — ver *Decisión: identificador de la aplicación*
- ~~`companyName` real en lugar de `DefaultCompany`~~ ✅ `jic51`
- **Keystore de Android — urgente**, ya no es solo requisito de publicación: la firma de debug provocó el fallo de instalación del 2026-09-21. Crearla **y respaldarla fuera de la máquina**: perder la keystore de producción tras publicar significa no poder actualizar la app nunca más
- Ícono de la app
- Capturas de pantalla para la ficha
- Política de privacidad y formulario de Data Safety (la app pide cámara)
- Optimización de build: R8 activo y target SDK fijo en lugar de Automatic

---

## Decisiones permanentes

Decisiones que ya están tomadas y no hay que volver a discutir. Si alguna se revisa, se anota aquí la fecha y el motivo.

### Decisión: identificador de la aplicación — 2026-09-21

**`io.github.jic51.arcubetower`**, aplicado en `ProjectSettings.asset:173`. `companyName` pasa de `DefaultCompany` a `jic51`.

El proyecto no tiene dominio propio. La convención de Android es usar un dominio en orden inverso, y `io.github.<usuario>` es la forma estándar y reconocida de derivar un espacio de nombres de una cuenta de GitHub cuando no hay dominio: está respaldado por https://jic51.github.io/AR-CUBE-TOWER/, que sí controlamos. El ID anterior, `com.unity.jc.ARTowerGame`, colgaba de un dominio de Unity Technologies.

**Por qué importaba decidirlo ya:** el ID es libre de cambiar mientras el proyecto no se haya subido nunca a Play Console. Desde la primera subida —incluso a un canal de prueba interna— queda congelado de por vida: Play lo usa como clave primaria de la ficha. Cambiarlo después no es editar un campo, es publicar una aplicación distinta, con ficha nueva, URL nueva, cero instalaciones y cero reseñas, y los usuarios que ya tuvieran la anterior no recibirían la actualización. El ID viejo tampoco se libera.

**Efecto secundario a tener en cuenta:** en Android la ruta de datos de la app deriva del ID, así que **los guardados de cualquier dispositivo de prueba se pierden** en la próxima instalación. Sin consecuencia real: `DEBUG_DarRecursos` rehace el estado.

### Decisión: el reloj del dispositivo — 2026-09-21

**Riesgo asumido y documentado. Cierra los hallazgos 7, 14 y N1.**

La regeneración de vidas, el bonus diario y el decrecimiento de recompensa por anuncio leen la fecha y hora del teléfono. Un jugador que adelante el reloj en ajustes puede regenerar vidas al instante, cobrar el bonus diario varias veces al día y resetear el contador de anuncios para mantener la recompensa al 100 %.

**No se va a corregir**, y el motivo es que el remedio cuesta más de lo que evita: anclar el tiempo a NTP o a un servidor propio mete una dependencia de red en el arranque de un juego que hoy funciona entero sin conexión, y obliga a decidir qué pasa cuando esa consulta falla.

**Lo que hace aceptable el riesgo:** aquí no hay dinero real. El jugador que manipule el reloj se hace trampas a sí mismo en un juego de un solo jugador, sin clasificaciones ni compras integradas. El daño máximo es que se salte una espera diseñada para él.

**Lo que invalidaría esta decisión —revisarla si ocurre cualquiera de estas—:**

- Se añaden compras integradas con dinero real
- Se añade una clasificación, un multijugador o cualquier comparación entre jugadores
- El contrato con un anunciante empieza a pagar por impresión, porque entonces el reloj manipulado nos cuesta dinero a nosotros o al anunciante

Mientras nada de eso pase, estos tres hallazgos **no son fallos pendientes** y no deben volver a listarse como tales.

---

## Sesiones

### 2026-09-21 (Mac) — Revisión de 2 videos de juego en el Editor

Dos grabaciones del juego en el Editor de Unity con XR Simulation (4:03 y 2:48). Se analizaron 103 fotogramas, uno cada 4 s, y uno por segundo en los tramos dudosos.

**Primera validación real de la Fase 1** — compila y corre:

- Las vidas bajan 5 → 4 → 3 al *iniciar* cada partida
- El primer anuncio del día dio las 50 monedas completas (130 → 180)
- Botón contextual del header (pausa ↔ X en la tienda) y pausa real con la tienda abierta
- Botones del rescate deshabilitados sin monedas suficientes; gemas al completar nivel por primera vez

**Bugs corregidos en código**

| Bug visto en el video | Causa | Arreglo |
|---|---|---|
| "Platform descending..." encima del panel "Don't give up!" | El rescate no cambia el estado, así que `Update()` seguía corriendo el pozo | Flag `_enRescate` que detiene la partida mientras el panel está abierto |
| Dos contadores de Skip distintos a la vez ("Skip in 3s" arriba, "Skip in 5s" fijo abajo) | El botón tenía su propio texto estático y la cuenta iba en otro | La cuenta se muestra en el propio botón; el texto suelto se oculta |
| El Skip se podía pulsar desde el segundo 0 | Se activaba antes de la cuenta sin bloquear la interacción | `interactable = false` hasta que termina la cuenta |
| Error de macOS "-50" al pulsar "Saber más" | `urlCTA` es `WWW.DENTACCEPT.COM`, sin `https://` | Normalización de URL (cierra el hallazgo 16) |
| El pack de 3 vidas muestra 50 y cobra 120 | Precio escrito a mano en escena y código | Constantes en `EconomiaManager`; el texto sale del código (falta asignarlo en el Inspector) |
| Con 4 vidas el pack cobra 120 y da 1 | No validaba el hueco antes de cobrar | Cierra el hallazgo 6 |

**Aclarado: por qué "Watch ad!" no rescató en el primer video.** El clic llegó en el último segundo de la cuenta; el panel se cerró por tiempo y lo que se vio fue el anuncio *automático* de derrota (con `adCadaNDerrotas = 1` sale tras cada derrota), que termina en GameOver por diseño. Desde el punto de vista del jugador parece que "vio el anuncio y no le rescató". Queda como decisión de producto.

**Por investigar en dispositivo**

- Tras "Retry" o "Next level" el entorno AR se ve negro y el escaneo tarda mucho. Solo la primera partida muestra la habitación simulada. Puede ser propio de XR Simulation al recargar la escena
- La consola se llena de avisos `JobTempAlloc ... more than 4 frames old — likely a leak`. Suele venir del Editor; confirmar que no ocurre en el teléfono

**Decisiones pendientes**

- Idioma de la interfaz: casi todos los paneles mezclan español e inglés ("¡Don't give up!", "75 monedas", "¡TIEMPO AGOTADO!", "Retry", "Cubos:")
- Frecuencia del anuncio automático de derrota
- Criterio del pozo: baja la plataforma aunque la torre sea corta y se vea entera (con 3 cubos, repetidas veces)

**Sincronización:** al subir, GitHub tenía 8 commits de la sesión de Windows que esta máquina no tenía. Se fusionaron sin forzar; la bitácora combina ambas.

---

### 2026-09-21 — Auditoría de contraste: bitácora vs. código real

Sesión de verificación, sin cambios de código. El objetivo era comprobar si lo que dice esta bitácora sigue siendo cierto y arreglar lo que no lo fuera.

**Estado de sincronización**

Local y `origin/main` en el mismo commit (`0440934`), sin cambios sin guardar. El repo pesa 15 MB. Nada que reconciliar: el push de la Fase 1 sí llegó.

**Lo que se comprobó**

Los 19 hallazgos de seguridad y las 4 tareas de código, uno por uno, leyendo el archivo y la línea. Resultado:

- **Los cuatro fixes de la Fase 1 están en el código.** Tope de 8 s en `ARAdBreak`, `HayVidasParaJugar()` invocado, el reloj del anuncio arrancando en el spawn, el watchdog de 45 s en ambos flujos. Siguen **sin compilar en Unity ni probarse en dispositivo** — eso no ha cambiado.
- **Las 11 tareas que seguían pendientes, siguen pendientes.** Ninguna se resolvió por accidente.
- **El hallazgo 17 ya estaba resuelto** desde la Fase 1 y seguía listado como pendiente en la tabla de bajos. Corregido.
- **Tres referencias `archivo:línea` habían quedado obsoletas** tras los cambios de la Fase 1: `OpenURL` pasó de `ARAdBreak.cs:555` a `:583`, los métodos `DEBUG_` de `GameManager.cs:870` a `:997-1054`, el bonus diario de `:155` a `:166`. Todas actualizadas.

**Hallazgos nuevos — 5**

Están detallados arriba en su propia tabla (N1–N5). Los dos que cambian prioridades:

- **N1 — el fix de farming de la Fase 1 tiene una puerta trasera.** La recompensa decreciente por anuncio se resetea cambiando la fecha del teléfono, igual que el bonus diario y la regeneración de vidas. Son tres síntomas de una sola causa: **toda la economía temporal confía en el reloj del dispositivo.** No tiene sentido parchearlos por separado; o se ancla el tiempo a una fuente externa, o se asume el riesgo por escrito y se deja de listarlos como fallos.
- **N4 — el ID de la aplicación es una decisión irreversible y está sin tomar.** `com.unity.jc.ARTowerGame` usa un dominio que no es nuestro y **no se puede cambiar después de publicar**. Esto deja de ser una tarea de build para convertirse en lo primero de la Fase 4.

De paso, el hallazgo 9 estaba parcialmente mal: el stripping de engine code ya estaba activo. Lo que falta es R8, el target SDK fijo y la keystore.

**Decisiones tomadas en esta sesión**

Las dos que quedaban abiertas se cerraron el mismo día y están arriba, en *Decisiones permanentes*:

- **Identificador de la aplicación:** `io.github.jic51.arcubetower`, ya aplicado en `ProjectSettings.asset`. `companyName` pasa a `jic51`. Era lo único de la Fase 4 con fecha de caducidad —deja de poder cambiarse en la primera subida a Play— y ya no lo es.
- **Reloj del dispositivo:** riesgo asumido por escrito, con las tres condiciones que obligarían a revisarlo. Los hallazgos 7, 14 y N1 dejan de contar como pendientes.

**Sobre la licencia del Android SDK**

Se revisó el acuerdo que Unity pide aceptar al instalar el SDK de Android. Es el contrato estándar y hay que aceptarlo para compilar; no hay nada en él que condicione el diseño del juego. Los tres puntos que sí nos tocan:

- **§4.3** obliga a dar aviso de privacidad adecuado a los usuarios. No añade trabajo: ya lo exigía Play por pedir cámara, y sigue siendo el hallazgo bloqueante 4.
- **§3.7** prohíbe usar nombres, logos o marcas de Google. A tener presente al diseñar el ícono y las capturas de la ficha.
- **§6.1** dice que el SDK recoge telemetría de uso del propio SDK, previo consentimiento. Afecta a la máquina de desarrollo, no a la app ni a los jugadores.

**Pendiente inmediato**

1. Abrir el proyecto en Unity, compilar y probar el flujo completo en dispositivo. Todo lo de la Fase 1 depende de esto y nada nuevo debería construirse encima hasta validarlo. **Ojo:** al cambiar el ID de la aplicación, la primera instalación tras este cambio empieza con el guardado vacío.
2. Resto de la Fase 4: keystore propia, R8, target SDK fijo, ícono, capturas y política de privacidad.

---

### Incidente diagnosticado — Build and Run falla al instalar en el teléfono

**Causa confirmada por el `Editor.log`.** Error exacto:

```
adb.exe: failed to install AR CUBE PILE.apk: Failure
[INSTALL_FAILED_UPDATE_INCOMPATIBLE: Existing package com.unity.jc.ARTowerGame
 signatures do not match newer version; ignoring!]
```

**Lo primero: el build funcionó.** `Build completed with a result of 'Succeeded' in 1217 seconds`. Veinte minutos, no una hora — la hora percibida incluía la reimportación de assets previa. Lo que falló fue únicamente el despliegue. **El APK está construido y es válido**, en `AR-CUBE-TOWER\AR CUBE PILE.apk`. No hace falta recompilar para probar la Fase 1.

**Las dos cosas que confirma el log**

1. **El cambio de ID no llegó a la máquina de desarrollo.** El log nombra `com.unity.jc.ARTowerGame`, no `io.github.jic51.arcubetower`. Unity compiló con el identificador viejo. Un detalle apuntaba en la misma dirección: el APK se llama `AR CUBE PILE.apk` mientras el repo tiene `productName: AR_Tower_Game`.

   **Causa confirmada ese mismo día.** Al intentar el `git pull`, git lo rechazó:

   ```
   Updating 681e1cc..a23667f
   error: Your local changes to the following files would be overwritten by merge:
           ProjectSettings/ProjectSettings.asset
   ```

   Dos cosas quedan probadas. La máquina estaba en `681e1cc` —trajo el primer commit del día pero nunca `f5102bc`, el del cambio de ID— y **Unity había reescrito `ProjectSettings.asset` localmente**, dejando cambios sin registrar que bloqueaban la actualización. Es la segunda de las dos causas que se habían planteado, y confirma que la regla de cerrar Unity antes del pull no era una precaución teórica.

   Salida aplicada: `git stash` para apartar lo que escribió Unity —recuperable, no borrado— y después `git pull`.
2. **`signatures do not match`** — la app instalada se firmó con una debug keystore que ya no es la de esta máquina. Android nunca permite actualizar una app con una firma distinta, por diseño: es la garantía de que nadie pueda suplantar una app ajena. No hay forma de reconciliarlo; la app vieja **hay que desinstalarla**.

**Por qué esto importa más de lo que parece**

Es el hallazgo 9 (`androidUseCustomKeystore: 0`) cobrándose su primera factura. Y es un ensayo a escala pequeña de un desastre grande: **el día que la app esté en Play, perder la keystore de producción significa perder la app para siempre** — sin posibilidad de publicar una actualización nunca más, con el mismo error que salió hoy. Hoy se arregla desinstalando; entonces no se arregla.

**Crear la keystore propia y respaldarla deja de ser tarea de publicación y pasa a ser urgente.**

**Aprendizaje operativo:** cualquier cambio que llegue por git y toque `ProjectSettings/` o `Packages/` exige **cerrar Unity antes del pull**. Ya incorporado a la rutina de trabajo.

**Salida del incidente**

La primera propuesta fue desinstalar la app vieja con `adb` e instalar a mano el APK ya construido. Se descartó: obligaba a usar la consola y a pelear contra el conflicto en lugar de evitarlo.

**El camino elegido rodea el problema.** Al traer el cambio de identificador, el juego pasa a llamarse `io.github.jic51.arcubetower` y el teléfono lo trata como **una aplicación distinta**: se instala sin chocar con la vieja, que se queda ahí sin molestar. No hay que desinstalar nada ni tocar `adb`.

Pasos en [`COMO-PROBAR-EN-EL-TELEFONO.md`](COMO-PROBAR-EN-EL-TELEFONO.md).

---

### Incidente — Intento de mudanza fuera de OneDrive

A raíz del hallazgo N6 se intentó mover el proyecto copiando la carpeta a mano. La copia no terminó, se borraron archivos a medio camino y quedó la duda de si se había perdido algo de la carpeta original.

**No se perdió nada.** Verificado contra el repositorio: **1.020 archivos rastreados**, incluidos los 22 scripts de `Assets/Scripts`, la escena `ARTowerGame.unity`, 53 imágenes, 44 materiales, 35 prefabs, los `ProjectSettings` y el paquete `ARAdSystem` completo. Aunque se borrase la carpeta entera, se recupera íntegra desde GitHub.

Lo único sin respaldo sigue siendo el video de desarrollo (`Assets/Videos/*.mp4`), excluido desde el 2026-09-19 por pesar 399 MB. El resto de lo no rastreado —`Library/`, `Temp/`, `Logs/`, el APK— se regenera solo.

El `git stash` previo había guardado cuatro archivos de configuración reescritos por Unity (`ProjectSettings.asset`, `manifest.json`, `packages-lock.json`, `UniversalRenderPipelineGlobalSettings.asset`). Nada de trabajo creativo: ni escena, ni scripts, ni prefabs.

**Causa raíz del fallo de copia:** copiar un proyecto de Unity a mano falla casi siempre por `Library/`, que tiene decenas de miles de archivos pequeños en uso. Y no hace falta copiarla: Unity la reconstruye sola.

**Lección — no copiar, clonar.** La forma correcta de mudar el proyecto es `git clone` en la ruta nueva. Sale limpio, actualizado y sin `Library/`. Documentado en [`COMO-MOVER-EL-PROYECTO.md`](COMO-MOVER-EL-PROYECTO.md).

**Regla de aquí en adelante:** los proyectos van en `C:\Dev\`, nunca en carpetas sincronizadas (OneDrive, Dropbox, Google Drive, Documentos). El respaldo es GitHub, que además guarda historial. La carpeta del disco es desechable.

---

### 2026-09-20 — Fase 1: fixes bloqueantes

**Decisiones de producto tomadas**

| Tema | Decisión |
|---|---|
| Vidas | Se cobran **al iniciar partida**; con 0 vidas no se puede jugar y se abre la tienda |
| Recompensa por anuncios | **Decreciente dentro del día**: 50 / 35 / 20 / 10 monedas |
| Público objetivo | **Mayores de 13 años** (Teen) — evita restricciones de la política Families |

**Logrado**

- **Softlock del anuncio resuelto.** `EsperarDireccion()` ahora tiene un tope de 8 segundos: si el usuario no consigue apuntar (acostado, contra una pared, tracking degradado), el anuncio aparece igualmente frente a la cámara. Añadida guarda por si la cámara desaparece a mitad del break.
- **Farming de monedas resuelto.** Se eliminó el `return` que saltaba el bloque de economía. Ahora la vida se cobra, el progreso se guarda y las estadísticas se actualizan siempre; solo la aparición del panel de GameOver se difiere hasta que el anuncio termina.
- **Fraude al anunciante resuelto.** El reloj del anuncio arranca en el spawn del objeto, no al abrir el break. La duración contratada es ahora tiempo real con el anuncio visible.
- **Sistema de vidas conectado.** `HayVidasParaJugar()` bloquea la entrada a partida desde los tres caminos posibles (selección de nivel, reintentar, siguiente nivel). La vida se descuenta en `IniciarPartida()`, no antes: abandonar durante el setup AR no cuesta nada. El escudo pasa de evitar el cobro a devolver la vida.
- **Recompensa decreciente implementada.** `EconomiaManager.CalcularRecompensaAd()` con contador diario persistido en `PlayerData`.
- **Watchdog de anuncios.** Si el SDK no devuelve callback en 45 segundos, el juego se recupera solo. En el flujo de rescate el watchdog **concede** el rescate: si falla el anuncio, el fallo es nuestro y no se castiga al jugador.
- **Callback garantizado.** `LayeredAds.MostrarBreak()` ahora invoca el callback también cuando está ocupado, cumpliendo el contrato documentado. Sin esto, una llamada concurrente dejaba `timeScale` en 0 para siempre.
- Evitado el encadenamiento de dos anuncios seguidos (saltar el de rescate ya no dispara el automático).

**No verificado**

Los cambios pasan comprobación de sintaxis (llaves y paréntesis balanceados) pero **no se han compilado en Unity ni probado en dispositivo**. Queda pendiente abrir el proyecto y validar.

**Pendiente inmediato**

- Compilar en Unity y probar el flujo completo en dispositivo
- Fase 3 (endurecer build) y Fase 4 (publicación)

---

### 2026-09-19

**Logrado**

- Auditoría de seguridad manual completa — 5 hallazgos, ninguno crítico
- Auditoría profunda lanzada con agente (configuración de build, privacidad, lógica explotable, dependencias)
- Repositorio Git publicado en GitHub correctamente
- Establecida la rutina de sincronización diaria y esta bitácora

**Problemas encontrados y resueltos**

- **El push de la sesión anterior había fallado en silencio.** El repositorio en GitHub estaba vacío pese a que el output del terminal parecía indicar éxito (el mensaje `Everything up-to-date` apareció después de un `fatal: the remote end hung up unexpectedly`, lo que llevó a una confirmación errónea).
  - **Causa raíz:** un video de desarrollo de 399 MB (`Assets/Videos/Screen_Recording_20260430_145727_AR_Tower_Game.mp4`) estaba incluido en el commit. GitHub rechaza archivos mayores a 100 MB.
  - **Solución:** se excluyó la carpeta de videos vía `.gitignore`, se rehízo el commit inicial y se purgó el blob huérfano con `git gc`. El repositorio pasó de 414 MB a 15 MB. El video sigue en disco, solo se excluyó del control de versiones.

**Aprendizaje**

Un push de Git puede imprimir `Everything up-to-date` tras un fallo real. La verificación válida es `git ls-remote origin main` o `git status -sb`, no el texto del output.

**Pendiente de esta sesión** — ✅ cerrado el 2026-09-21

- ~~Revisar resultados de la auditoría profunda del agente e incorporarlos a la lista de seguridad~~ Incorporados y reverificados uno por uno contra el código.

---

### Sesiones anteriores (previo a la bitácora)

**Logrado**

- Sistema de anuncios AR con parámetros auto-ajustables para anunciantes: el tamaño del anuncio se calcula desde el FOV real de la cámara, y el borde del marco es una proporción del anuncio en vez de una medida absoluta
- Guía dinámica: el texto y la flecha de instrucción desaparecen cuando el usuario mira el anuncio y reaparecen si mira hacia otro lado, con texto contextual ("Turn right", "Look up")
- Sistema de economía conectado por eventos — la UI se actualiza sola
- Botón de acción contextual: el mismo botón es pausa durante el juego y cerrar dentro de la tienda

**Bugs corregidos**

| Bug | Causa | Solución |
|---|---|---|
| Header ocupaba toda la pantalla | `SafeAreaFitter` sobrescribía los anchors cada frame | Quitar el componente del header |
| Desuscripción de eventos nunca funcionaba | Los lambdas en `OnDestroy` eran instancias distintas a las de `Start` | Guardar los delegates como campos |
| Botón de monedas quedaba agrandado | La corrutina leía la escala ya inflada por un pulso concurrente | Capturar la escala original una vez en `Start()` |
| El juego seguía corriendo durante el anuncio | `Time.timeScale` nunca se ponía en 0 | Pausar antes del anuncio, restaurar en el callback |
| Texto del anuncio encima del panel de derrota | Los paneles seguían activos | Ocultar paneles antes del anuncio, restaurar después |
| Grid de niveles mostraba solo 5 | Faltaba layout group y `nivelMaximoDesbloqueado` estaba en 0 | Agregar layout en el Editor + subir el valor para pruebas |
