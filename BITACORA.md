# Bitácora — AR Tower Game

Registro de trabajo del proyecto. Se actualiza al cierre de cada sesión.

**Repositorio:** https://github.com/jic51/AR-CUBE-TOWER
**Stack:** Unity 6.0.3 · ARFoundation 6.3.2 · URP 17.0 · Android

---

## Rutina de trabajo

| Momento | Acción |
|---|---|
| **Al iniciar** | Traer el repo (`git fetch`), comparar lo que hay en GitHub con lo local y **releer esta bitácora antes de tocar nada** |
| **Durante** | Contrastar lo que dice la bitácora contra el código real: lo que sigue pendiente, lo que ya está hecho y todo hallazgo nuevo se anota aquí |
| **Al cerrar** | Actualizar esta bitácora + commit + **push a `main`** |

El objetivo de esta rutina es que la bitácora sea suficiente para retomar el proyecto desde cualquier máquina sin depender de la memoria de nadie.

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

21 hallazgos totales (19 de la auditoría + 2 nuevos del 2026-09-21). Los 4 primeros **bloquean el lanzamiento**. Todas las referencias `archivo:línea` fueron reverificadas el 2026-09-21 contra el código de `main`.

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
| 6 | El pack de 3 vidas cobra 120 monedas y puede entregar 1 | `TiendaManager.cs:223-235` | **Pendiente — confirmado.** `GanarVida(3)` hace `Min(vidas+3, 5)`: con 4 vidas pagas 120 y recibes 1. La guarda solo rechaza cuando ya estás al máximo |
| 7 | Regeneración de vidas manipulable adelantando el reloj del teléfono | `EconomiaManager.cs:60` | Pendiente — confirmado (`DateTimeOffset.UtcNow` local, sin ancla de servidor) |
| 8 | ARCore Depth marcado *Required* pero desactivado en runtime — recorta dispositivos | `AR Core Settings.asset` (`m_Depth: 0`) | Pendiente — confirmado y agravado, ver hallazgo **N3** |
| 9 | Build sin endurecer | `ProjectSettings.asset` | Pendiente — **parcialmente incorrecto**, ver hallazgo **N4** |
| 10 | API key `DEMO_KEY` serializada en la escena e inicializa el SDK en producción | `ARTowerGame.unity:6790` | Pendiente — confirmado |
| 11 | Guardado JSON sin HMAC — economía editable | `SaveSystem.cs:14` | Pendiente — confirmado (`JsonUtility.ToJson` + `File.WriteAllText`, texto plano) |
| 12 | Métodos `DEBUG_` sin `#if UNITY_EDITOR` (uno da 999 monedas) | `GameManager.cs:997-1054` | Pendiente — confirmado. Son 4 y ninguno está entre directivas. Riesgo real atenuado: son `[ContextMenu]` privados, no alcanzables desde la UI del build, pero viajan en el binario |
| 13 | Eliminar campo `fechaNacimiento` sin uso | `PlayerData.cs:9` | Pendiente — confirmado: es su **única** aparición en todo el repo |
| 14 | Bonus diario manipulable por fecha del dispositivo | `GameManager.cs:166` | Pendiente — confirmado (`System.DateTime.Now`) |
| 15 | Nombre de usuario sin límite ni sanitización | `PanelConfiguracion.cs:125-127` | Pendiente — confirmado: la única validación es `Length > 0` |

#### Bajos

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 16 | `Application.OpenURL` sin validar el esquema de la URL | `ARAdBreak.cs:583` | Pendiente — confirmado (la línea se movió de 555 a 583 tras la Fase 1) |
| 17 | `MostrarBreak()` no llama al callback si está ocupado → deja `timeScale` en 0 | `LayeredAds.cs:103-111` | ✅ Resuelto en la Fase 1 — estaba mal listado aquí |
| 18 | `PlayerPrefs.DeleteAll()` borra claves de terceros | `PanelConfiguracion.cs:160` | Pendiente — confirmado |
| 19 | Log de API key visible en Logcat de producción | `LayeredAds.cs:52` | Pendiente — confirmado. Imprime los primeros 8 caracteres en cualquier build |

#### Hallazgos nuevos — 2026-09-21

| # | Hallazgo | Archivo:Línea | Severidad |
|---|---|---|---|
| **N1** | **El decrecimiento de recompensa por anuncio es evadible.** `CalcularRecompensaAd()` compara contra `DateTime.Now` (hora local del teléfono) para decidir si resetear `adsVistosHoy`. Adelantar la fecha del dispositivo devuelve la recompensa al 100 % tantas veces como se quiera, así que el fix de la Fase 1 no cierra el farming: lo encarece un toque de ajustes. Es el mismo agujero que el hallazgo 14, y también el 7: **los tres dependen del reloj del dispositivo** y solo se cierran juntos, con una fuente de tiempo fiable (NTP o servidor) o aceptando el riesgo de forma explícita | `EconomiaManager.cs:123` | Medio |
| **N2** | `LayeredAds.Inicializar(null)` lanza `NullReferenceException`: el log evalúa `apiKey.Length` antes de cualquier guarda. Con cadena vacía funciona; solo `null` rompe. Un `if (string.IsNullOrEmpty(apiKey))` al entrar lo resuelve y de paso cubre el hallazgo 19 | `LayeredAds.cs:47-52` | Bajo |
| **N3** | El hallazgo 8 es peor de lo listado: además de que `GameManager.cs:144-148` desactiva la oclusión en runtime, **no hay ni un solo `AROcclusionManager` en la escena** (0 ocurrencias en `ARTowerGame.unity`). Es decir, `m_Depth: 0` (*Required*) está recortando el catálogo de dispositivos a cambio de una función que el juego no usa en ninguna parte. Cambiarlo a *Optional* no rompe nada | `AR Core Settings.asset` | Medio |
| **N4** | El hallazgo 9 estaba parcialmente mal: **`stripEngineCode: 1` ya está activo**. Lo que de verdad falta, leído del asset: `AndroidTargetSdkVersion: 0` (Automatic), `AndroidMinifyRelease: 0` (sin R8), `androidUseCustomKeystore: 0` (firma con la keystore de debug), `companyName: DefaultCompany` y `applicationIdentifier.Android: com.unity.jc.ARTowerGame` — un prefijo de dominio ajeno que **no se puede cambiar una vez publicado**. Decidir el ID definitivo es lo primero de la Fase 4 | `ProjectSettings.asset:15,184-189,292,298` | Alto |
| **N5** | No existe ningún `AndroidManifest.xml` propio en el repo: Unity genera el suyo en cada build. No es un fallo, pero conviene saberlo antes de la ficha de Play, porque declarar permisos a mano exigirá crear uno en `Assets/Plugins/Android/` | — | Informativo |

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

### Pendiente — Publicación (0%)

- **Decidir el `applicationIdentifier` definitivo** — hoy es `com.unity.jc.ARTowerGame`. Irreversible una vez publicado, así que va primero
- Keystore de Android (hoy firma con la de debug)
- `companyName` real en lugar de `DefaultCompany`
- Ícono de la app
- Capturas de pantalla para la ficha
- Política de privacidad y formulario de Data Safety (la app pide cámara)
- Optimización de build: R8 activo y target SDK fijo en lugar de Automatic

---

## Sesiones

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

**Pendiente inmediato (sin cambios respecto a ayer)**

1. Abrir el proyecto en Unity, compilar y probar el flujo completo en dispositivo. Todo lo de la Fase 1 depende de esto y nada nuevo debería construirse encima hasta validarlo.
2. Decidir el `applicationIdentifier` definitivo (N4) — bloquea la publicación y es irreversible.
3. Decidir qué hacer con la dependencia del reloj del dispositivo (N1 + hallazgos 7 y 14): arreglarlo de raíz o aceptarlo explícitamente.

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
