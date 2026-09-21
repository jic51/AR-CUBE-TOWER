# Bitácora — AR Tower Game

Registro de trabajo del proyecto. Se actualiza al cierre de cada sesión.

**Repositorio:** https://github.com/jic51/AR-CUBE-TOWER
**Stack:** Unity 6.0.3 · ARFoundation 6.3.2 · URP 17.0 · Android

---

## Rutina de trabajo

| Momento | Acción |
|---|---|
| **Al iniciar** | Verificar que local y GitHub estén sincronizados (`git fetch` + comparar commits) |
| **Durante** | Trabajar normal, anotando logros y bloqueos |
| **Al cerrar** | Actualizar esta bitácora + commit + push a GitHub |

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

Verificado contra el código real el 2026-09-19.

| # | Tarea | Archivo | Estado |
|---|---|---|---|
| 1 | **Sistema de sonido completo** — no existe ningún `AudioSource` ni clip propio en el proyecto | nuevo | No empezado |
| 2 | **Modo supervivencia** — no existe | nuevo | No empezado |
| 3 | **Toggle de idioma** en configuración (sonido y vibración ya funcionan) | `PanelConfiguracion.cs` | No empezado |
| 4 | Verificar en dispositivo que el bug de destrucción en cascada quedó resuelto | `CuboInteligente.cs:251` | Umbral aplicado, falta probar |

### Ya resuelto (estaba mal listado como pendiente)

| Tarea | Verificación |
|---|---|
| `GruaController.Instance` | Existe en `GruaController.cs:6` |
| `ReticuloVisible` | Existe en `GruaController.cs:30` |
| `ReticuloAlturaHit` | Existe en `GruaController.cs:32` |
| Sensibilidad de pinch | Ahora `0.000025f` con slider en Inspector (`SetupFase.cs:22`) |
| Mensajes de setup AR en inglés | Implementado (`SetupFase.cs:122`) |
| Interacciones entre tipos de cubo | Las 5 implementadas en `CuboInteligente.cs:582-598` |
| CuboPreview3D con RawImage | Implementado (RenderTexture presente) |

### Pendiente — Seguridad

19 hallazgos totales. Los 4 primeros **bloquean el lanzamiento** y están verificados leyendo el código.

#### Bloqueantes

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 1 | Softlock durante el anuncio AR | `ARAdBreak.cs` | ✅ Resuelto (sin probar en dispositivo) |
| 2 | Farming infinito de monedas | `GameManager.cs` | ✅ Resuelto (sin probar) |
| 3 | Las vidas no bloquean nada | `GameManager.cs` | ✅ Resuelto (sin probar) |
| 4 | **Sin política de privacidad ni Data Safety** — la app pide cámara; Play lo exige | ficha de Play | Pendiente |

#### Altos y medios

| # | Tarea | Archivo:Línea | Estado |
|---|---|---|---|
| 5 | Recompensa completa del anuncio sin verlo | `ARAdBreak.cs` | ✅ Resuelto (sin probar) |
| 6 | El pack de 3 vidas cobra 120 monedas y puede entregar 1 | `TiendaManager.cs:222` | Pendiente |
| 7 | Regeneración de vidas manipulable adelantando el reloj del teléfono | `EconomiaManager.cs:60` |
| 8 | ARCore Depth marcado *Required* pero desactivado en runtime — recorta dispositivos | `AR Core Settings.asset` |
| 9 | Build sin endurecer: target SDK Automatic, sin R8, logs activos, sin keystore, ID `com.unity.*` | `ProjectSettings.asset` |
| 10 | API key `DEMO_KEY` serializada en la escena e inicializa el SDK en producción | `ARTowerGame.unity:6790` |
| 11 | Guardado JSON sin HMAC — economía editable | `SaveSystem.cs` |
| 12 | Métodos `DEBUG_` sin `#if UNITY_EDITOR` (uno da 999 monedas) | `GameManager.cs:870` |
| 13 | Eliminar campo `fechaNacimiento` sin uso | `PlayerData.cs:9` |
| 14 | Bonus diario manipulable por fecha del dispositivo | `GameManager.cs:155` |
| 15 | Nombre de usuario sin límite ni sanitización | `PanelConfiguracion.cs:127` |

#### Bajos

| # | Tarea | Archivo:Línea |
|---|---|---|
| 16 | `Application.OpenURL` sin validar el esquema de la URL | `ARAdBreak.cs:555` |
| 17 | `MostrarBreak()` no llama al callback si está ocupado → deja `timeScale` en 0 | `LayeredAds.cs:103` |
| 18 | `PlayerPrefs.DeleteAll()` borra claves de terceros | `PanelConfiguracion.cs:160` |
| 19 | Log de API key visible en Logcat de producción | `LayeredAds.cs:52` |

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

- Keystore de Android
- Ícono de la app
- Capturas de pantalla para la ficha
- Política de privacidad
- Optimización de build

---

## Sesiones

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

**Pendiente de esta sesión**

- Revisar resultados de la auditoría profunda del agente e incorporarlos a la lista de seguridad

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
